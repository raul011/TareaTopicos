using System;
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
                name: "Aulas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "text", nullable: false),
                    Capacidad = table.Column<int>(type: "integer", nullable: false),
                    Ubicacion = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Aulas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Carreras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carreras", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Docentes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Registro = table.Column<string>(type: "text", nullable: false),
                    Ci = table.Column<string>(type: "text", nullable: false),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Telefono = table.Column<string>(type: "text", nullable: true),
                    Estado = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Docentes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Horarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Dia = table.Column<string>(type: "text", nullable: false),
                    HoraInicio = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    HoraFin = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Horarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Niveles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Numero = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Niveles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PeriodosAcademicos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Gestion = table.Column<string>(type: "text", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeriodosAcademicos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Estudiantes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Registro = table.Column<string>(type: "text", nullable: false),
                    Ci = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Telefono = table.Column<string>(type: "text", nullable: true),
                    Direccion = table.Column<string>(type: "text", nullable: true),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    CarreraId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Estudiantes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Estudiantes_Carreras_CarreraId",
                        column: x => x.CarreraId,
                        principalTable: "Carreras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlanesEstudio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    CarreraId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanesEstudio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanesEstudio_Carreras_CarreraId",
                        column: x => x.CarreraId,
                        principalTable: "Carreras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Materias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "text", nullable: false),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Creditos = table.Column<int>(type: "integer", nullable: false),
                    NivelId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Materias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Materias_Niveles_NivelId",
                        column: x => x.NivelId,
                        principalTable: "Niveles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Inscripciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    EstudianteId = table.Column<int>(type: "integer", nullable: false),
                    PeriodoId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inscripciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Inscripciones_Estudiantes_EstudianteId",
                        column: x => x.EstudianteId,
                        principalTable: "Estudiantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Inscripciones_PeriodosAcademicos_PeriodoId",
                        column: x => x.PeriodoId,
                        principalTable: "PeriodosAcademicos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GruposMaterias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Grupo = table.Column<string>(type: "text", nullable: false),
                    Cupo = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    MateriaId = table.Column<int>(type: "integer", nullable: false),
                    DocenteId = table.Column<int>(type: "integer", nullable: false),
                    PeriodoId = table.Column<int>(type: "integer", nullable: false),
                    HorarioId = table.Column<int>(type: "integer", nullable: true),
                    AulaId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GruposMaterias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GruposMaterias_Aulas_AulaId",
                        column: x => x.AulaId,
                        principalTable: "Aulas",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GruposMaterias_Docentes_DocenteId",
                        column: x => x.DocenteId,
                        principalTable: "Docentes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GruposMaterias_Horarios_HorarioId",
                        column: x => x.HorarioId,
                        principalTable: "Horarios",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GruposMaterias_Materias_MateriaId",
                        column: x => x.MateriaId,
                        principalTable: "Materias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GruposMaterias_PeriodosAcademicos_PeriodoId",
                        column: x => x.PeriodoId,
                        principalTable: "PeriodosAcademicos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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

            migrationBuilder.CreateTable(
                name: "Prerequisitos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Tipo = table.Column<string>(type: "text", nullable: false),
                    NotaMin = table.Column<decimal>(type: "numeric", nullable: false),
                    MateriaId = table.Column<int>(type: "integer", nullable: false),
                    MateriaPrerequisitoId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prerequisitos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Prerequisitos_Materias_MateriaId",
                        column: x => x.MateriaId,
                        principalTable: "Materias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Prerequisitos_Materias_MateriaPrerequisitoId",
                        column: x => x.MateriaPrerequisitoId,
                        principalTable: "Materias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DetallesInscripciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "text", nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    InscripcionId = table.Column<int>(type: "integer", nullable: false),
                    GrupoMateriaId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesInscripciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetallesInscripciones_GruposMaterias_GrupoMateriaId",
                        column: x => x.GrupoMateriaId,
                        principalTable: "GruposMaterias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DetallesInscripciones_Inscripciones_InscripcionId",
                        column: x => x.InscripcionId,
                        principalTable: "Inscripciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HistorialesAcademicos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Intento = table.Column<int>(type: "integer", nullable: false),
                    UltimaNota = table.Column<decimal>(type: "numeric", nullable: true),
                    Aprobado = table.Column<bool>(type: "boolean", nullable: false),
                    FechaAprobacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DetalleInscripcionId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialesAcademicos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialesAcademicos_DetallesInscripciones_DetalleInscripc~",
                        column: x => x.DetalleInscripcionId,
                        principalTable: "DetallesInscripciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Aulas",
                columns: new[] { "Id", "Capacidad", "Codigo", "Ubicacion" },
                values: new object[,]
                {
                    { 1, 40, "A101", "Bloque A" },
                    { 2, 35, "B201", "Bloque B" },
                    { 3, 50, "C301", "Bloque C" }
                });

            migrationBuilder.InsertData(
                table: "Carreras",
                columns: new[] { "Id", "Nombre" },
                values: new object[,]
                {
                    { 1, "Ingeniería de Sistemas" },
                    { 2, "Ingeniería Industrial" },
                    { 3, "Administración de Empresas" }
                });

            migrationBuilder.InsertData(
                table: "Docentes",
                columns: new[] { "Id", "Ci", "Estado", "Nombre", "Registro", "Telefono" },
                values: new object[,]
                {
                    { 1, "123456", "ACTIVO", "Juan Pérez", "DOC001", "70011111" },
                    { 2, "789012", "ACTIVO", "María Gómez", "DOC002", "70022222" }
                });

            migrationBuilder.InsertData(
                table: "Horarios",
                columns: new[] { "Id", "Dia", "HoraFin", "HoraInicio" },
                values: new object[,]
                {
                    { 1, "Lunes", new TimeOnly(10, 0, 0), new TimeOnly(8, 0, 0) },
                    { 2, "Martes", new TimeOnly(12, 0, 0), new TimeOnly(10, 0, 0) }
                });

            migrationBuilder.InsertData(
                table: "Niveles",
                columns: new[] { "Id", "Nombre", "Numero" },
                values: new object[,]
                {
                    { 1, "Primer Nivel", 1 },
                    { 2, "Segundo Nivel", 2 },
                    { 3, "Tercer Nivel", 3 }
                });

            migrationBuilder.InsertData(
                table: "PeriodosAcademicos",
                columns: new[] { "Id", "FechaFin", "FechaInicio", "Gestion" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 6, 30, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), "2025-1" },
                    { 2, new DateTime(2025, 12, 15, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "2025-2" }
                });

            migrationBuilder.InsertData(
                table: "Estudiantes",
                columns: new[] { "Id", "CarreraId", "Ci", "Direccion", "Email", "Estado", "Nombre", "PasswordHash", "Registro", "Telefono" },
                values: new object[,]
                {
                    { 1, 1, "7894561", null, "carlos@uni.edu", "ACTIVO", "Carlos Sánchez", "$2a$11$YP4gNElOt/d79qZIHbwT3eAxgSg8R.DkDofhZOnq2dH1/IytjKdSq", "20251234", null },
                    { 2, 2, "222222", null, "ana@uni.edu", "ACTIVO", "Ana Rodríguez", "$2a$11$YP4gNElOt/d79qZIHbwT3eAxgSg8R.DkDofhZOnq2dH1/IytjKdSq", "EST002", null }
                });

            migrationBuilder.InsertData(
                table: "Materias",
                columns: new[] { "Id", "Codigo", "Creditos", "NivelId", "Nombre" },
                values: new object[,]
                {
                    { 1, "MAT101", 5, 1, "Matemáticas I" },
                    { 2, "PRG101", 5, 1, "Programación I" },
                    { 3, "BD101", 4, 2, "Bases de Datos" }
                });

            migrationBuilder.InsertData(
                table: "PlanesEstudio",
                columns: new[] { "Id", "CarreraId", "Codigo", "Estado", "Fecha", "Nombre" },
                values: new object[,]
                {
                    { 1, 1, "SIS25", "ACTIVO", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Plan 2025" },
                    { 2, 2, "IND24", "ACTIVO", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Plan 2024" }
                });

            migrationBuilder.InsertData(
                table: "GruposMaterias",
                columns: new[] { "Id", "AulaId", "Cupo", "DocenteId", "Estado", "Grupo", "HorarioId", "MateriaId", "PeriodoId" },
                values: new object[,]
                {
                    { 1, 1, 40, 1, "ACTIVO", "A", 1, 1, 1 },
                    { 2, 2, 35, 2, "ACTIVO", "B", 2, 2, 1 }
                });

            migrationBuilder.InsertData(
                table: "Inscripciones",
                columns: new[] { "Id", "Estado", "EstudianteId", "Fecha", "PeriodoId" },
                values: new object[,]
                {
                    { 1, "PENDIENTE", 1, new DateTime(2025, 2, 5, 0, 0, 0, 0, DateTimeKind.Utc), 1 },
                    { 2, "PENDIENTE", 2, new DateTime(2025, 2, 6, 0, 0, 0, 0, DateTimeKind.Utc), 1 }
                });

            migrationBuilder.InsertData(
                table: "PlanMaterias",
                columns: new[] { "Id", "MateriaId", "PlanId", "Semestre" },
                values: new object[,]
                {
                    { 1, 1, 1, 1 },
                    { 2, 2, 1, 1 },
                    { 3, 3, 1, 2 }
                });

            migrationBuilder.InsertData(
                table: "Prerequisitos",
                columns: new[] { "Id", "MateriaId", "MateriaPrerequisitoId", "NotaMin", "Tipo" },
                values: new object[] { 1, 3, 1, 60m, "Académico" });

            migrationBuilder.InsertData(
                table: "DetallesInscripciones",
                columns: new[] { "Id", "Codigo", "Estado", "GrupoMateriaId", "InscripcionId" },
                values: new object[,]
                {
                    { 1, "DET001", "INSCRITO", 1, 1 },
                    { 2, "DET002", "INSCRITO", 2, 2 }
                });

            migrationBuilder.InsertData(
                table: "HistorialesAcademicos",
                columns: new[] { "Id", "Aprobado", "DetalleInscripcionId", "FechaAprobacion", "Intento", "UltimaNota" },
                values: new object[,]
                {
                    { 1, true, 1, new DateTime(2025, 6, 30, 0, 0, 0, 0, DateTimeKind.Utc), 1, 85m },
                    { 2, false, 2, new DateTime(2025, 6, 30, 0, 0, 0, 0, DateTimeKind.Utc), 1, 70m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_DetallesInscripciones_GrupoMateriaId",
                table: "DetallesInscripciones",
                column: "GrupoMateriaId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesInscripciones_InscripcionId_GrupoMateriaId",
                table: "DetallesInscripciones",
                columns: new[] { "InscripcionId", "GrupoMateriaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Estudiantes_CarreraId",
                table: "Estudiantes",
                column: "CarreraId");

            migrationBuilder.CreateIndex(
                name: "IX_GruposMaterias_AulaId",
                table: "GruposMaterias",
                column: "AulaId");

            migrationBuilder.CreateIndex(
                name: "IX_GruposMaterias_DocenteId",
                table: "GruposMaterias",
                column: "DocenteId");

            migrationBuilder.CreateIndex(
                name: "IX_GruposMaterias_HorarioId",
                table: "GruposMaterias",
                column: "HorarioId");

            migrationBuilder.CreateIndex(
                name: "IX_GruposMaterias_MateriaId",
                table: "GruposMaterias",
                column: "MateriaId");

            migrationBuilder.CreateIndex(
                name: "IX_GruposMaterias_PeriodoId",
                table: "GruposMaterias",
                column: "PeriodoId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialesAcademicos_DetalleInscripcionId",
                table: "HistorialesAcademicos",
                column: "DetalleInscripcionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inscripciones_EstudianteId",
                table: "Inscripciones",
                column: "EstudianteId");

            migrationBuilder.CreateIndex(
                name: "IX_Inscripciones_PeriodoId",
                table: "Inscripciones",
                column: "PeriodoId");

            migrationBuilder.CreateIndex(
                name: "IX_Materias_NivelId",
                table: "Materias",
                column: "NivelId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanesEstudio_CarreraId",
                table: "PlanesEstudio",
                column: "CarreraId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanMaterias_MateriaId",
                table: "PlanMaterias",
                column: "MateriaId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanMaterias_PlanId_MateriaId",
                table: "PlanMaterias",
                columns: new[] { "PlanId", "MateriaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Prerequisitos_MateriaId_MateriaPrerequisitoId",
                table: "Prerequisitos",
                columns: new[] { "MateriaId", "MateriaPrerequisitoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Prerequisitos_MateriaPrerequisitoId",
                table: "Prerequisitos",
                column: "MateriaPrerequisitoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistorialesAcademicos");

            migrationBuilder.DropTable(
                name: "PlanMaterias");

            migrationBuilder.DropTable(
                name: "Prerequisitos");

            migrationBuilder.DropTable(
                name: "DetallesInscripciones");

            migrationBuilder.DropTable(
                name: "PlanesEstudio");

            migrationBuilder.DropTable(
                name: "GruposMaterias");

            migrationBuilder.DropTable(
                name: "Inscripciones");

            migrationBuilder.DropTable(
                name: "Aulas");

            migrationBuilder.DropTable(
                name: "Docentes");

            migrationBuilder.DropTable(
                name: "Horarios");

            migrationBuilder.DropTable(
                name: "Materias");

            migrationBuilder.DropTable(
                name: "Estudiantes");

            migrationBuilder.DropTable(
                name: "PeriodosAcademicos");

            migrationBuilder.DropTable(
                name: "Niveles");

            migrationBuilder.DropTable(
                name: "Carreras");
        }
    }
}
