using EcoFood.Api.Models;

namespace EcoFood.Api.Dtos;

public record DoacaoCreateDto(
    int DoadorId,
    string Descricao,
    string? Categoria,
    int Quantidade,
    string UnidadeMedida,
    DateOnly? ValidadeAlimento,
    string? Observacoes
);

public record DoacaoResponseDto(
    int Id,
    int DoadorId,
    string DoadorNome,
    string DoadorEndereco,
    string Descricao,
    string? Categoria,
    int Quantidade,
    string UnidadeMedida,
    DateOnly? ValidadeAlimento,
    string Status,
    string? Observacoes,
    DateTime DataCriacao
)
{
    public static DoacaoResponseDto FromEntity(Doacao d) => new(
        d.Id,
        d.DoadorId,
        d.Doador?.Nome ?? string.Empty,
        d.Doador?.Endereco ?? string.Empty,
        d.Descricao,
        d.Categoria,
        d.Quantidade,
        d.UnidadeMedida,
        d.ValidadeAlimento,
        d.Status.ToString(),
        d.Observacoes,
        d.DataCriacao
    );
}