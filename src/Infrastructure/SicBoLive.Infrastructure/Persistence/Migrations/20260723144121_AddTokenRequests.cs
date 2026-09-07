using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SicBoLive.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTokenRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TokenRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlayerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    RespondedByDealerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RespondedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TokenRequests_GroupId_Status",
                table: "TokenRequests",
                columns: new[] { "GroupId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TokenRequests");
        }
    }
}
