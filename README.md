# Logistique Les Lions

Plataforma SaaS de compraventa internacional de vehículos con gestión documental transfronteriza completa: aduanas, homologaciones y trámites legales por país.

## Stack Técnico

| Capa | Tecnología |
|---|---|
| Frontend | Angular 19, Tailwind CSS v4, Signals, SSR, PWA |
| Backend | .NET 9, Clean Architecture, CQRS/MediatR, Minimal APIs |
| Base de datos | PostgreSQL 16 (schemas por módulo, tsvector, JSONB) |
| Caché | Redis 7 |
| Auth | JWT (access 15min + refresh 30d) |
| Contenedores | Docker Compose |

## Inicio rápido

### Prerequisitos

- Docker Desktop
- .NET 9 SDK
- Node.js 22+
- Angular CLI 19+

### 1. Levantar la infraestructura

```bash
docker-compose up -d postgres redis pgadmin
```

Servicios disponibles:
- PostgreSQL: `localhost:5432`
- pgAdmin: `http://localhost:5050` (admin@logistiqueleslions.com / admin_dev_password)
- Redis: `localhost:6379`

### 2. Backend (.NET 9)

```bash
cd backend

# Restaurar paquetes
dotnet restore

# Crear el archivo de secretos de desarrollo
# (Copiar appsettings.Development.json y ajustar connection strings)

# Aplicar migraciones (se aplican automáticamente en Development)
dotnet run --project src/LogistiqueLesLions.API

# API disponible en: http://localhost:5000
# Docs Scalar: http://localhost:5000/scalar/v1
# Health: http://localhost:5000/health
```

### 3. Frontend (Angular 19)

```bash
cd frontend

# Instalar dependencias
npm install

# Servidor de desarrollo (con proxy a :5000)
npm start

# Frontend disponible en: http://localhost:4200
```

### 4. Levantar todo con Docker

```bash
docker-compose up --build
```

- Frontend: http://localhost:4200
- API: http://localhost:5000
- pgAdmin: http://localhost:5050

## Arquitectura

Consulta [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) para el diagrama completo y las decisiones técnicas.

## Estado de los módulos

Consulta [docs/MODULES.md](docs/MODULES.md) para el roadmap de implementación.

## Documentación de tramitación

Consulta [docs/COMPLIANCE.md](docs/COMPLIANCE.md) para la documentación detallada del módulo de gestión documental internacional.

## Tests

```bash
# Backend
cd backend
dotnet test

# Frontend
cd frontend
npm test
```

## Variables de entorno (producción)

Configurar en Render (backend) y Vercel (frontend):

```
# Backend (Render)
ConnectionStrings__DefaultConnection=<neon-postgresql-url>
ConnectionStrings__Redis=<redis-url>
Jwt__Key=<secret-min-32-chars>
Jwt__Issuer=logistique-les-lions-api
Jwt__Audience=logistique-les-lions-client
Cors__AllowedOrigins=<vercel-url>
ASPNETCORE_ENVIRONMENT=Production

# Frontend (Vercel)
# environment.production.ts apunta a la URL de Render
```

## Colores de marca

| Color | Hex | Uso |
|---|---|---|
| Navy profundo | `#0B1F3A` | Fondo hero, navbar, headings |
| Dorado | `#C9A84C` | Acentos, CTAs, decoraciones |
| Blanco roto | `#F8F6F0` | Fondo principal |

## Licencia

Proyecto privado — Logistique Les Lions © 2026
