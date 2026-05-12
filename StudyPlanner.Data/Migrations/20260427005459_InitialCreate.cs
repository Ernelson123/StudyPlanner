using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudyPlanner.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
#pragma warning disable CA1062 // Validate arguments of public methods
            migrationBuilder.CreateTable(
                name: "StudySessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SubjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DurationTicks = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudySessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Subjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    PriorityWeight = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subjects", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.DropTable(
                name: "StudySessions");

            migrationBuilder.DropTable(
                name: "Subjects");
        }
    }
}
