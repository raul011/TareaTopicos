using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicioA.Migrations
{
    /// <inheritdoc />
    public partial class ResetInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Estudiantes",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$YP4gNElOt/d79qZIHbwT3eAxgSg8R.DkDofhZOnq2dH1/IytjKdSq");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Estudiantes",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$8F6jTsB1t4RoaZQnRtHh8uMzF7yUB1RMB8o6hHbnr31NZB4NdP1nK");
        }
    }
}
