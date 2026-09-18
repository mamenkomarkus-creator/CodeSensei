#!/usr/bin/env bash
set -euo pipefail
URL="${1:-https://codesensei-d5zi.onrender.com/health}"
echo "Pinging $URL"
curl -fsS -m 90 "$URL"
echo
