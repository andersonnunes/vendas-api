using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;
using Scalar.AspNetCore;
using VendasApi.Application.Veiculos;
using VendasApi.Application.Veiculos.Listagem;
using VendasApi.Application.Vendas;
using VendasApi.Erros;
using VendasApi.Infrastructure;
using VendasApi.Infrastructure.Persistence;
using VendasApi.Seguranca;

[assembly: InternalsVisibleTo("VendasApi.Tests")]

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSegurancaKeycloak(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ExcecoesDeNegocioHandler>();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<AplicarSnapshotVeiculoCasoDeUso>();
builder.Services.AddScoped<ListarVeiculosDisponiveisCasoDeUso>();
builder.Services.AddScoped<ListarVeiculosVendidosCasoDeUso>();
builder.Services.AddScoped<EfetuarVendaCasoDeUso>();

var app = builder.Build();

using (var escopo = app.Services.CreateScope())
    await escopo.ServiceProvider.GetRequiredService<VendasDbContext>().Database.MigrateAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

await app.RunAsync();
