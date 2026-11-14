using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WebOtchet.Data;

namespace WebOtchet.Migrations;

[DbContext(typeof(WebotchetDbContext))]
[Migration("20261115010000_AlignIncidentStorage")]
public sealed class AlignIncidentStorage : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // MariaDB 10.1 may initialize the schema with a latin1 default.
        // Convert both sides of the foreign key before comparing Cyrillic values.
        migrationBuilder.Sql("ALTER DATABASE CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci");
        migrationBuilder.Sql("ALTER TABLE INCIDENTS DROP FOREIGN KEY IF EXISTS FK_INCIDENTS_USERS_CreatedByUserId");
        migrationBuilder.Sql("ALTER TABLE USERS CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci");
        migrationBuilder.Sql("ALTER TABLE INCIDENTS CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci");
        migrationBuilder.Sql("ALTER TABLE INCIDENTS ADD CONSTRAINT FK_INCIDENTS_USERS_CreatedByUserId FOREIGN KEY (CreatedByUserId) REFERENCES USERS (ID) ON DELETE RESTRICT");

        migrationBuilder.Sql("UPDATE INCIDENTS SET NUMB_RBS = '0' WHERE CAST(NUMB_RBS AS CHAR CHARACTER SET utf8mb4) = 'С'");
        migrationBuilder.Sql("ALTER TABLE INCIDENTS MODIFY NUMB_RBS INT NOT NULL");

        // DDL commits independently in MariaDB 10.1. Inspect the actual column
        // before replaying the conversion after a startup interrupted by DDL.
        migrationBuilder.Sql("ALTER TABLE INCIDENTS ADD COLUMN IF NOT EXISTS COMMON_PROSTOY_SECONDS BIGINT NULL");
        migrationBuilder.Sql("UPDATE INCIDENTS SET COMMON_PROSTOY_SECONDS = IF((SELECT DATA_TYPE = 'time' FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'INCIDENTS' AND COLUMN_NAME = 'COMMON_PROSTOY'), TIME_TO_SEC(COMMON_PROSTOY), COMMON_PROSTOY)");
        migrationBuilder.Sql("ALTER TABLE INCIDENTS DROP COLUMN COMMON_PROSTOY, CHANGE COLUMN COMMON_PROSTOY_SECONDS COMMON_PROSTOY BIGINT NULL");

        migrationBuilder.Sql("UPDATE INCIDENTS SET END_TIME = '0000-00-00 00:00:00' WHERE END_TIME IS NULL");
        migrationBuilder.Sql("CREATE TRIGGER IF NOT EXISTS INCIDENTS_OPEN_INSERT BEFORE INSERT ON INCIDENTS FOR EACH ROW SET NEW.END_TIME = COALESCE(NEW.END_TIME, '0000-00-00 00:00:00')");
        migrationBuilder.Sql("CREATE TRIGGER IF NOT EXISTS INCIDENTS_OPEN_UPDATE BEFORE UPDATE ON INCIDENTS FOR EACH ROW SET NEW.END_TIME = COALESCE(NEW.END_TIME, '0000-00-00 00:00:00')");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Returning to TIME could discard valid durations over 838:59:59.
        throw new NotSupportedException("Restore a database backup to roll back this migration.");
    }
}

