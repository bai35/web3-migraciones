using GimnasioApi.Data;

namespace GimnasioApi.Repositorios;

public class UnidadDeTrabajo : IUnidadDeTrabajo
{
    private readonly GimnasioDbContext _db;

    public UnidadDeTrabajo(GimnasioDbContext db)
    {
        _db = db;
    }

    public Task<int> GuardarAsync() => _db.SaveChangesAsync();

    public async Task<TResultado> EjecutarEnTransaccionAsync<TResultado>(Func<Task<TResultado>> operacion)
    {
        await using var transaccion = await _db.Database.BeginTransactionAsync();
        try
        {
            var resultado = await operacion();
            await transaccion.CommitAsync();
            return resultado;
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }
}
