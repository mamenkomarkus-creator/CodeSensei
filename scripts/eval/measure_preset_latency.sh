#!/usr/bin/env bash
# Затримка GET /api/preset/{id}: послідовні запити з паузою (ліміт сервера — 60 запитів/хв з IP).
# Використання: scripts/eval/measure_preset_latency.sh [BASE_URL] [N] [TOKEN]
# Виводить CSV: id,http,ttfb_s,total_s,bytes  (p50/p95 порахуйте будь-яким інструментом).
set -euo pipefail
BASE="${1:-https://codesensei-d5zi.onrender.com}"
N="${2:-30}"
TOKEN="${3:-secret123}"
for i in $(seq 1 "$N"); do
  id=$(( (i - 1) % 24 + 1 ))
  curl -s -o /dev/null \
    -w "${id},%{http_code},%{time_starttransfer},%{time_total},%{size_download}\n" \
    "${BASE}/api/preset/${id}?k=${TOKEN}"
  sleep 1.2
done
