using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CVision.Api.Migrations
{
    /// <inheritdoc />
    public partial class ConvertWeaknessesToJsonb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""ALTER TABLE "CandidateProfiles" DROP COLUMN "Weaknesses";""");
            migrationBuilder.Sql("""ALTER TABLE "CandidateProfiles" DROP COLUMN "Strengths";""");
            migrationBuilder.Sql("""ALTER TABLE "CandidateProfiles" DROP COLUMN "Skills";""");

            migrationBuilder.Sql("""ALTER TABLE "CandidateProfiles" ADD COLUMN "Weaknesses" jsonb;""");
            migrationBuilder.Sql("""ALTER TABLE "CandidateProfiles" ADD COLUMN "Strengths" jsonb;""");
            migrationBuilder.Sql("""ALTER TABLE "CandidateProfiles" ADD COLUMN "Skills" jsonb;""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
