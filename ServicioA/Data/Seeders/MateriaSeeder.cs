// Seeders/MateriaSeeder.cs
using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Data.Seeders
{
    public static class MateriaSeeder
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Materia>().HasData(
                new Materia { Id = 1, Nombre = "Matemáticas I" },
                new Materia { Id = 2, Nombre = "Programación I" },
                new Materia { Id = 3, Nombre = "Bases de Datos" }
            );
        }
    }
}
