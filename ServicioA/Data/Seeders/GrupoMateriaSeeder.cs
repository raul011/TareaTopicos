using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Data.Seeders
{
    public static class GrupoMateriaSeeder
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GrupoMateria>().HasData(
               // introducciona a la informatica
                new GrupoMateria { Id = 1, Grupo = "SB", Cupo = 5, Estado = "ACTIVO", MateriaId = 3, DocenteId = 97, PeriodoId = 1, HorarioId = 11, AulaId = 1 },
                new GrupoMateria { Id = 2, Grupo = "SD", Cupo = 5, Estado = "ACTIVO", MateriaId = 3, DocenteId = 57, PeriodoId = 1, HorarioId = 1, AulaId = 1 },
                new GrupoMateria { Id = 3, Grupo = "SF", Cupo = 5, Estado = "ACTIVO", MateriaId = 3, DocenteId = 35, PeriodoId = 1, HorarioId = 10, AulaId = 1 },
                new GrupoMateria { Id = 4, Grupo = "Z1", Cupo = 5, Estado = "ACTIVO", MateriaId = 3, DocenteId = 92, PeriodoId = 1, HorarioId = 12, AulaId = 1 },
               //estructuras discretas
                new GrupoMateria { Id = 5, Grupo = "SF", Cupo = 5, Estado = "ACTIVO", MateriaId = 2, DocenteId = 55, PeriodoId = 1, HorarioId = 12, AulaId = 1 },
                new GrupoMateria { Id = 6, Grupo = "SH", Cupo = 5, Estado = "ACTIVO", MateriaId = 2, DocenteId = 56, PeriodoId = 1, HorarioId = 2, AulaId = 2 },
                new GrupoMateria { Id = 7, Grupo = "SK", Cupo = 5, Estado = "ACTIVO", MateriaId = 2, DocenteId = 43, PeriodoId = 1, HorarioId = 2, AulaId = 2 },
                new GrupoMateria { Id = 8, Grupo = "Z2", Cupo = 5, Estado = "ACTIVO", MateriaId = 2, DocenteId = 14, PeriodoId = 1, HorarioId = 10, AulaId = 2 },
                //calculo I    - falta horario
                new GrupoMateria { Id = 9, Grupo = "F1", Cupo = 5, Estado = "ACTIVO", MateriaId = 1, DocenteId = 9, PeriodoId = 1, HorarioId = 10, AulaId = 2 },
                new GrupoMateria { Id = 10, Grupo = "SG", Cupo = 5, Estado = "ACTIVO", MateriaId = 1, DocenteId = 16, PeriodoId = 1, HorarioId = 10, AulaId = 2 },
                new GrupoMateria { Id = 11, Grupo = "Z1", Cupo = 5, Estado = "ACTIVO", MateriaId = 1, DocenteId = 80, PeriodoId = 1, HorarioId = 10, AulaId = 2 },
                //INGLES I    - falta horario
                new GrupoMateria { Id = 12, Grupo = "Z1", Cupo = 5, Estado = "ACTIVO", MateriaId = 5, DocenteId = 26, PeriodoId = 1, HorarioId = 10, AulaId = 2 },
                new GrupoMateria { Id = 13, Grupo = "Z3", Cupo = 5, Estado = "ACTIVO", MateriaId = 5, DocenteId = 98, PeriodoId = 1, HorarioId = 10, AulaId = 2 },
                //FISICA I   - falta horario
                new GrupoMateria { Id = 14, Grupo = "Z1", Cupo = 5, Estado = "ACTIVO", MateriaId = 4, DocenteId = 63, PeriodoId = 1, HorarioId = 10, AulaId = 2 },
                new GrupoMateria { Id = 15, Grupo = "Z3", Cupo = 5, Estado = "ACTIVO", MateriaId = 4, DocenteId = 25, PeriodoId = 1, HorarioId = 10, AulaId = 2 },
                new GrupoMateria { Id = 16, Grupo = "SI", Cupo = 5, Estado = "ACTIVO", MateriaId = 4, DocenteId = 23, PeriodoId = 1, HorarioId = 10, AulaId = 2 }
  );
        }
    }
}
