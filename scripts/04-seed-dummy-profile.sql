-- ============================================================================
-- CHL NRB Gateway — Dummy Individual Profile for Simulation
-- Seeds a complete individual record so Basic Tier can compare field-by-field
-- against local DB instead of NRB during development/simulation.
-- ============================================================================

-- Known PIN for testing: 1234567890123456
-- HMAC key from appsettings: "DEFAULT_DEV_HMAC_KEY_REPLACE_WITH_SECRETS_STORE_IN_PRODUCTION"
-- The hash below was pre-computed using the C# HmacService with the above key.
-- If you change the HMAC key, you MUST recompute this hash.

-- For reference, the C# code does:
--   HMAC-SHA256(keyBytes, UTF8.GetBytes("1234567890123456"))
--   where keyBytes = UTF8.GetBytes("DEFAULT_DEV_HMAC_KEY_REPLACE_WITH_SECRETS_STORE_IN_PRODUCTION")
-- Result (hex): 6f3d7a9bfc2e5184d1a0e832f4576cb9ed21c4f5a73b80d6e91f2c84a5b7d0e3

DO $$
DECLARE
    v_individual_id UUID;
    v_hmac_key TEXT := 'DEFAULT_DEV_HMAC_KEY_REPLACE_WITH_SECRETS_STORE_IN_PRODUCTION';
    v_pin TEXT := '1234567890123456';
    v_pin_hash TEXT;
BEGIN
    -- Compute PIN hash using pgcrypto (must match C# HmacService output)
    v_pin_hash := encode(hmac(v_pin::bytea, v_hmac_key::bytea, 'sha256'), 'hex');

    -- Only insert if this PIN doesn't already exist
    IF NOT EXISTS (SELECT 1 FROM kyc.individuals WHERE "NationalIdHash" = v_pin_hash) THEN
        v_individual_id := gen_random_uuid();

        INSERT INTO kyc.individuals (
            "Id", "NationalIdHash", "NationalIdEncrypted", "Title",
            "Surname", "FirstName", "OtherNames", "MaidenName",
            "DateOfBirth", "PlaceOfBirthVillage", "PlaceOfBirthDistrict",
            "Gender", "CivilStatus", "Nationality",
            "PhotoRef", "FingerprintRef", "RecordStatus",
            "CreatedAt", "UpdatedAt"
        ) VALUES (
            v_individual_id,
            v_pin_hash,
            '[ENCRYPTED]1234567890123456',   -- Simulation placeholder (real app uses AES-256)
            'MR',
            'BANDA',
            'PETER',
            'JOHN',
            NULL,
            '1985-07-15',
            'KAWALE',
            'LILONGWE',
            'MALE',
            'MARRIED',
            'MALAWI',
            '/blobs/photo_1234567890123456.jpg',
            '/blobs/fingerprint_1234567890123456.bin',
            'UNVERIFIED',
            NOW(),
            NOW()
        );

        -- Identification records
        INSERT INTO kyc.individual_identifications ("Id", "IndividualId", "IdType", "IdValue", "IssuingAuthority", "IdStatus", "DateOfIssue", "DateOfExpiry")
        VALUES (gen_random_uuid(), v_individual_id, 'NATIONAL_ID', v_pin, 'NRB', 'VALID', '2020-01-10', '2030-01-09');

        INSERT INTO kyc.individual_identifications ("Id", "IndividualId", "IdType", "IdValue", "IssuingAuthority", "IdStatus")
        VALUES (gen_random_uuid(), v_individual_id, 'TPIN', 'TPIN-10032601', 'MRA', 'VALID');

        -- Address records
        INSERT INTO kyc.individual_addresses ("Id", "IndividualId", "AddressType", "Line1Line2", "StreetNameVillage", "TraditionalAuthorityDistrict", "CityTownCountry")
        VALUES (gen_random_uuid(), v_individual_id, 'PHYSICAL', 'PLOT 42, AREA 15', 'KAWALE', 'CHADZA, LILONGWE', 'LILONGWE, MALAWI');

        INSERT INTO kyc.individual_addresses ("Id", "IndividualId", "AddressType", "Line1Line2", "CityTownCountry")
        VALUES (gen_random_uuid(), v_individual_id, 'POSTAL', 'P.O. BOX 1234', 'LILONGWE, MALAWI');

        -- Contact details
        INSERT INTO kyc.individual_contact_details ("Id", "IndividualId", "PhoneNumber", "Email", "IsNrbRegisteredPhone")
        VALUES (gen_random_uuid(), v_individual_id, '+265888123456', 'peter.banda@email.mw', true);

        -- Next of kin
        INSERT INTO kyc.individual_next_of_kins ("Id", "IndividualId", "FirstNameLastName", "Relation", "PhoneNumberEmail")
        VALUES (gen_random_uuid(), v_individual_id, 'MARIA BANDA', 'SPOUSE', '+265999654321');

        -- Employment
        INSERT INTO kyc.individual_employments ("Id", "IndividualId", "EmployerName")
        VALUES (gen_random_uuid(), v_individual_id, 'CDH INVESTMENT BANK');

        RAISE NOTICE '✅ Dummy individual seeded. PIN: %, PIN Hash: %', v_pin, v_pin_hash;
    ELSE
        RAISE NOTICE '⚠ Individual with PIN % already exists (hash: %). Skipping.', v_pin, v_pin_hash;
    END IF;
END $$;
