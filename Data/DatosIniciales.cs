using GimnasioApi.Models;
using GimnasioApi.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace GimnasioApi.Data;

/// <summary>Datos mínimos para poder usar la API: usuario ADMIN, membresías y ejercicios de ejemplo.</summary>
public static class DatosIniciales
{
    public static async Task SembrarAsync(GimnasioDbContext db, IConfiguration config)
    {
        if (!await db.Set<Usuario>().AnyAsync(u => u.Rol == Roles.Admin))
        {
            var email = (config["AdminInicial:Email"] ?? "admin@gimnasio.com").Trim().ToLowerInvariant();
            var password = config["AdminInicial:Password"] ?? "Admin123*";

            db.Set<Usuario>().Add(new Usuario
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Rol = Roles.Admin,
                Activo = true
            });
        }

        if (!await db.Set<Membresia>().AnyAsync())
        {
            db.Set<Membresia>().AddRange(
                new Membresia { Nombre = "Mensual", DuracionDias = 30, Precio = 80m, Activa = true },
                new Membresia { Nombre = "Trimestral", DuracionDias = 90, Precio = 210m, Activa = true },
                new Membresia { Nombre = "Anual", DuracionDias = 365, Precio = 720m, Activa = true });
        }

        if (!await db.Set<Ejercicio>().AnyAsync())
        {
            db.Set<Ejercicio>().AddRange(
                new Ejercicio { Nombre = "Press de banca", GrupoMuscular = "Pecho" },
                new Ejercicio { Nombre = "Sentadilla", GrupoMuscular = "Piernas" },
                new Ejercicio { Nombre = "Peso muerto", GrupoMuscular = "Espalda" },
                new Ejercicio { Nombre = "Press militar", GrupoMuscular = "Hombros" },
                new Ejercicio { Nombre = "Curl de bíceps", GrupoMuscular = "Brazos" },
                new Ejercicio { Nombre = "Plancha abdominal", GrupoMuscular = "Core" });
        }

        await db.SaveChangesAsync();
    }
}
