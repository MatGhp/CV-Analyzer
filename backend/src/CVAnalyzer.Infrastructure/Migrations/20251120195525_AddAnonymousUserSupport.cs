using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CVAnalyzer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAnonymousUserSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AnonymousExpiresAt",
                table: "Resumes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAnonymous",
                table: "Resumes",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnonymousExpiresAt",
                table: "Resumes");

            migrationBuilder.DropColumn(
                name: "IsAnonymous",
                table: "Resumes");
        }
    }
}
