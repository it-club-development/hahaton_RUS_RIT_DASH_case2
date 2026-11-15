using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WebOtchet.Data;

namespace WebOtchet.Migrations;

[DbContext(typeof(WebotchetDbContext))]
[Migration("20261115030000_AddIncidentVersion")]
public sealed class AddIncidentVersion : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE INCIDENTS ADD COLUMN IF NOT EXISTS ROW_VERSION CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'");
        migrationBuilder.Sql("UPDATE INCIDENTS SET ROW_VERSION = UUID() WHERE ROW_VERSION = '00000000-0000-0000-0000-000000000000'");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.DropColumn("ROW_VERSION", "INCIDENTS");
}
