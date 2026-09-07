using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SicBoLive.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRestrictBigSmallOddEvenBetsToGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RestrictBigSmallOddEvenBets",
                table: "Groups",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RestrictBigSmallOddEvenBets",
                table: "Groups");
        }
    }
}
