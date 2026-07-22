using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KaraW3B.Server.Songs.Core.Migrations
{
    /// <inheritdoc />
    public partial class LibraryAnalyzeStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsAnalyzing",
                table: "Libraries",
                newName: "AnalyzeStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AnalyzeStatus",
                table: "Libraries",
                newName: "IsAnalyzing");
        }
    }
}
