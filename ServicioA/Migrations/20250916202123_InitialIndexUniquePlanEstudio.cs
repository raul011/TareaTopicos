using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicioA.Migrations
{
    /// <inheritdoc />
    public partial class InitialIndexUniquePlanEstudio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PlanesEstudio_Codigo",
                table: "PlanesEstudio",
                column: "Codigo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PlanesEstudio_Codigo",
                table: "PlanesEstudio");
        }
    }
}
