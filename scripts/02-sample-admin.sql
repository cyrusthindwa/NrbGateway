-- ============================================================================
-- CHL NRB Verification Gateway — Sample Admin Seed Data (Development Only)
-- This creates the initial admin user for local development login
-- ============================================================================

-- Insert sample admin user (BCrypt hash for 'password')
-- The hash below is BCrypt for the password 'password' (12 rounds)
INSERT INTO config."AdminUsers" (
    "Id", "Name", "Email", "PasswordHash", "Status", "CreatedAt", "UpdatedAt"
) VALUES (
    gen_random_uuid(),
    'CHL ICT Admin',
    'cthindwa@continental.mw',
    crypt('password', gen_salt('bf', 12)),
    'ACTIVE',
    NOW(),
    NOW()
) ON CONFLICT DO NOTHING;
