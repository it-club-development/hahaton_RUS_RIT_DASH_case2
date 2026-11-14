using Doka.EntityFrameworkCore.MySql;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebOtchet.Migrations
{
    /// <inheritdoc />
    public partial class FIXED : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CreatedByUserId",
                table: "INCIDENTS",
                type: "char(36)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "char(36)")
                .Annotation("Doka:MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None)
                .OldAnnotation("Doka:MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The preceding migration now also leaves the column nullable.
        }
    }
}
