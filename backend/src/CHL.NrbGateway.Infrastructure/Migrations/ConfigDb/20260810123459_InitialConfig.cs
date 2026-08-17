using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CHL.NrbGateway.Infrastructure.Migrations.ConfigDb
{
    /// <inheritdoc />
    public partial class InitialConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "config");

            migrationBuilder.CreateTable(
                name: "admin_users",
                schema: "config",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    LastLoginAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "subsidiaries",
                schema: "config",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ShortCode = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subsidiaries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cache_retention_policy",
                schema: "config",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DataType = table.Column<string>(type: "text", nullable: false),
                    FreshnessValue = table.Column<int>(type: "integer", nullable: false),
                    FreshnessUnit = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cache_retention_policy", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cache_retention_policy_admin_users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "config",
                        principalTable: "admin_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "config_audit_log",
                schema: "config",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdminId = table.Column<Guid>(type: "uuid", nullable: false),
                    SettingArea = table.Column<string>(type: "text", nullable: false),
                    SettingKey = table.Column<string>(type: "text", nullable: false),
                    OldValue = table.Column<string>(type: "text", nullable: true),
                    NewValue = table.Column<string>(type: "text", nullable: true),
                    ChangedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RollbackOfId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_config_audit_log", x => x.Id);
                    table.ForeignKey(
                        name: "FK_config_audit_log_admin_users_AdminId",
                        column: x => x.AdminId,
                        principalSchema: "config",
                        principalTable: "admin_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_config_audit_log_config_audit_log_RollbackOfId",
                        column: x => x.RollbackOfId,
                        principalSchema: "config",
                        principalTable: "config_audit_log",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "nrb_environment_settings",
                schema: "config",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Environment = table.Column<string>(type: "text", nullable: false),
                    BasicEndpointUrl = table.Column<string>(type: "text", nullable: false),
                    TextLookupEndpointUrl = table.Column<string>(type: "text", nullable: false),
                    IntermediateEndpointUrl = table.Column<string>(type: "text", nullable: false),
                    AdvancedEndpointUrl = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nrb_environment_settings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_nrb_environment_settings_admin_users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "config",
                        principalTable: "admin_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "verification_tier_settings",
                schema: "config",
                columns: table => new
                {
                    Tier = table.Column<string>(type: "text", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_verification_tier_settings", x => x.Tier);
                    table.ForeignKey(
                        name: "FK_verification_tier_settings_admin_users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "config",
                        principalTable: "admin_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "subsidiary_api_keys",
                schema: "config",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubsidiaryId = table.Column<Guid>(type: "uuid", nullable: false),
                    KeyHash = table.Column<string>(type: "text", nullable: false),
                    KeyPrefix = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    RateLimitPerMinute = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RotatedAtRevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subsidiary_api_keys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_subsidiary_api_keys_admin_users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "config",
                        principalTable: "admin_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_subsidiary_api_keys_subsidiaries_SubsidiaryId",
                        column: x => x.SubsidiaryId,
                        principalSchema: "config",
                        principalTable: "subsidiaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_admin_users_Email",
                schema: "config",
                table: "admin_users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cache_retention_policy_UpdatedBy",
                schema: "config",
                table: "cache_retention_policy",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_config_audit_log_AdminId",
                schema: "config",
                table: "config_audit_log",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_config_audit_log_RollbackOfId",
                schema: "config",
                table: "config_audit_log",
                column: "RollbackOfId");

            migrationBuilder.CreateIndex(
                name: "IX_nrb_environment_settings_UpdatedBy",
                schema: "config",
                table: "nrb_environment_settings",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_subsidiaries_ShortCode",
                schema: "config",
                table: "subsidiaries",
                column: "ShortCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subsidiary_api_keys_CreatedBy",
                schema: "config",
                table: "subsidiary_api_keys",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_subsidiary_api_keys_KeyHash",
                schema: "config",
                table: "subsidiary_api_keys",
                column: "KeyHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subsidiary_api_keys_SubsidiaryId",
                schema: "config",
                table: "subsidiary_api_keys",
                column: "SubsidiaryId");

            migrationBuilder.CreateIndex(
                name: "IX_verification_tier_settings_UpdatedBy",
                schema: "config",
                table: "verification_tier_settings",
                column: "UpdatedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cache_retention_policy",
                schema: "config");

            migrationBuilder.DropTable(
                name: "config_audit_log",
                schema: "config");

            migrationBuilder.DropTable(
                name: "nrb_environment_settings",
                schema: "config");

            migrationBuilder.DropTable(
                name: "subsidiary_api_keys",
                schema: "config");

            migrationBuilder.DropTable(
                name: "verification_tier_settings",
                schema: "config");

            migrationBuilder.DropTable(
                name: "subsidiaries",
                schema: "config");

            migrationBuilder.DropTable(
                name: "admin_users",
                schema: "config");
        }
    }
}
