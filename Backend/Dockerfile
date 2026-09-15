# ==============================================================================
# Multi-stage Dockerfile for SaaS .NET 9 Web API
# Context: ./Backend
# ==============================================================================

# ------------------------------------------------------------------------------
# Stage 1: Build & Publish
# ------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS builder
WORKDIR /src

# Instalar dependencias de globalización requeridas para compilación
RUN apk add --no-cache icu-libs tzdata

# Copiar archivos .csproj de cada capa para aprovechar la caché de Docker
COPY ["src/Shared/Shared.csproj", "src/Shared/"]
COPY ["src/Domain/Domain.csproj", "src/Domain/"]
COPY ["src/Application/Application.csproj", "src/Application/"]
COPY ["src/Infrastructure/Infrastructure.csproj", "src/Infrastructure/"]
COPY ["src/Api/Api.csproj", "src/Api/"]

# Restaurar dependencias del proyecto API y sus referencias
RUN dotnet restore "src/Api/Api.csproj"

# Copiar todo el código fuente
COPY src/ src/

# Compilar y publicar en modo Release
WORKDIR "/src/src/Api"
RUN dotnet publish "Api.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ------------------------------------------------------------------------------
# Stage 2: Runtime Image (Lightweight Alpine)
# ------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app

# Instalar soporte de globalización (ICU) y zonas horarias
RUN apk add --no-cache icu-libs icu-data-full tzdata

# Crear usuario y grupo sin privilegios de root para ejecución segura
RUN addgroup -g 1000 appgroup && \
    adduser -u 1000 -G appgroup -s /bin/sh -D appuser && \
    chown -R appuser:appgroup /app

# Copiar artefactos publicados desde la fase de compilación
COPY --from=builder --chown=appuser:appgroup /app/publish .

# Configuración de variables de entorno por defecto
ENV ASPNETCORE_URLS=http://+:5000 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Exponer el puerto del API
EXPOSE 5000

# Cambiar al usuario sin privilegios root
USER appuser

# Healthcheck contra el endpoint /health configurado en Program.cs
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:5000/health || exit 1

ENTRYPOINT ["dotnet", "Api.dll"]
