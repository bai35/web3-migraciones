namespace GimnasioApi.Repositorios;

public interface IUnidadDeTrabajo
{
    /// <summary>Guarda los cambios pendientes (EF Core ya lo hace dentro de una transacción).</summary>
    Task<int> GuardarAsync();

    /// <summary>
    /// Ejecuta varios pasos como una sola transacción: si algo falla se hace rollback de todo.
    /// </summary>
    Task<TResultado> EjecutarEnTransaccionAsync<TResultado>(Func<Task<TResultado>> operacion);
}
