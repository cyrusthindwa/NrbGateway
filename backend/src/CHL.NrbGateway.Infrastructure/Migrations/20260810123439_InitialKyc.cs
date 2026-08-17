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
                name: "individuals",
                schema: "kyc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NationalIdHash = table.Column<string>(type: "text", nullable: true),
                    NationalIdEncrypted = table.Column<string>(type: "text", nullable: true),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Surname = table.Column<string>(type: "text", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    OtherNames = table.Column<string>(type: "text", nullable: true),
                    MaidenName = table.Column<string>(type: "text", nullable: true),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: false),
                    PlaceOfBirthVillage = table.Column<string>(type: "text", nullable: true),
                    PlaceOfBirthDistrict = table.Column<string>(type: "text", nullable: true),
                    Gender = table.Column<string>(type: "text", nullable: false),
                    CivilStatus = table.Column<string>(type: "text", nullable: true),
                    Nationality = table.Column<string>(type: "text", nullable: true),
                    PhotoRef = table.Column<string>(type: "text", nullable: true),
                    FingerprintRef = table.Column<string>(type: "text", nullable: true),
                    RecordStatus = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_individuals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "organizations",
                schema: "kyc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DateEstablished = table.Column<DateOnly>(type: "date", nullable: false),
                    NatureOfBusiness = table.Column<string>(type: "text", nullable: false),
                    IsSubsidiary = table.Column<bool>(type: "boolean", nullable: false),
                    HoldingCompanyName = table.Column<string>(type: "text", nullable: true),
                    OwnershipType = table.Column<int>(type: "integer", nullable: false),
                    ForeignCountry = table.Column<string>(type: "text", nullable: true),
                    CountryOfIncorporation = table.Column<string>(type: "text", nullable: false),
                    RegistrationNumber = table.Column<string>(type: "text", nullable: false),
                    MonthlyTurnover = table.Column<decimal>(type: "numeric", nullable: true),
                    IncomeTaxNumber = table.Column<string>(type: "text", nullable: false),
                    FinancialYearEnd = table.Column<string>(type: "text", nullable: false),
                    EverInsolvent = table.Column<bool>(type: "boolean", nullable: false),
                    InsolvencyRehabilitatedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "individual_addresses",
                schema: "kyc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IndividualId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddressType = table.Column<string>(type: "text", nullable: false),
                    Line1Line2 = table.Column<string>(type: "text", nullable: true),
                    StreetNameVillage = table.Column<string>(type: "text", nullable: true),
                    TraditionalAuthorityDistrict = table.Column<string>(type: "text", nullable: true),
                    CityTownCountry = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_individual_addresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_individual_addresses_individuals_IndividualId",
                        column: x => x.IndividualId,
                        principalSchema: "kyc",
                        principalTable: "individuals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "individual_contact_details",
                schema: "kyc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IndividualId = table.Column<Guid>(type: "uuid", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    IsNrbRegisteredPhone = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_individual_contact_details", x => x.Id);
                    table.ForeignKey(
                        name: "FK_individual_contact_details_individuals_IndividualId",
                        column: x => x.IndividualId,
                        principalSchema: "kyc",
                        principalTable: "individuals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "individual_field_verification",
                schema: "kyc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IndividualId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldName = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    VerificationStatus = table.Column<string>(type: "text", nullable: false),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Superseded = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_individual_field_verification", x => x.Id);
                    table.ForeignKey(
                        name: "FK_individual_field_verification_individuals_IndividualId",
                        column: x => x.IndividualId,
                        principalSchema: "kyc",
                        principalTable: "individuals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "individual_identifications",
                schema: "kyc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IndividualId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdType = table.Column<string>(type: "text", nullable: false),
                    IdValue = table.Column<string>(type: "text", nullable: false),
                    IssuingAuthority = table.Column<string>(type: "text", nullable: true),
                    IdStatus = table.Column<string>(type: "text", nullable: true),
                    DateOfIssue = table.Column<DateOnly>(type: "date", nullable: true),
                    DateOfExpiry = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_individual_identifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_individual_identifications_individuals_IndividualId",
                        column: x => x.IndividualId,
                        principalSchema: "kyc",
                        principalTable: "individuals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "individual_next_of_kin",
                schema: "kyc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IndividualId = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstNameLastName = table.Column<string>(type: "text", nullable: false),
                    Relation = table.Column<string>(type: "text", nullable: false),
                    PhoneNumberEmail = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_individual_next_of_kin", x => x.Id);
                    table.ForeignKey(
                        name: "FK_individual_next_of_kin_individuals_IndividualId",
                        column: x => x.IndividualId,
                        principalSchema: "kyc",
                        principalTable: "individuals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "nrb_verification_events",
                schema: "kyc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IndividualId = table.Column<Guid>(type: "uuid", nullable: true),
                    PinSubmittedHash = table.Column<string>(type: "text", nullable: false),
                    Tier = table.Column<string>(type: "text", nullable: false),
                    RequestingSubsidiary = table.Column<string>(type: "text", nullable: false),
                    RequestTimestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResponseTimestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResponseStatus = table.Column<string>(type: "text", nullable: false),
                    ConfirmationToken = table.Column<string>(type: "text", nullable: true),
                    RawResponseRef = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nrb_verification_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_nrb_verification_events_individuals_IndividualId",
                        column: x => x.IndividualId,
                        principalSchema: "kyc",
                        principalTable: "individuals",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "individual_employment",
                schema: "kyc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IndividualId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployerName = table.Column<string>(type: "text", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_individual_employment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_individual_employment_individuals_IndividualId",
                        column: x => x.IndividualId,
                        principalSchema: "kyc",
                        principalTable: "individuals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_individual_employment_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "kyc",
                        principalTable: "organizations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "organization_addresses",
                schema: "kyc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddressLine1Line2 = table.Column<string>(type: "text", nullable: false),
                    TelNumberFaxNumber = table.Column<string>(type: "text", nullable: false),
                    EmailWebsite = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_addresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_organization_addresses_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "kyc",
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "organization_attachments",
                schema: "kyc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttachmentType = table.Column<int>(type: "integer", nullable: false),
                    FileRef = table.Column<string>(type: "text", nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_organization_attachments_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "kyc",
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "organization_contacts",
                schema: "kyc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactName = table.Column<string>(type: "text", nullable: false),
                    Capacity = table.Column<string>(type: "text", nullable: false),
                    TelNumberFaxNumberCellNumber = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_contacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_organization_contacts_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "kyc",
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "organization_existing_schemes",
                schema: "kyc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    InstitutionName = table.Column<string>(type: "text", nullable: false),
                    EmployerNumber = table.Column<string>(type: "text", nullable: false),
                    NumberOfEmployees = table.Column<int>(type: "integer", nullable: false),
                    SchemeType = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_existing_schemes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_organization_existing_schemes_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "kyc",
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "organization_signatories",
                schema: "kyc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    IndividualId = table.Column<Guid>(type: "uuid", nullable: false),
                    Capacity = table.Column<string>(type: "text", nullable: false),
                    SignatureRef = table.Column<string>(type: "text", nullable: false),
                    AuthorizedFrom = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_signatories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_organization_signatories_individuals_IndividualId",
                        column: x => x.IndividualId,
                        principalSchema: "kyc",
                        principalTable: "individuals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_organization_signatories_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "kyc",
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gateway_requests",
                schema: "kyc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubsidiaryId = table.Column<Guid>(type: "uuid", nullable: false),
                    IndividualId = table.Column<Guid>(type: "uuid", nullable: true),
                    ServedFrom = table.Column<string>(type: "text", nullable: false),
                    NrbVerificationEventId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResponseStatus = table.Column<string>(type: "text", nullable: false),
                    RequestTimestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gateway_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_gateway_requests_individuals_IndividualId",
                        column: x => x.IndividualId,
                        principalSchema: "kyc",
                        principalTable: "individuals",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_gateway_requests_nrb_verification_events_NrbVerificationEve~",
                        column: x => x.NrbVerificationEventId,
                        principalSchema: "kyc",
                        principalTable: "nrb_verification_events",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_gateway_requests_IndividualId",
                schema: "kyc",
                table: "gateway_requests",
                column: "IndividualId");

            migrationBuilder.CreateIndex(
                name: "IX_gateway_requests_NrbVerificationEventId",
                schema: "kyc",
                table: "gateway_requests",
                column: "NrbVerificationEventId");

            migrationBuilder.CreateIndex(
                name: "IX_individual_addresses_IndividualId",
                schema: "kyc",
                table: "individual_addresses",
                column: "IndividualId");

            migrationBuilder.CreateIndex(
                name: "IX_individual_contact_details_IndividualId",
                schema: "kyc",
                table: "individual_contact_details",
                column: "IndividualId");

            migrationBuilder.CreateIndex(
                name: "IX_individual_employment_IndividualId",
                schema: "kyc",
                table: "individual_employment",
                column: "IndividualId");

            migrationBuilder.CreateIndex(
                name: "IX_individual_employment_OrganizationId",
                schema: "kyc",
                table: "individual_employment",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_individual_field_verification_IndividualId",
                schema: "kyc",
                table: "individual_field_verification",
                column: "IndividualId");

            migrationBuilder.CreateIndex(
                name: "IX_individual_identifications_IndividualId",
                schema: "kyc",
                table: "individual_identifications",
                column: "IndividualId");

            migrationBuilder.CreateIndex(
                name: "IX_individual_next_of_kin_IndividualId",
                schema: "kyc",
                table: "individual_next_of_kin",
                column: "IndividualId");

            migrationBuilder.CreateIndex(
                name: "IX_individuals_NationalIdHash",
                schema: "kyc",
                table: "individuals",
                column: "NationalIdHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_nrb_verification_events_IndividualId",
                schema: "kyc",
                table: "nrb_verification_events",
                column: "IndividualId");

            migrationBuilder.CreateIndex(
                name: "IX_nrb_verification_events_PinSubmittedHash",
                schema: "kyc",
                table: "nrb_verification_events",
                column: "PinSubmittedHash");

            migrationBuilder.CreateIndex(
                name: "IX_organization_addresses_OrganizationId",
                schema: "kyc",
                table: "organization_addresses",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_organization_attachments_OrganizationId",
                schema: "kyc",
                table: "organization_attachments",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_organization_contacts_OrganizationId",
                schema: "kyc",
                table: "organization_contacts",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_organization_existing_schemes_OrganizationId",
                schema: "kyc",
                table: "organization_existing_schemes",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_organization_signatories_IndividualId",
                schema: "kyc",
                table: "organization_signatories",
                column: "IndividualId");

            migrationBuilder.CreateIndex(
                name: "IX_organization_signatories_OrganizationId",
                schema: "kyc",
                table: "organization_signatories",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_organizations_RegistrationNumber",
                schema: "kyc",
                table: "organizations",
                column: "RegistrationNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gateway_requests",
                schema: "kyc");

            migrationBuilder.DropTable(
                name: "individual_addresses",
                schema: "kyc");

            migrationBuilder.DropTable(
                name: "individual_contact_details",
                schema: "kyc");

            migrationBuilder.DropTable(
                name: "individual_employment",
                schema: "kyc");

            migrationBuilder.DropTable(
                name: "individual_field_verification",
                schema: "kyc");

            migrationBuilder.DropTable(
                name: "individual_identifications",
                schema: "kyc");

            migrationBuilder.DropTable(
                name: "individual_next_of_kin",
                schema: "kyc");

            migrationBuilder.DropTable(
                name: "organization_addresses",
                schema: "kyc");

            migrationBuilder.DropTable(
                name: "organization_attachments",
                schema: "kyc");

            migrationBuilder.DropTable(
                name: "organization_contacts",
                schema: "kyc");

            migrationBuilder.DropTable(
                name: "organization_existing_schemes",
                schema: "kyc");

            migrationBuilder.DropTable(
                name: "organization_signatories",
                schema: "kyc");

            migrationBuilder.DropTable(
                name: "nrb_verification_events",
                schema: "kyc");

            migrationBuilder.DropTable(
                name: "organizations",
                schema: "kyc");

            migrationBuilder.DropTable(
                name: "individuals",
                schema: "kyc");
        }
    }
}
