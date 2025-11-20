using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CVAnalyzer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAnonymousCleanupIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add composite index for efficient cleanup queries
            // WHERE IsAnonymous = 1 AND AnonymousExpiresAt < @cutoffDate
            migrationBuilder.CreateIndex(
                name: "IX_Resumes_IsAnonymous_AnonymousExpiresAt",
                table: "Resumes",
                columns: new[] { "IsAnonymous", "AnonymousExpiresAt" },
                filter: "IsAnonymous = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Resumes_IsAnonymous_AnonymousExpiresAt",
                table: "Resumes");
        }
    }
}
