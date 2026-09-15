# ==============================================================================
# Multi-stage Dockerfile for SaaS Frontend (React + Vite + Nginx)
# Context: ./frontend
# ==============================================================================

# ------------------------------------------------------------------------------
# Stage 1: Build Application
# ------------------------------------------------------------------------------
FROM node:20-alpine AS builder
WORKDIR /app

# Copiar manifiestos para aprovechar el cache de dependencias
COPY package.json package-lock.json ./
RUN npm ci

# Copiar el resto del código fuente y compilar
COPY . .
RUN npm run build

# ------------------------------------------------------------------------------
# Stage 2: Production Web Server (Nginx Alpine)
# ------------------------------------------------------------------------------
FROM nginx:alpine AS runner

# Eliminar configuración por defecto de Nginx y copiar la personalizada
RUN rm -rf /etc/nginx/conf.d/default.conf
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copiar archivos compilados desde el builder
COPY --from=builder /app/dist /usr/share/nginx/html

# Exponer el puerto HTTP
EXPOSE 80

# Healthcheck
HEALTHCHECK --interval=30s --timeout=5s --start-period=5s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:80/ || exit 1

CMD ["nginx", "-g", "daemon off;"]
