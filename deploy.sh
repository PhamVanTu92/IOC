#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
# IOC Platform — Deploy script (Ubuntu)
# Chạy: bash deploy.sh
# ─────────────────────────────────────────────────────────────────────────────
set -euo pipefail

COMPOSE="docker compose -f docker/docker-compose.prod.yml"

echo "╔══════════════════════════════════════════╗"
echo "║  IOC Platform — Deploy                  ║"
echo "╚══════════════════════════════════════════╝"

# ── Kiểm tra .env ─────────────────────────────────────────────────────────────
if [[ ! -f docker/.env ]]; then
  echo "⚠️  Chưa có docker/.env"
  echo "   Chạy: cp docker/.env.example docker/.env && nano docker/.env"
  exit 1
fi

# ── Pull images mới nhất ──────────────────────────────────────────────────────
echo ""
echo "▶ Pulling base images..."
$COMPOSE pull --ignore-buildable

# ── Build images ──────────────────────────────────────────────────────────────
echo ""
echo "▶ Building backend & frontend..."
$COMPOSE build --no-cache backend frontend

# ── Khởi động ─────────────────────────────────────────────────────────────────
echo ""
echo "▶ Starting services..."
$COMPOSE up -d

# ── Chờ backend healthy ───────────────────────────────────────────────────────
echo ""
echo "▶ Waiting for backend to be healthy..."
for i in $(seq 1 30); do
  STATUS=$(docker inspect --format='{{.State.Health.Status}}' ioc-backend 2>/dev/null || echo "starting")
  if [[ "$STATUS" == "healthy" ]]; then
    echo "✅ Backend healthy!"
    break
  fi
  echo "   [${i}/30] ${STATUS}..."
  sleep 5
done

# ── Summary ───────────────────────────────────────────────────────────────────
echo ""
echo "╔══════════════════════════════════════════╗"
echo "║  Services                               ║"
echo "╚══════════════════════════════════════════╝"
$COMPOSE ps

SERVER_IP=$(hostname -I | awk '{print $1}')
echo ""
echo "🌐 Frontend  → http://${SERVER_IP}"
echo "🔌 GraphQL   → http://${SERVER_IP}/graphql"
echo "📡 SignalR   → ws://${SERVER_IP}/hubs/dashboard"
