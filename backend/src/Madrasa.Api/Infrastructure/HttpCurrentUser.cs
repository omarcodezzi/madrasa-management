using Madrasa.Application.Common;

namespace Madrasa.Api.Infrastructure;

public class HttpCurrentUser(IHttpContextAccessor http) : ICurrentUser
{
    public string? UserId => http.HttpContext?.User.FindFirst("sub")?.Value;
}
