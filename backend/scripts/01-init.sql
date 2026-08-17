-- CHL NRB Verification Gateway Database Initialization Script
-- Reference: CICT/10032601/NRB

-- 1. Enable pgcrypto extension for sym encryption / hashing
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- 2. Create Schemas
CREATE SCHEMA IF NOT EXISTS kyc;
CREATE SCHEMA IF NOT EXISTS config;

-- 3. Create Roles with least-privilege grants (if not already existing)
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'gateway_role') THEN
        CREATE ROLE gateway_role WITH LOGIN PASSWORD 'gateway_secure_password_dev';
    END IF;
    IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'portal_role') THEN
        CREATE ROLE portal_role WITH LOGIN PASSWORD 'portal_secure_password_dev';
    END IF;
END $$;

-- 4. Revoke public privileges on schemas
REVOKE ALL ON SCHEMA kyc FROM PUBLIC;
REVOKE ALL ON SCHEMA config FROM PUBLIC;

-- 5. Role Scoped Grants - gateway_role (kyc schema)
GRANT USAGE ON SCHEMA kyc TO gateway_role;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA kyc TO gateway_role;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA kyc TO gateway_role;
ALTER DEFAULT PRIVILEGES IN SCHEMA kyc GRANT ALL ON TABLES TO gateway_role;
ALTER DEFAULT PRIVILEGES IN SCHEMA kyc GRANT ALL ON SEQUENCES TO gateway_role;

-- 6. Role Scoped Grants - portal_role (config schema)
GRANT USAGE ON SCHEMA config TO portal_role;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA config TO portal_role;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA config TO portal_role;
ALTER DEFAULT PRIVILEGES IN SCHEMA config GRANT ALL ON TABLES TO portal_role;
ALTER DEFAULT PRIVILEGES IN SCHEMA config GRANT ALL ON SEQUENCES TO portal_role;
