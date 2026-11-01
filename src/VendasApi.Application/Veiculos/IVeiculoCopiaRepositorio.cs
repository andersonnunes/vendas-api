using VendasApi.Domain.Veiculos;

namespace VendasApi.Application.Veiculos;

public interface IVeiculoCopiaRepositorio
{
    /// <summary>
    /// Aplica uma alteração à cópia do veículo em uma única operação atômica. A implementação trava a
    /// cópia existente, entrega ao <paramref name="aplicar"/> a cópia atual (ou null, se não existir) e
    /// grava o resultado que ele devolver, confirmando tudo junto ou nada.
    /// </summary>
    /// <exception cref="ConcorrenciaCopiaVeiculoException">Outro envio criou o mesmo veículo ao mesmo tempo.</exception>
    /// <exception cref="SnapshotVeiculoDuplicadoException">Chassi ou placa já usados por outro veículo.</exception>
    Task AplicarAsync(Guid id, Func<VeiculoCopia?, VeiculoCopia> aplicar, CancellationToken cancellationToken);
}
