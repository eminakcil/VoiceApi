using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoiceApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IsMuted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMuted",
                table: "Sections",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OriginalAudioPath",
                table: "Sections",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TranslatedAudioPath",
                table: "Sections",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsMuted",
                table: "Sections");

            migrationBuilder.DropColumn(
                name: "OriginalAudioPath",
                table: "Sections");

            migrationBuilder.DropColumn(
                name: "TranslatedAudioPath",
                table: "Sections");
        }
    }
}
