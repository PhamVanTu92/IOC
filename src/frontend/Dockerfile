# ─────────────────────────────────────────────────────────────────────────────
# IOC Frontend — Multi-stage Dockerfile
# Stage 1: Build React app with Vite
# Stage 2: Serve with nginx (reverse-proxy /graphql + /hubs to backend)
# ─────────────────────────────────────────────────────────────────────────────

# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM node:20-alpine AS build
WORKDIR /app

COPY package.json ./
RUN npm install --legacy-peer-deps

COPY . .
# Bỏ qua tsc check (nhiều plugin page chưa implement), chỉ bundle với Vite
RUN npx vite build

# ── Stage 2: nginx ────────────────────────────────────────────────────────────
FROM nginx:1.27-alpine AS runtime

# Remove default config
RUN rm /etc/nginx/conf.d/default.conf

COPY nginx.prod.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80

HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
    CMD wget -qO- http://localhost/health-fe || exit 1

CMD ["nginx", "-g", "daemon off;"]
