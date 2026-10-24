using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoFood.Api.Models;

public class Reserva {
    public int Id { get; set; }

    public int DoacaoId { get; set; }

    [ForeignKey(nameof(DoacaoId))]
    public Doacao? Doacao { get; set; }

    public int ReceptorId { get; set; }

    [ForeignKey(nameof(ReceptorId))]
    public Usuario? Receptor { get; set; }

    // O receptor marcou na tela que vai retirar no endereco informado.
    // A API recusa criar a reserva se isto nao for true.
    public bool EnderecoConfirmado { get; set; }
    // Copia do endereco do doador no momento da reserva: se o doador
    // editar o endereco depois, o que foi acordado fica preservado aqui.
    [Required, MaxLength(255)]
    public string EnderecoRetiradaSnapshot { get; set; } = string.Empty;

    public DateTime DataReserva { get; set; } = DateTime.UtcNow;

    public DateTime? DataRetirada { get; set; }
    
    public StatusReserva Status { get; set; } = StatusReserva.Pendente;
}