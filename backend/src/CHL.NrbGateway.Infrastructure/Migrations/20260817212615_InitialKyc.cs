using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CHL.NrbGateway.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialKyc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "kyc");

            migrationBuilder.CreateTable(
                name: "identity_lookup",
                schema: "kyc",
                columns: table => new
                {
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    NationalIdHash = table.Column<string>(type: "text", nullable: false),
                    NationalIdEncrypted = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_identity_lookup", x => x.SubjectId);
                });

            migrationBuilder.CreateTable(
                name: "individuals",
                schema: "kyc",
                columns: table => new
                {
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Surname = table.Column<string>(type: "text", nullable: true),
                    FirstName = table.Column<string>(type: "text", nullable: true),
                    OtherNames = table.Column<string>(type: "text", nullable: true),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    Gender = table.Column<string>(type: "text", nullable: true),
                    Nationality = table.Column<string>(type: "text", nullable: true),
                    CivilStatus = table.Column<string>(type: "text", nullable: true),
                    BirthDistrict = table.Column<string>(type: "text", nullable: true),
                    ResidenceAddress = table.Column<string>(type: "text", nullable: true),
                    NrbRegisteredPhone = table.Column<string>(type: "text", nullable: true),
                    IdDateOfIssue = table.Column<DateOnly>(type: "date", nullable: true),
                    IdDateOfExpiry = table.Column<DateOnly>(type: "date", nullable: true),
                    CardStatus = table.Column<string>(type: "text", nullable: false),
                    MiddlewareStatus = table.Column<string>(type: "text", nullable: true),
                    LastCardCheckAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastMiddlewareCheckAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_individuals", x => x.SubjectId);
                    table.ForeignKey(
                        name: "FK_individuals_identity_lookup_SubjectId",
                        column: x => x.SubjectId,
                        principalSchema: "kyc",
                        principalTable: "identity_lookup",
                        principalColumn: "SubjectId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "nrb_field_check_results",
                schema: "kyc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    FieldName = table.Column<string>(type: "text", nullable: false),
                    Result = table.Column<string>(type: "text", nullable: false),
                    Tier = table.Column<string>(type: "text", nullable: false),
                    CheckedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nrb_field_check_results", x => x.Id);
                    table.ForeignKey(
                        name: "FK_nrb_field_check_results_identity_lookup_SubjectId",
                        column: x => x.SubjectId,
                        principalSchema: "kyc",
                        principalTable: "identity_lookup",
                        principalColumn: "SubjectId");
                });

            migrationBuilder.CreateTable(
                name: "nrb_verification_events",
                schema: "kyc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    PinSubmittedHash = table.Column<string>(type: "text", nullable: false),
                    PinSubmittedEncrypted = table.Column<string>(type: "text", nullable: true),
                    Tier = table.Column<string>(type: "text", nullable: false),
                    RequestingProjectCode = table.Column<string>(type: "text", nullable: false),
                    ResponseMode = table.Column<string>(type: "text", nullable: false),
                    TriggerSource = table.Column<string>(type: "text", nullable: false),
                    RequestTimestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResponseTimestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResponseStatus = table.Column<string>(type: "text", nullable: false),
                    ConfirmationToken = table.Column<string>(type: "text", nullable: true),
                    RawResponseRef = table.Column<string>(type: "text", nullable: true),
                    RevalidationBatchId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nrb_verification_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_nrb_verification_events_identity_lookup_SubjectId",
                        column: x => x.SubjectId,
                        principalSchema: "kyc",
                        principalTable: "identity_lookup",
                        principalColumn: "SubjectId");
                });

            migrationBuilder.CreateTable(
                name: "individual_documents",
                schema: "kyc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    BlobFormat = table.Column<string>(type: "text", nullable: true),
                    FingerPosition = table.Column<string>(type: "text", nullable: true),
                    BlobRef = table.Column<string>(type: "text", nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_individual_documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_individual_documents_individuals_SubjectId",
                        column: x => x.SubjectId,
                        principalSchema: "kyc",
                        principalTable: "individuals",
                        principalColumn: "SubjectId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "individual_source_values",
                schema: "kyc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldName = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    ObservedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_individual_source_values", x => x.Id);
                    table.ForeignKey(
                        name: "FK_individual_source_values_individuals_SubjectId",
                        column: x => x.SubjectId,
                        principalSchema: "kyc",
                        principalTable: "individuals",
                        principalColumn: "SubjectId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gateway_requests",
                schema: "kyc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    ServedFrom = table.Column<string>(type: "text", nullable: false),
                    NrbVerificationEventId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResponseStatus = table.Column<string>(type: "text", nullable: false),
                    CostIncurred = table.Column<decimal>(type: "numeric", nullable: true),
                    RequestTimestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gateway_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_gateway_requests_identity_lookup_SubjectId",
                        column: x => x.SubjectId,
                        principalSchema: "kyc",
                        principalTable: "identity_lookup",
                        principalColumn: "SubjectId");
                    table.ForeignKey(
                        name: "FK_gateway_requests_nrb_verification_events_NrbVerificationEve~",
                        column: x => x.NrbVerificationEventId,
                        principalSchema: "kyc",
                        principalTable: "nrb_verification_events",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_gateway_requests_NrbVerificationEventId",
                schema: "kyc",
                table: "gateway_requests",
                column: "NrbVerificationEventId");

            migrationBuilder.CreateIndex(
                name: "IX_gateway_requests_SubjectId",
                schema: "kyc",
                table: "gateway_requests",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_identity_lookup_NationalIdHash",
                schema: "kyc",
                table: "identity_lookup",
                column: "NationalIdHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_individual_documents_SubjectId",
                schema: "kyc",
                table: "individual_documents",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_individual_source_values_SubjectId",
                schema: "kyc",
                table: "individual_source_values",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_nrb_field_check_results_SubjectId",
                schema: "kyc",
                table: "nrb_field_check_results",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_nrb_verification_events_PinSubmittedHash",
                schema: "kyc",
                table: "nrb_verification_events",
                column: "PinSubmittedHash");

            migrationBuilder.CreateIndex(
                name: "IX_nrb_verification_events_SubjectId",
                schema: "kyc",
                table: "nrb_verification_events",
                column: "SubjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gateway_requests",
                schema: "kyc");

            migrationBuilder.DropTable(
                name: "individual_documents",
                schema: "kyc");

            migrationBuilder.DropTable(
                name: "individual_source_values",
                schema: "kyc");

            migrationBuilder.DropTable(
                name: "nrb_field_check_results",
                schema: "kyc");

            migrationBuilder.DropTable(
                name: "nrb_verification_events",
                schema: "kyc");

            migrationBuilder.DropTable(
                name: "individuals",
                schema: "kyc");

            migrationBuilder.DropTable(
                name: "identity_lookup",
                schema: "kyc");
        }
    }
}
