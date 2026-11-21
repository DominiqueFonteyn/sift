# Build context: repository root (see docker-compose.yml)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Sift.Domain/Sift.Domain.csproj src/Sift.Domain/
COPY src/Sift.Application/Sift.Application.csproj src/Sift.Application/
COPY src/Sift.Infrastructure/Sift.Infrastructure.csproj src/Sift.Infrastructure/
COPY src/Sift.Api/Sift.Api.csproj src/Sift.Api/
RUN dotnet restore src/Sift.Api/Sift.Api.csproj

COPY src/Sift.Domain/ src/Sift.Domain/
COPY src/Sift.Application/ src/Sift.Application/
COPY src/Sift.Infrastructure/ src/Sift.Infrastructure/
COPY src/Sift.Api/ src/Sift.Api/
RUN dotnet publish src/Sift.Api/Sift.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# SQLite lives on a volume so data survives container rebuilds; the non-root app user must own it.
RUN mkdir -p /data && chown app:app /data
VOLUME /data

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    ConnectionStrings__Sift="Data Source=/data/sift.db" \
    Database__MigrateOnStartup=true

COPY --from=build /app/publish .
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "Sift.Api.dll"]
