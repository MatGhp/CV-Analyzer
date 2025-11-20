using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CVAnalyzer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPromptTemplatesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PromptTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TaskType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Environment = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromptTemplates", x => x.Id);
                    table.CheckConstraint("CK_PromptTemplates_Environment", "[Environment] IN ('Development', 'Test', 'Production')");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PromptTemplates_Active_Lookup",
                table: "PromptTemplates",
                columns: new[] { "Environment", "AgentType", "TaskType" },
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_PromptTemplates_AgentTask_Version",
                table: "PromptTemplates",
                columns: new[] { "AgentType", "TaskType", "Environment", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PromptTemplates");
        }
    }
}
