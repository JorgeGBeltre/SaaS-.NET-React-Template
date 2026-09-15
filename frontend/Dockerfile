# ==============================================================================
# Multi-stage Dockerfile for ECF Manager Frontend (Next.js 15 Standalone)
# ==============================================================================

# ------------------------------------------------------------------------------
# Stage 1: Install Dependencies
# ------------------------------------------------------------------------------
FROM node:20-alpine AS deps
RUN apk add --no-cache libc6-compat
WORKDIR /app

# Instalar pnpm
RUN npm install -g pnpm@12.3.4

# Copiar manifiestos de paquetes
COPY package.json pnpm-lock.yaml pnpm-workspace.yaml* .npmrc* ./
RUN pnpm install --frozen-lockfile

# ------------------------------------------------------------------------------
# Stage 2: Build Application
# ------------------------------------------------------------------------------
FROM node:20-alpine AS builder
WORKDIR /app

# Instalar pnpm
RUN npm install -g pnpm@12.3.4

COPY --from=deps /app/node_modules ./node_modules
COPY . .
RUN mkdir -p /app/public

# Deshabilitar telemetría de Next.js durante el build
ENV NEXT_TELEMETRY_DISABLED=1
ENV NODE_ENV=production

ARG NEXT_PUBLIC_API_URL=http://localhost:5050
ARG NEXT_PUBLIC_ECF_AMBIENTE=Test
ENV NEXT_PUBLIC_API_URL=$NEXT_PUBLIC_API_URL
ENV NEXT_PUBLIC_ECF_AMBIENTE=$NEXT_PUBLIC_ECF_AMBIENTE

# Compilar la aplicación Next.js con salida standalone
RUN pnpm run build

# ------------------------------------------------------------------------------
# Stage 3: Production Runner (Lightweight Alpine)
# ------------------------------------------------------------------------------
FROM node:20-alpine AS runner
WORKDIR /app

ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1
ENV PORT=3000
ENV HOSTNAME="0.0.0.0"

# Crear usuario non-root para seguridad
RUN addgroup --system --gid 1001 nodejs && \
    adduser --system --uid 1001 nextjs

# Copiar archivos públicos y estáticos
COPY --from=builder /app/public ./public

# Crear directorio .next y ajustar permisos
RUN mkdir .next && chown nextjs:nodejs .next

# Copiar el build standalone y los assets estáticos
COPY --from=builder --chown=nextjs:nodejs /app/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/.next/static ./.next/static

# Cambiar al usuario sin privilegios
USER nextjs

# Exponer el puerto de la aplicación
EXPOSE 3000

# Healthcheck
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://127.0.0.1:3000/ || exit 0

# Iniciar servidor Node.js de Next.js standalone
CMD ["node", "server.js"]
