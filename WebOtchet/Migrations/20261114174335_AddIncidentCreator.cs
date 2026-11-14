using Doka.EntityFrameworkCore.MySql;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebOtchet.Migrations
{
    /// <inheritdoc />
    public partial class AddIncidentCreator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "INCIDENTS",
                type: "char(36)",
                nullable: false)
                .Annotation("Doka:MySql:RequiresExplicitBackfill", true)
                .Annotation("Doka:MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None);

            migrationBuilder.CreateIndex(
                name: "IX_INCIDENTS_CreatedByUserId",
                table: "INCIDENTS",
                column: "CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_INCIDENTS_USERS_CreatedByUserId",
                table: "INCIDENTS",
                column: "CreatedByUserId",
                principalTable: "USERS",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_INCIDENTS_USERS_CreatedByUserId",
                table: "INCIDENTS");

            migrationBuilder.DropIndex(
                name: "IX_INCIDENTS_CreatedByUserId",
                table: "INCIDENTS");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "INCIDENTS")
                .Annotation("Doka:MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None);
        }
    }
}
