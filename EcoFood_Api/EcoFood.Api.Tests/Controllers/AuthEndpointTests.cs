using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EcoFood.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EcoFood.Api.Tests.Controllers;

public class AuthEndpointTests : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client;
    private readonly ApiTestFactory _factory;
    private const string Senha = "SenhaTeste123!";
    private const string NovaSenha = "SenhaTrocada456!";

    public AuthEndpointTests(ApiTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ContaComSenha_PermiteLoginEdicaoExclusaoEInvalidaSessoesAntigas()
    {
        const string email = "conta-login@teste.com";

        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await LoginAsync("padaria@integracao.com", Senha)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await CadastrarAsync("admin-novo@teste.com", "Admin")).StatusCode);

        var cadastro = await CadastrarAsync(email);
        Assert.Equal(HttpStatusCode.Created, cadastro.StatusCode);
        var conta = await LerJsonAsync(cadastro);
        var id = conta.GetProperty("id").GetInt32();
        Assert.False(conta.TryGetProperty("senhaHash", out _));

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var usuario = await db.Usuarios.SingleAsync(u => u.Id == id);
            Assert.False(string.IsNullOrEmpty(usuario.SenhaHash));
            Assert.NotEqual(Senha, usuario.SenhaHash);
        }

        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(email, "errada")).StatusCode);
        await AutenticarAsync(email.ToUpperInvariant(), Senha);

        var me = await LerJsonAsync(await _client.GetAsync("/api/auth/me"));
        Assert.Equal(id, me.GetProperty("id").GetInt32());
        Assert.False(me.TryGetProperty("senhaHash", out _));

        var outraConta = await LerJsonAsync(await CadastrarAsync("outra-conta@teste.com"));
        var outroId = outraConta.GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.Forbidden, (await EditarAsync(outroId, email)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.DeleteAsync($"/api/usuarios/{outroId}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await EditarAsync(id, email, novaSenha: NovaSenha)).StatusCode);

        var edicao = await EditarAsync(id, email, Senha, NovaSenha);
        Assert.Equal(HttpStatusCode.OK, edicao.StatusCode);
        Assert.Equal("Nome Editado", (await LerJsonAsync(edicao)).GetProperty("nome").GetString());

        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(email, Senha)).StatusCode);
        await AutenticarAsync(email, NovaSenha);

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/usuarios/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/usuarios/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(email, NovaSenha)).StatusCode);
    }

    [Fact]
    public async Task ExcluirContaComDoacao_RetornaConflitoEPreservaDoacao()
    {
        const string email = "doador-com-doacao@teste.com";
        var conta = await LerJsonAsync(await CadastrarAsync(email));
        var id = conta.GetProperty("id").GetInt32();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Doacoes.Add(new EcoFood.Api.Models.Doacao
        {
            DoadorId = id,
            Descricao = "Paes",
            Quantidade = 3,
            UnidadeMedida = "un"
        });
        await db.SaveChangesAsync();

        await AutenticarAsync(email, Senha);
        Assert.Equal(HttpStatusCode.Conflict, (await _client.DeleteAsync($"/api/usuarios/{id}")).StatusCode);
        Assert.True(await db.Doacoes.AnyAsync(d => d.DoadorId == id));
    }

    private Task<HttpResponseMessage> CadastrarAsync(string email, string tipoUsuario = "Doador") =>
        _client.PostAsJsonAsync("/api/usuarios", new
        {
            nome = "Pessoa Teste", email, tipoUsuario, endereco = "Rua A, 1", senha = Senha
        });

    private Task<HttpResponseMessage> LoginAsync(string email, string senha) =>
        _client.PostAsJsonAsync("/api/auth/login", new { email, senha });

    private async Task AutenticarAsync(string email, string senha)
    {
        var resposta = await LoginAsync(email, senha);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var token = await LerJsonAsync(resposta);
        Assert.Equal("Bearer", token.GetProperty("tokenType").GetString());
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token.GetProperty("accessToken").GetString());
    }

    private Task<HttpResponseMessage> EditarAsync(
        int id, string email, string? senhaAtual = null, string? novaSenha = null) =>
        _client.PutAsJsonAsync($"/api/usuarios/{id}", new
        {
            nome = "Nome Editado", email, endereco = "Rua B, 2", senhaAtual, novaSenha
        });

    private static Task<JsonElement> LerJsonAsync(HttpResponseMessage resposta) =>
        resposta.Content.ReadFromJsonAsync<JsonElement>();
}
