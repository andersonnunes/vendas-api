using VendasApi.Domain.Vendas;

namespace VendasApi.Application.Vendas;

public sealed class EfetuarVendaCasoDeUso(IVendasRepositorio repositorio, TimeProvider relogio)
{
    public async Task<VendaDto> ExecutarAsync(EntradaEfetuarVenda entrada, CancellationToken cancellationToken)
    {
        var agora = relogio.GetUtcNow();
        var hoje = DateOnly.FromDateTime(agora.UtcDateTime);

        // A validação roda antes de qualquer trava no banco: entrada inválida não bloqueia o veículo.
        var venda = Venda.Criar(
            Guid.CreateVersion7(agora),
            entrada.VeiculoId,
            entrada.Cpf,
            entrada.DataVenda,
            hoje,
            Guid.NewGuid().ToString("N"),
            agora);

        await repositorio.ReservarVeiculoERegistrarVendaAsync(entrada.VeiculoId, venda, cancellationToken);

        return VendaDto.De(venda);
    }
}
