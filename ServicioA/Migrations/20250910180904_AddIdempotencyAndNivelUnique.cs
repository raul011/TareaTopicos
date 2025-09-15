using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicioA.Migrations
{
    /// <inheritdoc />
    public partial class AddIdempotencyAndNivelUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProcessedMessages",
                columns: table => new
                {
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedMessages", x => x.MessageId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Niveles_Numero",
                table: "Niveles",
                column: "Numero",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcessedMessages");

            migrationBuilder.DropIndex(
                name: "IX_Niveles_Numero",
                table: "Niveles");
        }
    }
}
