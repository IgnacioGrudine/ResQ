# syntax=docker/dockerfile:1

# ---------- Build stage ----------
FROM node:22-alpine AS build
WORKDIR /app

# Build arg para inyectar la API Key de Google Maps en producción
ARG GOOGLE_MAPS_API_KEY=""

# Install deps with cached layer
COPY package.json package-lock.json ./
RUN npm ci

# Copy source and replace the API key placeholder in the production environment file
COPY . .
RUN sed -i "s|__GOOGLE_MAPS_API_KEY__|${GOOGLE_MAPS_API_KEY}|g" src/environments/environment.ts

# Build the Angular app for production
RUN npm run build -- --configuration=production

# ---------- Runtime stage ----------
FROM nginx:alpine AS runtime

# Custom nginx config: SPA-aware (try_files for client-side routes)
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copy compiled artifacts. Angular 21 emits to dist/<project>/browser/
COPY --from=build /app/dist/resq-frontend/browser /usr/share/nginx/html

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
