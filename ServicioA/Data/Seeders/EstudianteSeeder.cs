using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Data.Seeders
{
    public static class EstudianteSeeder
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Estudiante>().HasData(
                new Estudiante { Id = 1, Registro = "EST001", Ci = "111111", Nombre = "Carlos Sánchez", Email = "carlos@uni.edu", Estado = "ACTIVO", CarreraId = 1 },
                new Estudiante { Id = 2, Registro = "EST002", Ci = "222222", Nombre = "Ana Rodríguez", Email = "ana@uni.edu", Estado = "ACTIVO", CarreraId = 2 }
            );
        }
    }
}
