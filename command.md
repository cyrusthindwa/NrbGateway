# 1. Apply Kyc schema migrations
dotnet ef database update --project src/CHL.NrbGateway.Infrastructure --startup-project src/CHL.NrbGateway.Api --context KycDbContext

# 2. Apply Config schema migrations
dotnet ef database update --project src/CHL.NrbGateway.Infrastructure --startup-project src/CHL.NrbGateway.Api --context ConfigDbContext

# 3. Seed the admin user (from scripts/02-sample-admin.sql)
docker exec -i chl_nrb_gateway_postgres psql -U postgres -d chl_nrb_gateway < scripts/02-sample-admin.sql

# 4. Start the backend
dotnet run --project src/CHL.NrbGateway.Api