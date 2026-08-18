# syntax=docker/dockerfile:1

# ── Build stage (SDK) ──────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/CHL.NrbGateway.Domain/CHL.NrbGateway.Domain.csproj", "src/CHL.NrbGateway.Domain/"]
COPY ["src/CHL.NrbGateway.Application/CHL.NrbGateway.Application.csproj", "src/CHL.NrbGateway.Application/"]
COPY ["src/CHL.NrbGateway.Infrastructure/CHL.NrbGateway.Infrastructure.csproj", "src/CHL.NrbGateway.Infrastructure/"]
COPY ["src/CHL.NrbGateway.Api/CHL.NrbGateway.Api.csproj", "src/CHL.NrbGateway.Api/"]
RUN dotnet restore "src/CHL.NrbGateway.Api/CHL.NrbGateway.Api.csproj"

COPY . .
RUN dotnet publish "src/CHL.NrbGateway.Api/CHL.NrbGateway.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ── Runtime stage (ASP.NET runtime only, no SDK) ───────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5050
ENV ASPNETCORE_URLS=http://+:5050
ENTRYPOINT ["dotnet", "CHL.NrbGateway.Api.dll"]
