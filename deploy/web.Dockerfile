# Builds the Angular app and serves it with Caddy, which also gets the HTTPS certificate and proxies /api to the API.
# Build context: the repository root (see deploy/docker-compose.yml).
FROM node:22-alpine AS build
WORKDIR /src
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ .
RUN npx ng build --configuration production

FROM caddy:2-alpine
COPY --from=build /src/dist/frontend/browser /srv
COPY deploy/Caddyfile /etc/caddy/Caddyfile
