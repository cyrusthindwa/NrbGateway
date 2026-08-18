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
                name: "companies",
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
                    table.PrimaryKey("PK_companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "monthly_usage_reports",
                schema: "config",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodYear = table.Column<int>(type: "integer", nullable: false),
                    PeriodMonth = table.Column<int>(type: "integer", nullable: false),
                    RequestCount = table.Column<int>(type: "integer", nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_monthly_usage_reports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "nrb_health_checks",
                schema: "config",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CheckedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsUp = table.Column<bool>(type: "boolean", nullable: false),
                    LatencyMs = table.Column<int>(type: "integer", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nrb_health_checks", x => x.Id);
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
                name: "notification_channels",
                schema: "config",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelType = table.Column<string>(type: "text", nullable: false),
                    Target = table.Column<string>(type: "text", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_channels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notification_channels_admin_users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "config",
                        principalTable: "admin_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "nrb_downtime_incidents",
                schema: "config",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DetectedBy = table.Column<string>(type: "text", nullable: false),
                    Notified = table.Column<bool>(type: "boolean", nullable: false),
                    ResolvedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nrb_downtime_incidents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_nrb_downtime_incidents_admin_users_ResolvedBy",
                        column: x => x.ResolvedBy,
                        principalSchema: "config",
                        principalTable: "admin_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
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
                name: "revalidation_batches",
                schema: "config",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TriggerType = table.Column<string>(type: "text", nullable: false),
                    InitiatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_revalidation_batches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_revalidation_batches_admin_users_InitiatedBy",
                        column: x => x.InitiatedBy,
                        principalSchema: "config",
                        principalTable: "admin_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "verification_tier_settings",
                schema: "config",
                columns: table => new
                {
                    Tier = table.Column<string>(type: "text", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    CostPerRequest = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
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
                name: "billing_invoices",
                schema: "config",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PaidAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    GeneratedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_billing_invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_billing_invoices_admin_users_GeneratedBy",
                        column: x => x.GeneratedBy,
                        principalSchema: "config",
                        principalTable: "admin_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_billing_invoices_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "config",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "projects",
                schema: "config",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ShortCode = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_projects_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "config",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_api_keys",
                schema: "config",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_project_api_keys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_api_keys_admin_users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "config",
                        principalTable: "admin_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_project_api_keys_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "config",
                        principalTable: "projects",
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
                name: "IX_billing_invoices_CompanyId",
                schema: "config",
                table: "billing_invoices",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_billing_invoices_GeneratedBy",
                schema: "config",
                table: "billing_invoices",
                column: "GeneratedBy");

            migrationBuilder.CreateIndex(
                name: "IX_companies_ShortCode",
                schema: "config",
                table: "companies",
                column: "ShortCode",
                unique: true);

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
                name: "IX_monthly_usage_reports_ProjectId_PeriodYear_PeriodMonth",
                schema: "config",
                table: "monthly_usage_reports",
                columns: new[] { "ProjectId", "PeriodYear", "PeriodMonth" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_channels_CreatedBy",
                schema: "config",
                table: "notification_channels",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_nrb_downtime_incidents_ResolvedBy",
                schema: "config",
                table: "nrb_downtime_incidents",
                column: "ResolvedBy");

            migrationBuilder.CreateIndex(
                name: "IX_nrb_environment_settings_UpdatedBy",
                schema: "config",
                table: "nrb_environment_settings",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_project_api_keys_CreatedBy",
                schema: "config",
                table: "project_api_keys",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_project_api_keys_KeyHash",
                schema: "config",
                table: "project_api_keys",
                column: "KeyHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_project_api_keys_ProjectId",
                schema: "config",
                table: "project_api_keys",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_projects_CompanyId",
                schema: "config",
                table: "projects",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_projects_ShortCode",
                schema: "config",
                table: "projects",
                column: "ShortCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_revalidation_batches_InitiatedBy",
                schema: "config",
                table: "revalidation_batches",
                column: "InitiatedBy");

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
                name: "billing_invoices",
                schema: "config");

            migrationBuilder.DropTable(
                name: "config_audit_log",
                schema: "config");

            migrationBuilder.DropTable(
                name: "monthly_usage_reports",
                schema: "config");

            migrationBuilder.DropTable(
                name: "notification_channels",
                schema: "config");

            migrationBuilder.DropTable(
                name: "nrb_downtime_incidents",
                schema: "config");

            migrationBuilder.DropTable(
                name: "nrb_environment_settings",
                schema: "config");

            migrationBuilder.DropTable(
                name: "nrb_health_checks",
                schema: "config");

            migrationBuilder.DropTable(
                name: "project_api_keys",
                schema: "config");

            migrationBuilder.DropTable(
                name: "revalidation_batches",
                schema: "config");

            migrationBuilder.DropTable(
                name: "verification_tier_settings",
                schema: "config");

            migrationBuilder.DropTable(
                name: "projects",
                schema: "config");

            migrationBuilder.DropTable(
                name: "admin_users",
                schema: "config");

            migrationBuilder.DropTable(
                name: "companies",
                schema: "config");
        }
    }
}
