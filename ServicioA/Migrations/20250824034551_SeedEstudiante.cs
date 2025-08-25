using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicioA.Migrations
{
    /// <inheritdoc />
    public partial class SeedEstudiante : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Estudiantes",
                columns: new[] { "Id", "Apellido", "CarnetIdentidad", "Correo", "Nombre", "NumeroRegistro", "PasswordHash" },
                values: new object[] { 1, "Fernandez", "7894561", "maria@uagrm.edu.bo", "Maria", "20251234", "$2a$11$8F6jTsB1t4RoaZQnRtHh8uMzF7yUB1RMB8o6hHbnr31NZB4NdP1nK" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Estudiantes",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
