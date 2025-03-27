using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddVPNSubscription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VPNSubscriptions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ConnectionUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubscriptionName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    InboundId = table.Column<int>(type: "int", nullable: false),
                    Reset = table.Column<int>(type: "int", nullable: false),
                    DownloadBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadBytes = table.Column<long>(type: "bigint", nullable: false),
                    PurchaseDateUnix = table.Column<long>(type: "bigint", nullable: false),
                    ExpiryDateUnix = table.Column<long>(type: "bigint", nullable: false),
                    VPNServerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreateDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDelete = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VPNSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VPNSubscriptions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VPNSubscriptions_VPNServers_VPNServerId",
                        column: x => x.VPNServerId,
                        principalTable: "VPNServers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VPNSubscriptions_UserId",
                table: "VPNSubscriptions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VPNSubscriptions_VPNServerId",
                table: "VPNSubscriptions",
                column: "VPNServerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VPNSubscriptions");
        }
    }
}
