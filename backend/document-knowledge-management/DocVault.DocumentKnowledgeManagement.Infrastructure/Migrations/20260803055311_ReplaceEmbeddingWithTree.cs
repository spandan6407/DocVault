using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocVault.DocumentKnowledgeManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceEmbeddingWithTree : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EmbeddingJson",
                table: "Documents",
                newName: "TreeJson");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TreeJson",
                table: "Documents",
                newName: "EmbeddingJson");
        }
    }
}
