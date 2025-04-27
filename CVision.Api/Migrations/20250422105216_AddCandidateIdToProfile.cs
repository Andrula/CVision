using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CVision.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCandidateIdToProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CandidateId",
                table: "CandidateProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CandidateId",
                table: "CandidateProfiles");
        }
    }
}
