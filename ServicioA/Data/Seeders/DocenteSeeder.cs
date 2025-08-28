using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Data.Seeders
{
    public static class DocenteSeeder
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Docente>().HasData(
                new Docente { Id = 1, Registro = "DOC001", Ci = "123456", Nombre = "Juan Pérez", Telefono = "70011111", Estado = "ACTIVO" },
                new Docente { Id = 2, Registro = "DOC002", Ci = "789012", Nombre = "María Gómez", Telefono = "70022222", Estado = "ACTIVO" }
            );
        }
    }
}
