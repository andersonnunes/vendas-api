using VendasApi.Domain.Veiculos;

namespace VendasApi.Application.Veiculos;

public interface IVeiculoCopiaRepositorio
{
    /// <summary>
    /// Trava a cópia para atualização (se existir) e inicia a unidade de trabalho. Retorna null quando
    /// ainda não existe; nesse caso o chamador deve usar <see cref="AdicionarAsync"/>.
    /// </summary>
    Task<VeiculoCopia?> ObterParaAtualizacaoAsync(Guid id, CancellationToken cancellationToken);

    Task AdicionarAsync(VeiculoCopia copia, CancellationToken cancellationToken);

    /// <summary>Confirma a unidade de trabalho iniciada por <see cref="ObterParaAtualizacaoAsync"/>.</summary>
    /// <exception cref="ConcorrenciaCopiaVeiculoException">Outro envio criou o mesmo veículo ao mesmo tempo.</exception>
    /// <exception cref="SnapshotVeiculoDuplicadoException">Chassi ou placa já usados por outro veículo.</exception>
    Task SalvarAsync(CancellationToken cancellationToken);
}
