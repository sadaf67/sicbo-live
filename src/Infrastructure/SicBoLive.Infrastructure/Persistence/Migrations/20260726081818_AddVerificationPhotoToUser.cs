using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SicBoLive.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVerificationPhotoToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "VerificationPhoto",
                table: "AspNetUsers",
                type: "BLOB",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VerificationPhoto",
                table: "AspNetUsers");
        }
    }
}
