using System.Text;
using System.Text.Json.Serialization;
using Madrasa.Api.Infrastructure;
using Madrasa.Application;
using Madrasa.Application.Auth;
using Madrasa.Application.Common;
using Madrasa.Infrastructure;
using Madrasa.Infrastructure.Persistence;
using System.Threading.RateLimiting;
using Madrasa.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;

builder.Services.AddApplication();
builder.Services.AddInfrastructure(cfg);
builder.Services.AddExceptionHandler<AppExceptionHandler>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

var jwtKey = cfg["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
    throw new InvalidOperationException("Set Jwt:Key (env Jwt__Key) to a random secret of at least 32 characters.");
if (!builder.Environment.IsDevelopment())
{
    // Refuse to start a real server on placeholder settings
    if ((cfg.GetConnectionString("Default") ?? "").Contains("CHANGE_ME"))
        throw new InvalidOperationException("Set ConnectionStrings__Default for production.");
    if (jwtKey.StartsWith("dev-", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("Jwt:Key looks like a development key. Generate a new random one for production.");
}
builder.Services.AddMemoryCache();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.MapInboundClaims = false;
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = cfg["Jwt:Issuer"], ValidAudience = cfg["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        NameClaimType = "name", RoleClaimType = System.Security.Claims.ClaimTypes.Role,
        ClockSkew = TimeSpan.FromMinutes(1),
    };
    // A token stays valid only while the account is active and its security stamp unchanged
    // (password reset, deactivation, role or permission change). Checked at most every 30 s per user.
    o.Events = new JwtBearerEvents
    {
        OnTokenValidated = async ctx =>
        {
            var sub = ctx.Principal?.FindFirst("sub")?.Value;
            var stamp = ctx.Principal?.FindFirst(AuthService.StampClaim)?.Value;
            if (sub is null || stamp is null) { ctx.Fail("Token has no stamp"); return; }
            var cache = ctx.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();
            var current = await cache.GetOrCreateAsync($"auth.stamp:{sub}", async e =>
            {
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
                var u = await ctx.HttpContext.RequestServices.GetRequiredService<UserManager<AppUser>>().FindByIdAsync(sub);
                return u is { IsActive: true } ? u.SecurityStamp : null;
            });
            if (current is null || current != stamp) ctx.Fail("Session ended");
        },
    };
});
builder.Services.AddAuthorization(o =>
{
    foreach (var p in Permissions.All) o.AddPolicy(p, pb => pb.RequireClaim("perm", p));
});

builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Photos arrive as data URLs
builder.WebHost.ConfigureKestrel(k => k.Limits.MaxRequestBodySize = 5 * 1024 * 1024);
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Madrasa API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT" });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = Array.Empty<string>()
    });
});

// Public website forms (online admission, status check): per-IP fixed window
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.OnRejected = async (ctx, ct) =>
    {
        ctx.HttpContext.Response.ContentType = "application/problem+json";
        await ctx.HttpContext.Response.WriteAsync("{\"title\":\"অনেকবার চেষ্টা করা হয়েছে। কিছুক্ষণ পরে আবার চেষ্টা করুন।\",\"status\":429}", ct);
    };
    // Sign-in: per IP (generous, since a whole office may share one IP), on top of the per-account lockout after 5 wrong passwords
    o.AddPolicy("login", http => RateLimitPartition.GetFixedWindowLimiter(http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(5), QueueLimit = 0 }));
    o.AddPolicy("public", http => RateLimitPartition.GetFixedWindowLimiter(http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(10), QueueLimit = 0 }));
});

var origins = cfg.GetSection("Cors:Origins").Get<string[]>() ?? new[] { "http://localhost:4200" };
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));

// Behind the HTTPS proxy (Caddy) the client IP and scheme come from X-Forwarded-* headers
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear(); o.KnownProxies.Clear();   // only the proxy can reach the API (see deploy/docker-compose.yml)
    o.ForwardLimit = 1;
});

var app = builder.Build();

// Maintenance command: set a new password for a user, e.g. a forgotten admin password.
//   docker compose run --rm -e NEW_PASSWORD='...' api --reset-password admin
if (args is ["--reset-password", var who, ..])
{
    var pwd = Environment.GetEnvironmentVariable("NEW_PASSWORD");
    if (string.IsNullOrWhiteSpace(pwd) || pwd.Length < 10) { Console.Error.WriteLine("Set NEW_PASSWORD (at least 10 characters)."); return 1; }
    using var scope = app.Services.CreateScope();
    var um = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    var u = await um.FindByNameAsync(who);
    if (u is null) { Console.Error.WriteLine($"No user named '{who}'."); return 1; }
    var r = await um.SetPasswordAsync(u, pwd);
    if (!r.Succeeded) { Console.Error.WriteLine(string.Join("; ", r.Errors.Select(e => e.Description))); return 1; }
    if (!u.IsActive) { u.IsActive = true; await um.UpdateAsync(u); }
    Console.WriteLine($"Password for '{who}' changed. Other sessions of this user are signed out.");
    return 0;
}

app.UseForwardedHeaders();
app.Use(async (ctx, next) =>
{
    var h = ctx.Response.Headers;
    h["X-Content-Type-Options"] = "nosniff";
    h["X-Frame-Options"] = "DENY";
    h["Referrer-Policy"] = "strict-origin-when-cross-origin";
    h["Cross-Origin-Resource-Policy"] = "same-site";
    if (!ctx.Request.Path.StartsWithSegments("/swagger")) h["Content-Security-Policy"] = "default-src 'none'; img-src 'self'; frame-ancestors 'none'";
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseExceptionHandler();
app.UseCors();
// Student photos and other uploads
var uploads = cfg["Uploads:Root"] ?? Path.Combine(AppContext.BaseDirectory, "wwwroot", "uploads");
Directory.CreateDirectory(uploads);
app.UseStaticFiles(new StaticFileOptions { FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploads), RequestPath = "/uploads" });
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

if (!app.Environment.IsEnvironment("Testing")) await DbInitializer.InitializeAsync(app.Services);

app.Run();
return 0;

public partial class Program;
