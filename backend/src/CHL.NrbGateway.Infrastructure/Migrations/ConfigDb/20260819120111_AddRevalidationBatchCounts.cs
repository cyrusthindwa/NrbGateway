using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CHL.NrbGateway.Infrastructure.Migrations.ConfigDb
{
    /// <inheritdoc />
    public partial class AddRevalidationBatchCounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeceasedCount",
                schema: "config",
                table: "revalidation_batches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ErrorCount",
                schema: "config",
                table: "revalidation_batches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ExpiredCount",
                schema: "config",
                table: "revalidation_batches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SeeNrbCount",
                schema: "config",
                table: "revalidation_batches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalCount",
                schema: "config",
                table: "revalidation_batches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ValidCount",
                schema: "config",
                table: "revalidation_batches",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeceasedCount",
                schema: "config",
                table: "revalidation_batches");

            migrationBuilder.DropColumn(
                name: "ErrorCount",
                schema: "config",
                table: "revalidation_batches");

            migrationBuilder.DropColumn(
                name: "ExpiredCount",
                schema: "config",
                table: "revalidation_batches");

            migrationBuilder.DropColumn(
                name: "SeeNrbCount",
                schema: "config",
                table: "revalidation_batches");

            migrationBuilder.DropColumn(
                name: "TotalCount",
                schema: "config",
                table: "revalidation_batches");

            migrationBuilder.DropColumn(
                name: "ValidCount",
                schema: "config",
                table: "revalidation_batches");
        }
    }
}
