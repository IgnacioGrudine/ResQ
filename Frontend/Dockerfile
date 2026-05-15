# syntax=docker/dockerfile:1

# ---------- Build stage ----------
FROM node:22-alpine AS build
WORKDIR /app

# Install deps with cached layer
COPY package.json package-lock.json ./
RUN npm ci

# Build the Angular app for production
COPY . .
RUN npm run build -- --configuration=production

# ---------- Runtime stage ----------
FROM nginx:alpine AS runtime

# Custom nginx config: SPA-aware (try_files for client-side routes)
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copy compiled artifacts. Angular 21 emits to dist/<project>/browser/
COPY --from=build /app/dist/resq-frontend/browser /usr/share/nginx/html

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
