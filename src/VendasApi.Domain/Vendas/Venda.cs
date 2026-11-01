namespace VendasApi.Domain.Vendas;

/// <summary>
/// Venda de um veículo. Nasce reservada, aguardando o pagamento. A conclusão ou o cancelamento
/// pertencem à US2.5.
/// </summary>
public sealed class Venda
{
    private const int DiasMaximosRetroativos = 30;

    private Venda()
    {
    }

    public Guid Id { get; private set; }
    public Guid VeiculoId { get; private set; }
    public string Cpf { get; private set; } = string.Empty;
    public DateOnly DataVenda { get; private set; }
    public StatusVenda Status { get; private set; }
    public string CodigoPagamento { get; private set; } = string.Empty;
    public DateTimeOffset CriadoEm { get; private set; }

    /// <summary>
    /// Aplica o resultado do pagamento. Reservada vira concluída (aprovado) ou cancelada (cancelado);
    /// repetir o resultado que já foi aplicado é idempotente; o resultado contrário lança exceção.
    /// </summary>
    public DesfechoPagamento AplicarPagamento(ResultadoPagamento resultado)
    {
        switch (resultado, Status)
        {
            case (ResultadoPagamento.Aprovado, StatusVenda.Reservada):
                Concluir();
                return DesfechoPagamento.Concluida;

            case (ResultadoPagamento.Cancelado, StatusVenda.Reservada):
                Cancelar();
                return DesfechoPagamento.Cancelada;

            case (ResultadoPagamento.Aprovado, StatusVenda.Concluida):
            case (ResultadoPagamento.Cancelado, StatusVenda.Cancelada):
                return DesfechoPagamento.JaAplicado;

            default:
                throw new TransicaoInvalidaException(
                    $"Pagamento {resultado.ToString().ToLowerInvariant()} não se aplica a uma venda {Status.ToString().ToLowerInvariant()}.");
        }
    }

    /// <summary>Pagamento confirmado: a reserva vira venda concluída.</summary>
    public void Concluir()
    {
        if (Status != StatusVenda.Reservada)
            throw new TransicaoInvalidaException($"Uma venda {Status.ToString().ToLowerInvariant()} não pode ser concluída.");

        Status = StatusVenda.Concluida;
    }

    /// <summary>Pagamento cancelado: a reserva é desfeita.</summary>
    public void Cancelar()
    {
        if (Status != StatusVenda.Reservada)
            throw new TransicaoInvalidaException($"Uma venda {Status.ToString().ToLowerInvariant()} não pode ser cancelada.");

        Status = StatusVenda.Cancelada;
    }

    public static Venda Criar(
        Guid id,
        Guid veiculoId,
        string cpf,
        DateOnly dataVenda,
        DateOnly hoje,
        string codigoPagamento,
        DateTimeOffset criadoEm)
    {
        var cpfNormalizado = CpfValidacao.Normalizar(cpf);
        var erros = new Dictionary<string, List<string>>();

        if (!CpfValidacao.EhValido(cpfNormalizado))
            Adicionar(erros, "cpf", "CPF inválido.");

        if (dataVenda > hoje)
            Adicionar(erros, "dataVenda", "A data da venda não pode ser futura.");
        else if (dataVenda < hoje.AddDays(-DiasMaximosRetroativos))
            Adicionar(erros, "dataVenda", $"A data da venda não pode ser anterior a {DiasMaximosRetroativos} dias.");

        if (erros.Count > 0)
            throw new VendaInvalidaException(erros.ToDictionary(par => par.Key, par => par.Value.ToArray()));

        return new Venda
        {
            Id = id,
            VeiculoId = veiculoId,
            Cpf = cpfNormalizado,
            DataVenda = dataVenda,
            Status = StatusVenda.Reservada,
            CodigoPagamento = codigoPagamento,
            CriadoEm = criadoEm,
        };
    }

    private static void Adicionar(Dictionary<string, List<string>> erros, string campo, string mensagem)
    {
        if (!erros.TryGetValue(campo, out var lista))
            erros[campo] = lista = [];
        lista.Add(mensagem);
    }
}
