using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SicBoLive.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTokenDenominations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TokenDenominations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TokenDenominations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Color = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Value = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenDenominations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TokenDenominations_GroupId",
                table: "TokenDenominations",
                column: "GroupId");
        }
    }
}
