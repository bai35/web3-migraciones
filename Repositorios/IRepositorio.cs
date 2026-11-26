namespace GimnasioApi.Repositorios;

/// <summary>Repositorio genérico sobre Entity Framework Core.</summary>
public interface IRepositorio<T> where T : class
{
    /// <summary>Consulta LINQ sobre la tabla (se ejecuta recién al materializarla).</summary>
    IQueryable<T> Consultar();

    Task<T?> ObtenerPorIdAsync(int id);

    Task AgregarAsync(T entidad);
}
