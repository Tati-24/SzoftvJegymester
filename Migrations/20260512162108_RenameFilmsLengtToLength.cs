using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AspNetServer.Migrations
{
    /// <inheritdoc />
    public partial class RenameFilmsLengtToLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Lengt",
                table: "Films",
                newName: "Length");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Length",
                table: "Films",
                newName: "Lengt");
        }
    }
}
