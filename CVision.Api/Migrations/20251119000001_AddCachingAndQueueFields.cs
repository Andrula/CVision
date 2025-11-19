using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CVision.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCachingAndQueueFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ParsedAt",
                table: "Candidates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileHash",
                table: "Candidates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Candidates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "Candidates",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Candidates_FileHash_JobId",
                table: "Candidates",
                columns: new[] { "FileHash", "JobId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Candidates_FileHash_JobId",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "ParsedAt",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "FileHash",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "Candidates");
        }
    }
}
