using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sift.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRecruiterBureau : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BureauId",
                table: "Recruiters",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recruiters_BureauId",
                table: "Recruiters",
                column: "BureauId");

            migrationBuilder.AddForeignKey(
                name: "FK_Recruiters_Bureaus_BureauId",
                table: "Recruiters",
                column: "BureauId",
                principalTable: "Bureaus",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Recruiters_Bureaus_BureauId",
                table: "Recruiters");

            migrationBuilder.DropIndex(
                name: "IX_Recruiters_BureauId",
                table: "Recruiters");

            migrationBuilder.DropColumn(
                name: "BureauId",
                table: "Recruiters");
        }
    }
}
