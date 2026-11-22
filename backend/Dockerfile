FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/Madrasa.Api -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
# Uploads (photos, logo) live on a volume; create it owned by the non-root user so a fresh volume inherits that
RUN mkdir -p /data/uploads && chown -R $APP_UID /data
ENV Uploads__Root=/data/uploads ASPNETCORE_URLS=http://+:8080
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Madrasa.Api.dll"]
