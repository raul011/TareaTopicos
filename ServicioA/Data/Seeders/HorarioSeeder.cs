using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Data.Seeders
{
    public static class HorarioSeeder
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Horario>().HasData(
                new Horario { Id = 1, Dia = "Mar-Jue", HoraInicio = new TimeOnly(18,15), HoraFin = new TimeOnly(20,30) }, //intro :mollo
                new Horario { Id = 2, Dia = "Mar-Jue", HoraInicio = new TimeOnly(7,0), HoraFin = new TimeOnly(9,15) }, //estructuras : miranda,katime
                new Horario { Id = 3, Dia = "Mar-Jue", HoraInicio = new TimeOnly(8,0), HoraFin = new TimeOnly(10,0) },
                new Horario { Id = 4, Dia = "Mar-Jue", HoraInicio = new TimeOnly(8,0), HoraFin = new TimeOnly(10,0) },
                new Horario { Id = 5, Dia = "Mar-Jue", HoraInicio = new TimeOnly(8,0), HoraFin = new TimeOnly(10,0) },
                new Horario { Id = 6, Dia = "Mar-Jue", HoraInicio = new TimeOnly(8,0), HoraFin = new TimeOnly(10,0) },
                new Horario { Id = 7, Dia = "Mar-Jue", HoraInicio = new TimeOnly(8,0), HoraFin = new TimeOnly(10,0) },
                new Horario { Id = 8, Dia = "Mar-Jue", HoraInicio = new TimeOnly(8,0), HoraFin = new TimeOnly(10,0) },
                new Horario { Id = 9, Dia = "Mar-Jue", HoraInicio = new TimeOnly(8,0), HoraFin = new TimeOnly(10,0) },
                new Horario { Id = 10, Dia = "Lun-Mie-Vie", HoraInicio = new TimeOnly(8,30), HoraFin = new TimeOnly(10,0) }, // intro :angelica, estructuras : braulio
                new Horario { Id = 11, Dia = "Lun-Mie-Vie", HoraInicio = new TimeOnly(10,0), HoraFin = new TimeOnly(11,30) }, // intro :zuna 
                new Horario { Id = 12, Dia = "Lun-Mie-Vie", HoraInicio = new TimeOnly(7,0), HoraFin = new TimeOnly(8,30) } // intro : winipeg , estructuras : sarah
  );
        }
    }
}
