using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ServicioA.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Materias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Creditos = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Materias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlanesEstudio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Anio = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanesEstudio", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlanMaterias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlanId = table.Column<int>(type: "integer", nullable: false),
                    MateriaId = table.Column<int>(type: "integer", nullable: false),
                    Semestre = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanMaterias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanMaterias_Materias_MateriaId",
                        column: x => x.MateriaId,
                        principalTable: "Materias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlanMaterias_PlanesEstudio_PlanId",
                        column: x => x.PlanId,
                        principalTable: "PlanesEstudio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Materias",
                columns: new[] { "Id", "Creditos", "Nombre" },
                values: new object[,]
                {
                    { 1, 0, "Matemáticas I" },
                    { 2, 0, "Programación I" },
                    { 3, 0, "Bases de Datos" }
                });

            migrationBuilder.InsertData(
                table: "PlanesEstudio",
                columns: new[] { "Id", "Anio", "Nombre" },
                values: new object[] { 1, 2025, "Ingeniería de Sistemas" });

            migrationBuilder.InsertData(
                table: "PlanMaterias",
                columns: new[] { "Id", "MateriaId", "PlanId", "Semestre" },
                values: new object[,]
                {
                    { 1, 1, 1, 1 },
                    { 2, 2, 1, 1 },
                    { 3, 3, 1, 2 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlanMaterias_MateriaId",
                table: "PlanMaterias",
                column: "MateriaId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanMaterias_PlanId_MateriaId",
                table: "PlanMaterias",
                columns: new[] { "PlanId", "MateriaId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlanMaterias");

            migrationBuilder.DropTable(
                name: "Materias");

            migrationBuilder.DropTable(
                name: "PlanesEstudio");
        }
    }
}
