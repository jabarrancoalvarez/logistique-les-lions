# ─── Build stage ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Copiar archivos de proyecto primero para aprovechar caché de capas
COPY ["src/LogistiqueLesLions.API/LogistiqueLesLions.API.csproj", "src/LogistiqueLesLions.API/"]
COPY ["src/LogistiqueLesLions.Application/LogistiqueLesLions.Application.csproj", "src/LogistiqueLesLions.Application/"]
COPY ["src/LogistiqueLesLions.Domain/LogistiqueLesLions.Domain.csproj", "src/LogistiqueLesLions.Domain/"]
COPY ["src/LogistiqueLesLions.Infrastructure/LogistiqueLesLions.Infrastructure.csproj", "src/LogistiqueLesLions.Infrastructure/"]

RUN dotnet restore "src/LogistiqueLesLions.API/LogistiqueLesLions.API.csproj"

COPY . .
WORKDIR "/src/src/LogistiqueLesLions.API"
RUN dotnet publish -c Release -o /app/publish --no-restore

# ─── Runtime stage ───────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app

# Crear directorio de uploads y usuario sin privilegios
RUN mkdir -p /app/uploads && addgroup -S lll && adduser -S lll -G lll
RUN chown -R lll:lll /app

COPY --from=build /app/publish .
USER lll

EXPOSE 10000
ENV ASPNETCORE_URLS=http://+:10000
ENTRYPOINT ["dotnet", "LogistiqueLesLions.API.dll"]
