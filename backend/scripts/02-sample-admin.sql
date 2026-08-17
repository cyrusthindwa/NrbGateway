-- CHL NRB Verification Gateway - Sample Admin Seed Script
-- Reference: CICT/10032601/NRB
-- Target Database: chl_nrb_gateway | Target Schema: config

-- 1. Enable pgcrypto for BCrypt salt generation
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- 2. Insert or update sample admin user (cthindwa@continental.mw / password)
INSERT INTO config.admin_users (
    "Id",
    "Name",
    "Email",
    "PasswordHash",
    "Status",
    "CreatedAt",
    "UpdatedAt"
)
VALUES (
    gen_random_uuid(),
    'C. Thindwa (ICT)',
    'cthindwa@continental.mw',
    -- BCrypt hash for 'password' using pgcrypto Blowfish salt
    crypt('password', gen_salt('bf')),
    'ACTIVE',
    NOW(),
    NOW()
)
ON CONFLICT ("Email") DO UPDATE 
SET "PasswordHash" = EXCLUDED."PasswordHash",
    "Status" = 'ACTIVE',
    "UpdatedAt" = NOW();
