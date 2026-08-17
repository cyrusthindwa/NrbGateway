INSERT INTO config.subsidiary_api_keys (
    "Id", "SubsidiaryId", "KeyHash", "KeyPrefix", "Status",
    "RateLimitPerMinute", "CreatedAt", "CreatedBy"
)
SELECT
    gen_random_uuid(),
    (SELECT "Id" FROM config.subsidiaries LIMIT 1),
    encode(digest('chl_live_test_key_12345', 'sha256'), 'hex'),
    'chl_live_tes',
    'ACTIVE',
    100,
    NOW(),
    (SELECT "Id" FROM config.admin_users LIMIT 1)
WHERE NOT EXISTS (
    SELECT 1 FROM config.subsidiary_api_keys WHERE "KeyHash" = encode(digest('chl_live_test_key_12345', 'sha256'), 'hex')
);
