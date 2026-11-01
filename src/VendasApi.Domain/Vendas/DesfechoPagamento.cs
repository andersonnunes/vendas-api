namespace VendasApi.Domain.Vendas;

/// <summary>O que a aplicação de um resultado de pagamento fez com a venda.</summary>
public enum DesfechoPagamento
{
    /// <summary>A venda passou de reservada para concluída.</summary>
    Concluida,

    /// <summary>A venda passou de reservada para cancelada.</summary>
    Cancelada,

    /// <summary>O resultado repetia o estado atual: nada mudou.</summary>
    JaAplicado
}
