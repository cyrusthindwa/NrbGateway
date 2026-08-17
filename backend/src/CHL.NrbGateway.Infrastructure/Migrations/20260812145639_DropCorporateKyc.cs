using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CHL.NrbGateway.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropCorporateKyc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "organizations",
                schema: "kyc");

            migrationBuilder.RenameColumn(
                name: "Title",
                schema: "kyc",
                table: "individuals",
                newName: "CardStatus");

            migrationBuilder.RenameColumn(
                name: "PlaceOfBirthVillage",
                schema: "kyc",
                table: "individuals",
                newName: "TelephoneNumber");

            migrationBuilder.RenameColumn(
                name: "PlaceOfBirthDistrict",
                schema: "kyc",
                table: "individuals",
                newName: "ResidentialAddress");

            migrationBuilder.RenameColumn(
                name: "Nationality",
                schema: "kyc",
                table: "individuals",
                newName: "MaritalStatus");

            migrationBuilder.RenameColumn(
                name: "MaidenName",
                schema: "kyc",
                table: "individuals",
                newName: "FingerPosition");

            migrationBuilder.RenameColumn(
                name: "CivilStatus",
                schema: "kyc",
                table: "individuals",
                newName: "BirthDistrict");

            migrationBuilder.AddColumn<DateOnly>(
                name: "ExpiryDate",
                schema: "kyc",
                table: "individuals",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "IssueDate",
                schema: "kyc",
                table: "individuals",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastRevalidatedAt",
                schema: "kyc",
                table: "individuals",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                schema: "kyc",
                table: "individuals");

            migrationBuilder.DropColumn(
                name: "IssueDate",
                schema: "kyc",
                table: "individuals");

            migrationBuilder.DropColumn(
                name: "LastRevalidatedAt",
                schema: "kyc",
                table: "individuals");

            migrationBuilder.RenameColumn(
                name: "TelephoneNumber",
                schema: "kyc",
                table: "individuals",
                newName: "PlaceOfBirthVillage");

            migrationBuilder.RenameColumn(
                name: "ResidentialAddress",
                schema: "kyc",
                table: "individuals",
                newName: "PlaceOfBirthDistrict");

            migrationBuilder.RenameColumn(
                name: "MaritalStatus",
                schema: "kyc",
                table: "individuals",
                newName: "Nationality");

            migrationBuilder.RenameColumn(
                name: "FingerPosition",
                schema: "kyc",
                table: "individuals",
                newName: "MaidenName");

            migrationBuilder.RenameColumn(
                name: "CardStatus",
                schema: "kyc",
                table: "individuals",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "BirthDistrict",
                schema: "kyc",
                table: "individuals",
                newName: "CivilStatus");

            migrationBuilder.CreateTable(
                name: "individual_addresses",
                schema: "kyc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IndividualId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddressType = table.Column<string>(type: "text", nullable: false),
                    CityTownCountry = table.Column<string>(type: "text", nullable: true),
                    Line1Line2 = table.Column<string>(type: "text", nullable: true),
                    StreetNameVillage = table.Column<string>(type: "text", nullable: true),
                    TraditionalAuthorityDistrict = table.Column<string>(type: "text", nullable: true)
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
                    Email = table.Column<string>(type: "text", nullable: true),
                    IsNrbRegisteredPhone = table.Column<bool>(type: "boolean", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false)
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
                name: "individual_next_of_kin",
                schema: "kyc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IndividualId = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstNameLastName = table.Column<string>(type: "text", nullable: false),
                    PhoneNumberEmail = table.Column<string>(type: "text", nullable: true),
                    Relation = table.Column<string>(type: "text", nullable: false)
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
                name: "organizations",
                schema: "kyc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CountryOfIncorporation = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DateEstablished = table.Column<DateOnly>(type: "date", nullable: false),
                    EverInsolvent = table.Column<bool>(type: "boolean", nullable: false),
                    FinancialYearEnd = table.Column<string>(type: "text", nullable: false),
                    ForeignCountry = table.Column<string>(type: "text", nullable: true),
                    HoldingCompanyName = table.Column<string>(type: "text", nullable: true),
                    IncomeTaxNumber = table.Column<string>(type: "text", nullable: false),
                    InsolvencyRehabilitatedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsSubsidiary = table.Column<bool>(type: "boolean", nullable: false),
                    MonthlyTurnover = table.Column<decimal>(type: "numeric", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    NatureOfBusiness = table.Column<string>(type: "text", nullable: false),
                    OwnershipType = table.Column<int>(type: "integer", nullable: false),
                    RegistrationNumber = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "individual_employment",
                schema: "kyc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IndividualId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    EmployerName = table.Column<string>(type: "text", nullable: false)
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
                    EmailWebsite = table.Column<string>(type: "text", nullable: false),
                    TelNumberFaxNumber = table.Column<string>(type: "text", nullable: false)
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
                    Capacity = table.Column<string>(type: "text", nullable: false),
                    ContactName = table.Column<string>(type: "text", nullable: false),
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
                    EmployerNumber = table.Column<string>(type: "text", nullable: false),
                    InstitutionName = table.Column<string>(type: "text", nullable: false),
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
                    IndividualId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorizedFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    Capacity = table.Column<string>(type: "text", nullable: false),
                    SignatureRef = table.Column<string>(type: "text", nullable: false)
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
                name: "IX_individual_next_of_kin_IndividualId",
                schema: "kyc",
                table: "individual_next_of_kin",
                column: "IndividualId");

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
    }
}
