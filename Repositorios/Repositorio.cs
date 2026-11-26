using GimnasioApi.Data;
using Microsoft.EntityFrameworkCore;

namespace GimnasioApi.Repositorios;

public class Repositorio<T> : IRepositorio<T> where T : class
{
    private readonly GimnasioDbContext _db;

    public Repositorio(GimnasioDbContext db)
    {
        _db = db;
    }

    // Se usa Set<T>() (y no una propiedad DbSet concreta) para que el repositorio
    // siga funcionando aunque Scaffold-DbContext genere nombres de DbSet distintos.
    public IQueryable<T> Consultar() => _db.Set<T>();

    public async Task<T?> ObtenerPorIdAsync(int id) => await _db.Set<T>().FindAsync(id);

    public async Task AgregarAsync(T entidad) => await _db.Set<T>().AddAsync(entidad);
}
