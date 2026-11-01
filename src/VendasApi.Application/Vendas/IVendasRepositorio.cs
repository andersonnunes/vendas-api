using VendasApi.Domain.Veiculos;
using VendasApi.Domain.Vendas;

namespace VendasApi.Application.Vendas;

public interface IVendasRepositorio
{
    /// <summary>
    /// Trava a cópia do veículo, entrega a cópia atual (ou null, se não existir) a <paramref name="efetuar"/>
    /// e grava a venda que ele devolver, junto com a alteração da cópia. Tudo é confirmado ou nada é gravado.
    /// Quem decide a regra é o caso de uso; o repositório só cuida da trava, da transação e da gravação.
    /// </summary>
    /// <exception cref="Application.Vendas.VeiculoNaoEncontradoException">Lançada por <paramref name="efetuar"/> quando a cópia é null.</exception>
    Task EfetuarVendaAsync(Guid veiculoId, Func<VeiculoCopia?, Venda> efetuar, CancellationToken cancellationToken);
}
