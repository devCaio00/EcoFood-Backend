using EcoFood.Api.Models;

namespace EcoFood.Api.Dtos;

// O cliente precisa declarar explicitamente que confirmou o endereco.
// Sem este campo chegando como true, a API recusa a reserva.
public record ReservaCreateDto(
    int DoacaoId,
    int ReceptorId,
    bool EnderecoConfirmado
);

public record ReservaResponseDto(
    int Id,
    int DoacaoId,
    string DoacaoDescricao,
    int ReceptorId,
    string ReceptorNome,
    bool EnderecoConfirmado,
    string EnderecoRetiradaSnapshot,
    DateTime DataReserva,
    DateTime? DataRetirada,
    string Status
)
{
    public static ReservaResponseDto FromEntity(Reserva r) => new(
        r.Id,
        r.DoacaoId,
        r.Doacao?.Descricao ?? string.Empty,
        r.ReceptorId,
        r.Receptor?.Nome ?? string.Empty,
        r.EnderecoConfirmado,
        r.EnderecoRetiradaSnapshot,
        r.DataReserva,
        r.DataRetirada,
        r.Status.ToString()
    );
}