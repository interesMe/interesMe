#!/bin/sh
set -eu

escape_js() {
    printf '%s' "$1" | sed 's/\\/\\\\/g; s/"/\\"/g'
}

api_base_url="$(escape_js "${API_BASE_URL:-http://localhost:8080/api}")"
google_client_id="$(escape_js "${GOOGLE_CLIENT_ID:-}")"

cat > /usr/share/nginx/html/env.js <<EOF
window.__INTERESME_CONFIG__ = {
  apiBaseUrl: "${api_base_url}",
  googleClientId: "${google_client_id}"
};
EOF
