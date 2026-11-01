using VendasApi.Domain.Veiculos;

namespace VendasApi.Application.Veiculos;

public sealed class AplicarSnapshotVeiculoCasoDeUso(IVeiculoCopiaRepositorio repositorio, TimeProvider relogio)
{
    // Só a criação concorrente do mesmo veículo é repetida; uma segunda falha vira erro para o chamador.
    private const int TentativasMaximas = 2;

    public async Task ExecutarAsync(DadosSnapshot dados, CancellationToken cancellationToken)
    {
        var anoAtual = relogio.GetUtcNow().Year;
        VeiculoCopia.ValidarSnapshot(dados, anoAtual);

        for (var tentativa = 1; ; tentativa++)
        {
            try
            {
                await AplicarAsync(dados, anoAtual, cancellationToken);
                return;
            }
            catch (ConcorrenciaCopiaVeiculoException) when (tentativa < TentativasMaximas)
            {
                // Outro envio criou a cópia entre a leitura e a gravação: a próxima tentativa a encontra e a atualiza.
            }
        }
    }

    private async Task AplicarAsync(DadosSnapshot dados, int anoAtual, CancellationToken cancellationToken)
    {
        var copia = await repositorio.ObterParaAtualizacaoAsync(dados.Id, cancellationToken);
        if (copia is null)
            await repositorio.AdicionarAsync(VeiculoCopia.Criar(dados, anoAtual), cancellationToken);
        else
            copia.AplicarSnapshot(dados, anoAtual);

        await repositorio.SalvarAsync(cancellationToken);
    }
}
