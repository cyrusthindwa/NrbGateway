-- ============================================================================
-- CHL NRB Verification Gateway — Manual Portal Schema & Seed Script
-- ============================================================================

-- 1. Create tables in verification_portal schema
CREATE TABLE IF NOT EXISTS verification_portal.manual_users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES config.companies(id) ON DELETE CASCADE,
    email TEXT NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    status TEXT NOT NULL DEFAULT 'ACTIVE',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_login_at TIMESTAMPTZ NULL
);

CREATE TABLE IF NOT EXISTS verification_portal.manual_verification_log (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    manual_user_id UUID NOT NULL REFERENCES verification_portal.manual_users(id) ON DELETE CASCADE,
    company_id UUID NOT NULL REFERENCES config.companies(id) ON DELETE CASCADE,
    national_id_masked TEXT NOT NULL,
    result_status TEXT NOT NULL,
    gateway_request_id UUID NULL REFERENCES kyc.gateway_requests(id) ON DELETE SET NULL,
    requested_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Grant privileges
GRANT ALL ON TABLE verification_portal.manual_users TO manual_portal_role, postgres;
GRANT ALL ON TABLE verification_portal.manual_verification_log TO manual_portal_role, postgres;

-- 2. Seed test company, project, API key, and manual user
DO $$
DECLARE
    v_company_id UUID;
    v_project_id UUID;
    v_admin_id UUID;
BEGIN
    -- Ensure test company exists
    SELECT id INTO v_company_id FROM config.companies WHERE short_code = 'CDHIB';
    IF v_company_id IS NULL THEN
        v_company_id := gen_random_uuid();
        INSERT INTO config.companies (id, name, short_code, created_at)
        VALUES (v_company_id, 'CDH Investment Bank', 'CDHIB', NOW());
    END IF;

    -- Ensure test internal project exists
    SELECT id INTO v_project_id FROM config.projects WHERE short_code = 'CDH-MAN';
    IF v_project_id IS NULL THEN
        v_project_id := gen_random_uuid();
        INSERT INTO config.projects (id, company_id, name, short_code, created_at, project_type)
        VALUES (v_project_id, v_company_id, 'Manual Verification Interface', 'CDH-MAN', NOW(), 'MANUAL_PORTAL');
    END IF;

    -- Ensure API key exists for this project
    SELECT id INTO v_admin_id FROM config.admin_users LIMIT 1;
    IF v_admin_id IS NULL THEN
        v_admin_id := gen_random_uuid();
        INSERT INTO config.admin_users (id, name, email, password_hash, status, created_at, updated_at)
        VALUES (v_admin_id, 'System Administrator', 'admin@continental.mw', crypt('Password123!', gen_salt('bf', 12)), 'ACTIVE', NOW(), NOW());
    END IF;

    IF NOT EXISTS (SELECT 1 FROM config.project_api_keys WHERE project_id = v_project_id) THEN
        INSERT INTO config.project_api_keys (id, project_id, key_hash, key_prefix, status, rate_limit_per_minute, created_at, created_by)
        VALUES (
            gen_random_uuid(),
            v_project_id,
            encode(digest('sec_live_cdh_manual_key_12345', 'sha256'), 'hex'),
            'sec_live_cdh',
            'ACTIVE',
            1000,
            NOW(),
            v_admin_id
        );
    END IF;

    -- Ensure seed manual user account exists
    IF NOT EXISTS (SELECT 1 FROM verification_portal.manual_users WHERE email = 'agent@cdhbank.mw') THEN
        INSERT INTO verification_portal.manual_users (id, company_id, email, password_hash, status, created_at)
        VALUES (
            gen_random_uuid(),
            v_company_id,
            'agent@cdhbank.mw',
            crypt('Password123!', gen_salt('bf', 12)),
            'ACTIVE',
            NOW()
        );
    END IF;

END $$;
