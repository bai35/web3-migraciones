namespace GimnasioApi.Models;

public class RefreshToken
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string TokenHash { get; set; } = null!;
    public DateTime Creado { get; set; }
    public DateTime Expira { get; set; }
    public bool Revocado { get; set; }

    public virtual Usuario Usuario { get; set; } = null!;
}
