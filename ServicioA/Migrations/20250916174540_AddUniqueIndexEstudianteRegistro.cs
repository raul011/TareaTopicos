using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicioA.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexEstudianteRegistro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Estudiantes_Registro",
                table: "Estudiantes",
                column: "Registro",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Estudiantes_Registro",
                table: "Estudiantes");
        }
    }
}
