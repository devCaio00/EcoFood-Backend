using System.ComponentModel.DataAnnotations;

namespace EcoFood.Api.Models;

public class Usuario {
    public int Id { get; set; }

    [Required, MaxLength(150)]  
    public string Nome { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Telefone { get; set; }

    [Required]
    public TipoUsuario TipoUsuario { get; set; }

    [Required, MaxLength(255)]
    public string Endereco { get; set; } = string.Empty;

    public DateTime DataCadastro { get; set; } = DateTime.UtcNow;

    public ICollection<Doacao> Doacoes { get; set; } = new List<Doacao>();
    
    public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
}