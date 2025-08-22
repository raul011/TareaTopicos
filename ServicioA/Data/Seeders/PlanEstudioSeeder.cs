// Seeders/PlanEstudioSeeder.cs
using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Data.Seeders
{
    public static class PlanEstudioSeeder
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PlanEstudio>().HasData(
                new PlanEstudio { Id = 1, Nombre = "Ingeniería de Sistemas", Anio = 2025 }
            );

            modelBuilder.Entity<PlanMateria>().HasData(
                new PlanMateria { Id = 1, PlanId = 1, MateriaId = 1, Semestre = 1 },
                new PlanMateria { Id = 2, PlanId = 1, MateriaId = 2, Semestre = 1 },
                new PlanMateria { Id = 3, PlanId = 1, MateriaId = 3, Semestre = 2 }
            );
        }
    }
}
