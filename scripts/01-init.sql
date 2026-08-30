-- ============================================================================
-- CHL NRB Verification Gateway — Database Initialization Script
-- Ref: CICT/10032601/NRB
-- PostgreSQL 18 — Schema, Roles, Extensions, Seed Admin
-- ============================================================================

-- 1. Enable required extensions
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- 2. Create schemas
CREATE SCHEMA IF NOT EXISTS kyc;
CREATE SCHEMA IF NOT EXISTS config;
CREATE SCHEMA IF NOT EXISTS verification_portal;

-- 3. Create least-privilege roles
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'gateway_role') THEN
        CREATE ROLE gateway_role WITH LOGIN PASSWORD 'gateway_secure_password_dev';
    END IF;
    IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'portal_role') THEN
        CREATE ROLE portal_role WITH LOGIN PASSWORD 'portal_secure_password_dev';
    END IF;
    IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'manual_portal_role') THEN
        CREATE ROLE manual_portal_role WITH LOGIN PASSWORD 'manual_portal_secure_password_dev';
    END IF;
END
$$;

-- 4. Grant schema permissions
GRANT USAGE ON SCHEMA kyc TO gateway_role;
GRANT USAGE ON SCHEMA config TO portal_role;
GRANT USAGE ON SCHEMA verification_portal TO manual_portal_role;

-- Grant full DML on respective schemas (tables created later via EF migrations)
ALTER DEFAULT PRIVILEGES IN SCHEMA kyc
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO gateway_role;
ALTER DEFAULT PRIVILEGES IN SCHEMA kyc
    GRANT USAGE ON SEQUENCES TO gateway_role;

ALTER DEFAULT PRIVILEGES IN SCHEMA config
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO portal_role;
ALTER DEFAULT PRIVILEGES IN SCHEMA config
    GRANT USAGE ON SEQUENCES TO portal_role;

ALTER DEFAULT PRIVILEGES IN SCHEMA verification_portal
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO manual_portal_role;
ALTER DEFAULT PRIVILEGES IN SCHEMA verification_portal
    GRANT USAGE ON SEQUENCES TO manual_portal_role;

-- Also grant gateway_role read access to config.subsidiaries for cross-schema lookups
GRANT USAGE ON SCHEMA config TO gateway_role;
ALTER DEFAULT PRIVILEGES IN SCHEMA config
    GRANT SELECT ON TABLES TO gateway_role;

-- Grant manual_portal_role read access to config and kyc schemas for validation
GRANT USAGE ON SCHEMA config TO manual_portal_role;
GRANT USAGE ON SCHEMA kyc TO manual_portal_role;
ALTER DEFAULT PRIVILEGES IN SCHEMA config GRANT SELECT ON TABLES TO manual_portal_role;
ALTER DEFAULT PRIVILEGES IN SCHEMA kyc GRANT SELECT ON TABLES TO manual_portal_role;

-- 5. Grant postgres superuser default privileges on all schemas (for EF migrations)
ALTER DEFAULT PRIVILEGES IN SCHEMA kyc
    GRANT ALL ON TABLES TO postgres;
ALTER DEFAULT PRIVILEGES IN SCHEMA kyc
    GRANT ALL ON SEQUENCES TO postgres;

ALTER DEFAULT PRIVILEGES IN SCHEMA config
    GRANT ALL ON TABLES TO postgres;
ALTER DEFAULT PRIVILEGES IN SCHEMA config
    GRANT ALL ON SEQUENCES TO postgres;

ALTER DEFAULT PRIVILEGES IN SCHEMA verification_portal
    GRANT ALL ON TABLES TO postgres;
ALTER DEFAULT PRIVILEGES IN SCHEMA verification_portal
    GRANT ALL ON SEQUENCES TO postgres;
