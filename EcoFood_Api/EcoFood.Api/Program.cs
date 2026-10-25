using Microsoft.EntityFrameworkCore;
using EcoFood.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// ---------- 1. Registro de servicos ----------

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "ConnectionString 'DefaultConnection' nao configurada."
    );

// Injecao de dependencia: quem pedir um AppDbContext no construtor recebe
// uma instancia por requisicao, ja conectada ao MySQL.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString)
    )
);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // JSON em camelCase: "doadorNome" em vez de "DoadorNome",
        // que e a convencao esperada pelo JavaScript do frontend.
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;

        // Aceita/devolve enums como string ("Receptor") em vez de
        // numero (1), que e o que o frontend envia e espera.
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter()
        );
    });

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "EcoFood API", Version = "v1" });
});

// CORS: o navegador bloqueia chamadas de uma origem para outra.
// Como o React roda em :5173 e a API em :5050, sao origens diferentes
// e precisamos liberar explicitamente a origem do frontend.
var frontendOrigin =
    builder.Configuration["Cors:AllowedOrigin"]
    ?? "http://localhost:5173";

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins(frontendOrigin)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// ---------- 2. Pipeline da requisicao (a ORDEM importa) ----------

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(c =>
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "EcoFood API v1")
    );
}

app.UseCors("FrontendPolicy"); // antes do MapControllers

app.MapControllers();

app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();