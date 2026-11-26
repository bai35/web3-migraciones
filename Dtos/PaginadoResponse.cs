using Microsoft.EntityFrameworkCore;

namespace GimnasioApi.Dtos;

public class PaginadoResponse<T>
{
    public int Pagina { get; set; }
    public int TamanioPagina { get; set; }
    public int TotalRegistros { get; set; }
    public int TotalPaginas { get; set; }
    public IReadOnlyList<T> Datos { get; set; } = new List<T>();

    public static PaginadoResponse<T> Crear(IReadOnlyList<T> datos, int total, int pagina, int tamanio)
    {
        return new PaginadoResponse<T>
        {
            Pagina = pagina,
            TamanioPagina = tamanio,
            TotalRegistros = total,
            TotalPaginas = (int)Math.Ceiling(total / (double)tamanio),
            Datos = datos
        };
    }
}

public static class PaginacionExtensions
{
    public const int TamanioMaximo = 50;

    /// <summary>Pagina en la base de datos (COUNT + OFFSET/FETCH). La consulta debe venir ordenada.</summary>
    public static async Task<PaginadoResponse<T>> PaginarAsync<T>(this IQueryable<T> consulta, int pagina, int tamanio)
    {
        if (pagina < 1)
            throw new ArgumentException("La página debe ser 1 o mayor.");
        if (tamanio < 1 || tamanio > TamanioMaximo)
            throw new ArgumentException($"El tamaño de página debe estar entre 1 y {TamanioMaximo}.");

        var total = await consulta.CountAsync();
        var datos = await consulta.Skip((pagina - 1) * tamanio).Take(tamanio).ToListAsync();

        return PaginadoResponse<T>.Crear(datos, total, pagina, tamanio);
    }
}
