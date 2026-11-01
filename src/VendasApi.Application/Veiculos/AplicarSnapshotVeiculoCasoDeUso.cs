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

        for (var tentativa = 1; tentativa < TentativasMaximas; tentativa++)
        {
            try
            {
                await repositorio.AplicarAsync(dados.Id, copia => Aplicar(copia, dados, anoAtual), cancellationToken);
                return;
            }
            catch (ConcorrenciaCopiaVeiculoException)
            {
                // Outro envio criou a cópia entre a leitura e a gravação: a próxima tentativa a encontra e a atualiza.
            }
        }

        // Última tentativa: se também houver conflito, a exceção segue para o chamador.
        await repositorio.AplicarAsync(dados.Id, copia => Aplicar(copia, dados, anoAtual), cancellationToken);
    }

    private static VeiculoCopia Aplicar(VeiculoCopia? copia, DadosSnapshot dados, int anoAtual)
    {
        if (copia is null)
            return VeiculoCopia.Criar(dados, anoAtual);

        copia.AplicarSnapshot(dados, anoAtual);
        return copia;
    }
}
