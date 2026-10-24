using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoFood.Api.Models;

public class Doacao {
    public int Id { get; set; }

    [Required]
    public int DoadorId { get; set; }

    [ForeignKey(nameof(DoadorId))]
    public Usuario? Doador { get; set; }
    
    [Required, MaxLength(255)]
    public string Descricao { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? Categoria { get; set; }

    [Required]
    public int Quantidade { get; set; }

    [MaxLength(20)]
    public string UnidadeMedida { get; set; } = "kg";

    public DateOnly? ValidadeAlimento { get; set; }

    public StatusDoacao Status { get; set; } = StatusDoacao.Disponivel;

    [MaxLength(500)]
    public string? Observacoes { get; set; }

    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    // Uma doacao tem no maximo UMA reserva (por isso nao e uma colecao)
    public Reserva? Reserva { get; set; }
}