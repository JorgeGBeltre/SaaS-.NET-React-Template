# ==============================================================================
# Multi-stage Dockerfile for Ecf.Api (.NET 10 Web API)
# Standalone container build (context: .)
# Dominican Republic Electronic Invoicing (e-CF DGII)
# ==============================================================================

# ------------------------------------------------------------------------------
# Stage 1: Build & Publish
# ------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS builder
WORKDIR /src

# Instalar dependencias de compilación
RUN apk add --no-cache icu-libs

# Copiar csproj de cada capa interna de Ecf.Api
COPY ["Ecf.Shared/Ecf.Shared.csproj", "Ecf.Shared/"]
COPY ["Ecf.Domain/Ecf.Domain.csproj", "Ecf.Domain/"]
COPY ["Ecf.Application/Ecf.Application.csproj", "Ecf.Application/"]
COPY ["Ecf.Infrastructure/Ecf.Infrastructure.csproj", "Ecf.Infrastructure/"]
COPY ["Ecf.API/Ecf.API.csproj", "Ecf.API/"]

# Restaurar dependencias NuGet
RUN dotnet restore "Ecf.API/Ecf.API.csproj"

# Copiar todo el código fuente interno
COPY ["Ecf.Shared/", "Ecf.Shared/"]
COPY ["Ecf.Domain/", "Ecf.Domain/"]
COPY ["Ecf.Application/", "Ecf.Application/"]
COPY ["Ecf.Infrastructure/", "Ecf.Infrastructure/"]
COPY ["Ecf.API/", "Ecf.API/"]

# Compilar y publicar la aplicación en modo Release
WORKDIR "/src/Ecf.API"
RUN dotnet publish "Ecf.API.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ------------------------------------------------------------------------------
# Stage 2: Runtime Image (Lightweight Alpine)
# ------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

# Instalar librerías de globalización (ICU) requeridas por .NET en Alpine
RUN apk add --no-cache icu-libs icu-data-full tzdata

# Crear directorios para persistencia de datos, logs y certificados
RUN mkdir -p /app/data /app/certificates /app/xml-storage && \
    addgroup -g 1000 appgroup && \
    adduser -u 1000 -G appgroup -s /bin/sh -D appuser && \
    chown -R appuser:appgroup /app

# Copiar artefactos publicados desde el builder
COPY --from=builder --chown=appuser:appgroup /app/publish .

# Variables de entorno por defecto
ENV ASPNETCORE_URLS=http://+:5000 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    TZ=America/Santo_Domingo

# Exponer el puerto del API REST
EXPOSE 5000

# Usar usuario sin privilegios root por seguridad
USER appuser

# Healthcheck
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:5000/api/v1/auth/me || exit 0

# Punto de entrada
ENTRYPOINT ["dotnet", "Ecf.API.dll"]
