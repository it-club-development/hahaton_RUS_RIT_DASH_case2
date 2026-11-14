using System;
using Doka.EntityFrameworkCore.MySql;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebOtchet.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "INCIDENTS",
                columns: table => new
                {
                    ID = table.Column<string>(type: "char(36)", nullable: false)
                        .Annotation("Doka:MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None),
                    PW = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: false)
                        .Annotation("Doka:MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None),
                    NAPR = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: false)
                        .Annotation("Doka:MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None),
                    TEHC = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: false)
                        .Annotation("Doka:MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None),
                    KATEG = table.Column<byte>(type: "tinyint unsigned", nullable: false)
                        .Annotation("Doka:MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None),
                    NETW_TYPE = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: false)
                        .Annotation("Doka:MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None),
                    SERV_ELEM = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: false)
                        .Annotation("Doka:MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None),
                    NUMB_RBS = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("Doka:MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None),
                    EQIP_TYPE = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: false)
                        .Annotation("Doka:MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None),
                    ALARM_TYPE = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: false)
                        .Annotation("Doka:MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None),
                    DESCR = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: false)
                        .Annotation("Doka:MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None),
                    BEGIN_TIME = table.Column<DateTime>(type: "datetime", nullable: false)
                        .Annotation("Doka:MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None),
                    END_TIME = table.Column<DateTime>(type: "datetime", nullable: true)
                        .Annotation("Doka:MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None),
                    AKB = table.Column<TimeSpan>(type: "time", nullable: true)
                        .Annotation("Doka:MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None),
                    DGU = table.Column<TimeSpan>(type: "time", nullable: true)
                        .Annotation("Doka:MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None),
                    LENGHT = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: false)
                        .Annotation("Doka:MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None),
                    PROSTOY = table.Column<TimeSpan>(type: "time", nullable: true)
                        .Annotation("Doka:MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None),
                    COMMON_PROSTOY = table.Column<TimeSpan>(type: "time", nullable: true)
                        .Annotation("Doka:MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None),
                    ALARM_WHO = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: false)
                        .Annotation("Doka:MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None),
                    DESC_NOTICE = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: false)
                        .Annotation("Doka:MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_INCIDENTS", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "USERS",
                columns: table => new
                {
                    ID = table.Column<string>(type: "char(36)", nullable: false)
                        .Annotation("Doka:MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None),
                    USERNAME = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("Doka:MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None),
                    PASSWORD_HASH = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("Doka:MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None),
                    ROLE = table.Column<byte>(type: "tinyint unsigned", nullable: false)
                        .Annotation("Doka:MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USERS", x => x.ID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_INCIDENTS_BEGIN_TIME",
                table: "INCIDENTS",
                column: "BEGIN_TIME");

            migrationBuilder.CreateIndex(
                name: "UX_USERS_USERNAME",
                table: "USERS",
                column: "USERNAME",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "INCIDENTS");

            migrationBuilder.DropTable(
                name: "USERS");
        }
    }
}
