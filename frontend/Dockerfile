# ─── Build stage ───────────────────────────────────────────────────────────
FROM node:22-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm ci --prefer-offline

COPY . .
RUN npm run build:prod

# ─── Runtime stage (Nginx) ──────────────────────────────────────────────────
FROM nginx:alpine AS runtime

# Copiar build de Angular
COPY --from=build /app/dist/logistique-les-lions/browser /usr/share/nginx/html

# Configuración de Nginx para SPA (Angular Router)
RUN printf 'server {\n\
  listen 80;\n\
  root /usr/share/nginx/html;\n\
  index index.html;\n\
  gzip on;\n\
  gzip_types text/plain text/css application/json application/javascript text/xml application/xml image/svg+xml;\n\
  location ~* \\.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2|ttf|eot|webp)$ {\n\
    expires 1y;\n\
    add_header Cache-Control "public, immutable";\n\
  }\n\
  location / {\n\
    try_files $uri $uri/ /index.html;\n\
    add_header X-Frame-Options "DENY";\n\
    add_header X-Content-Type-Options "nosniff";\n\
    add_header Referrer-Policy "strict-origin-when-cross-origin";\n\
  }\n\
}\n' > /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
