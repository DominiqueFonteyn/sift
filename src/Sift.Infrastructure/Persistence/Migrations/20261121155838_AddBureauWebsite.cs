using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sift.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBureauWebsite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "Bureaus",
                type: "TEXT",
                maxLength: 2048,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Website",
                table: "Bureaus");
        }
    }
}
