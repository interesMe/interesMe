FROM node:22-alpine

WORKDIR /app

COPY package*.json ./
RUN npm install

COPY . .

EXPOSE 4200

CMD ["sh", "-c", "printf 'window.__INTERESME_CONFIG__ = { apiBaseUrl: \"%s\", googleClientId: \"%s\" };\\n' \"${API_BASE_URL:-http://localhost:8080/api}\" \"${GOOGLE_CLIENT_ID:-}\" > /app/public/env.js && npm start -- --host 0.0.0.0"]
