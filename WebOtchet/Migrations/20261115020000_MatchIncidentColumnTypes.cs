using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WebOtchet.Data;

namespace WebOtchet.Migrations;

[DbContext(typeof(WebotchetDbContext))]
[Migration("20261115020000_MatchIncidentColumnTypes")]
public sealed class MatchIncidentColumnTypes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE INCIDENTS MODIFY KATEG TINYINT NOT NULL");
        migrationBuilder.Sql("ALTER TABLE INCIDENTS ADD COLUMN IF NOT EXISTS COMMON_PROSTOY_SECONDS BIGINT NULL");

        // The placeholder makes a retry safe if startup stopped between DROP and ADD.
        migrationBuilder.Sql("ALTER TABLE INCIDENTS ADD COLUMN IF NOT EXISTS COMMON_PROSTOY BIGINT NULL");
        migrationBuilder.Sql("""
            UPDATE INCIDENTS SET COMMON_PROSTOY_SECONDS = COMMON_PROSTOY
            WHERE COMMON_PROSTOY IS NOT NULL
              AND (SELECT DATA_TYPE FROM information_schema.COLUMNS
                   WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'INCIDENTS'
                     AND COLUMN_NAME = 'COMMON_PROSTOY') = 'bigint'
            """);

        migrationBuilder.Sql("ALTER TABLE INCIDENTS DROP COLUMN COMMON_PROSTOY");
        migrationBuilder.Sql("""
            ALTER TABLE INCIDENTS ADD COLUMN COMMON_PROSTOY TIME AS
                (CASE WHEN COMMON_PROSTOY_SECONDS BETWEEN -3020399 AND 3020399
                      THEN SEC_TO_TIME(COMMON_PROSTOY_SECONDS) ELSE NULL END) PERSISTENT
                AFTER PROSTOY
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException("Restore a database backup to roll back this migration.");
    }
}
