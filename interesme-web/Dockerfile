FROM node:22-alpine AS development

WORKDIR /app

COPY package*.json ./
RUN npm install

COPY . .

EXPOSE 4200

CMD ["sh", "-c", "printf 'window.__INTERESME_CONFIG__ = { apiBaseUrl: \"%s\", googleClientId: \"%s\" };\\n' \"${API_BASE_URL:-http://localhost:8080/api}\" \"${GOOGLE_CLIENT_ID:-}\" > /app/public/env.js && npm start -- --host 0.0.0.0 --port 4200"]

FROM node:22-alpine AS builder

WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .
RUN npm run build -- --configuration production

FROM nginx:alpine AS production

COPY docker/nginx/default.conf /etc/nginx/conf.d/default.conf
COPY docker/entrypoint/30-interesme-env.sh /docker-entrypoint.d/30-interesme-env.sh
COPY --from=builder /app/dist/interesme-web/browser /usr/share/nginx/html

RUN chmod +x /docker-entrypoint.d/30-interesme-env.sh

EXPOSE 80
