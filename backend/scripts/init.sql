-- Script de inicialización de PostgreSQL para Logistique Les Lions
-- Ejecutado automáticamente por docker-entrypoint cuando se crea la BD por primera vez

-- Crear schemas separados por módulo (siguiendo el spec)
CREATE SCHEMA IF NOT EXISTS vehicles;
CREATE SCHEMA IF NOT EXISTS users;
CREATE SCHEMA IF NOT EXISTS transactions;
CREATE SCHEMA IF NOT EXISTS compliance;
CREATE SCHEMA IF NOT EXISTS messaging;
CREATE SCHEMA IF NOT EXISTS reviews;
CREATE SCHEMA IF NOT EXISTS logistics;
CREATE SCHEMA IF NOT EXISTS admin;

-- Extensión para UUID generation
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Extensión para búsqueda full-text
CREATE EXTENSION IF NOT EXISTS "unaccent";

-- Extensión para Row Level Security
-- (RLS se habilita tabla por tabla via EF Core migrations)

-- Función de trigrama para búsqueda aproximada
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

-- Log de inicialización
DO $$
BEGIN
  RAISE NOTICE 'Logistique Les Lions — Schemas y extensiones inicializadas correctamente';
END $$;
