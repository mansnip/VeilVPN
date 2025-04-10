using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataLayer.Migrations
{
    /// <inheritdoc />
    public partial class _002 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DiscountPercent",
                table: "Invoices",
                newName: "PlanDiscountPercent");

            migrationBuilder.RenameColumn(
                name: "DiscountAmount",
                table: "Invoices",
                newName: "PlanDiscountAmount");

            migrationBuilder.AddColumn<string>(
                name: "AppliedDiscountCode",
                table: "Invoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AppliedDiscountCodeId",
                table: "Invoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CouponDiscountAmount",
                table: "Invoices",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CouponDiscountPercent",
                table: "Invoices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DiscountCodes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DiscountPercent = table.Column<int>(type: "int", nullable: false),
                    MaxUses = table.Column<int>(type: "int", nullable: false),
                    CurrentUses = table.Column<int>(type: "int", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscountCodes", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscountCodes");

            migrationBuilder.DropColumn(
                name: "AppliedDiscountCode",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "AppliedDiscountCodeId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CouponDiscountAmount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CouponDiscountPercent",
                table: "Invoices");

            migrationBuilder.RenameColumn(
                name: "PlanDiscountPercent",
                table: "Invoices",
                newName: "DiscountPercent");

            migrationBuilder.RenameColumn(
                name: "PlanDiscountAmount",
                table: "Invoices",
                newName: "DiscountAmount");
        }
    }
}
