using EcoFood.Api.Models;

namespace EcoFood.Api.Dtos;

// Entrada: o que o cliente PODE enviar ao cadastrar
public record UsuarioCreateDto(
    string Nome,
    string Email,
    string? Telefone,
    TipoUsuario TipoUsuario,
    string Endereco
);

// Saída: o que a API devolve
public record UsuarioResponseDto(
    int Id,
    string Nome,
    string Email,
    string? Telefone,
    string TipoUsuario,
    string Endereco,
    DateTime DataCadastro
)
{
    // Fábrica: converte a entidade no DTO de saída.
    // Fica aqui para a conversão existir em um único lugar.
    public static UsuarioResponseDto FromEntity(Usuario u) => new(
        u.Id,
        u.Nome,
        u.Email,
        u.Telefone,
        u.TipoUsuario.ToString(),
        u.Endereco,
        u.DataCadastro
    );
}