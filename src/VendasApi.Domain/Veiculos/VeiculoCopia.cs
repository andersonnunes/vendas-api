using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace VendasApi.Domain.Veiculos;

/// <summary>
/// Cópia local de um veículo de veiculos-api, usada para listar e vender sem consultar o outro serviço.
/// </summary>
public sealed partial class VeiculoCopia
{
    private const int TamanhoMaximoMarcaModelo = 50;
    private const int TamanhoMaximoCor = 30;
    private const int AnoMinimo = 1900;
    private const decimal PrecoMaximo = 9_999_999_999.99m;

    /// <summary>Critério de "à venda" usado pelas consultas: só o domínio decide o que é disponível.</summary>
    public static Expression<Func<VeiculoCopia, bool>> EstaDisponivelParaVenda =>
        v => v.Status == StatusVeiculoCopia.Disponivel;

    /// <summary>Critério de "vendido" usado pelas consultas administrativas.</summary>
    public static Expression<Func<VeiculoCopia, bool>> EstaVendido =>
        v => v.Status == StatusVeiculoCopia.Vendido;

    private VeiculoCopia()
    {
    }

    public Guid Id { get; private set; }
    public string Marca { get; private set; } = string.Empty;
    public string Modelo { get; private set; } = string.Empty;
    public int Ano { get; private set; }
    public string Cor { get; private set; } = string.Empty;
    public decimal Preco { get; private set; }
    public string Chassi { get; private set; } = string.Empty;
    public string? Placa { get; private set; }
    public StatusVeiculoCopia Status { get; private set; }
    public DateTimeOffset AtualizadoEm { get; private set; }

    public static VeiculoCopia Criar(DadosSnapshot dados, int anoAtual)
    {
        ValidarSnapshot(dados, anoAtual);

        return new VeiculoCopia
        {
            Id = dados.Id,
            Marca = dados.Marca,
            Modelo = dados.Modelo,
            Ano = dados.Ano,
            Cor = dados.Cor,
            Preco = dados.Preco,
            Chassi = dados.Chassi,
            Placa = dados.Placa,
            Status = dados.Status,
            AtualizadoEm = dados.AtualizadoEm,
        };
    }

    /// <summary>
    /// Aplica um snapshot de veiculos-api. Retorna false, sem alterar nada, quando o snapshot é mais antigo
    /// que o último aplicado; snapshot igual é idempotente.
    /// Regras de status: `vendido` sempre prevalece; `disponivel` não sobrescreve uma reserva em andamento.
    /// </summary>
    public bool AplicarSnapshot(DadosSnapshot dados, int anoAtual)
    {
        ValidarSnapshot(dados, anoAtual);

        if (dados.AtualizadoEm < AtualizadoEm)
            return false;

        Marca = dados.Marca;
        Modelo = dados.Modelo;
        Ano = dados.Ano;
        Cor = dados.Cor;
        Preco = dados.Preco;
        Chassi = dados.Chassi;
        Placa = dados.Placa;
        Status = ResolverStatus(Status, dados.Status);
        AtualizadoEm = dados.AtualizadoEm;
        return true;
    }

    /// <summary>
    /// Marca o veículo como reservado durante uma venda em andamento (US2.4). Ainda sem caminho de negócio
    /// nesta história; existe para que a regra de precedência da reserva seja testada pelo domínio.
    /// </summary>
    /// <summary>Pagamento da venda confirmado: o veículo reservado passa a vendido.</summary>
    public void MarcarVendido()
    {
        if (Status != StatusVeiculoCopia.Reservado)
            throw new TransicaoInvalidaException("Só um veículo reservado pode ser marcado como vendido.");

        Status = StatusVeiculoCopia.Vendido;
    }

    /// <summary>Pagamento cancelado: o veículo reservado volta a disponível.</summary>
    public void Liberar()
    {
        if (Status != StatusVeiculoCopia.Reservado)
            throw new TransicaoInvalidaException("Só um veículo reservado pode ser liberado.");

        Status = StatusVeiculoCopia.Disponivel;
    }

    public void Reservar()
    {
        if (Status != StatusVeiculoCopia.Disponivel)
            throw new VeiculoIndisponivelParaVendaException();

        Status = StatusVeiculoCopia.Reservado;
    }

    /// <summary>
    /// Valida o snapshot antes de qualquer alteração. Lança <see cref="SnapshotInvalidoException"/> com os erros por campo.
    /// </summary>
    public static void ValidarSnapshot(DadosSnapshot dados, int anoAtual)
    {
        var erros = new Dictionary<string, List<string>>();

        ValidarTexto(erros, "marca", dados.Marca, TamanhoMaximoMarcaModelo, "Marca");
        ValidarTexto(erros, "modelo", dados.Modelo, TamanhoMaximoMarcaModelo, "Modelo");
        ValidarTexto(erros, "cor", dados.Cor, TamanhoMaximoCor, "Cor");

        if (dados.Ano < AnoMinimo || dados.Ano > anoAtual + 1)
            Adicionar(erros, "ano", $"Ano deve estar entre {AnoMinimo} e {anoAtual + 1}.");

        if (dados.Preco <= 0)
            Adicionar(erros, "preco", "Preço deve ser maior que zero.");
        else if (dados.Preco > PrecoMaximo)
            Adicionar(erros, "preco", $"Preço não pode ser maior que {PrecoMaximo:N2}.");
        else if (dados.Preco != decimal.Round(dados.Preco, 2))
            Adicionar(erros, "preco", "Preço deve ter no máximo duas casas decimais.");

        if (!RegexChassi().IsMatch(dados.Chassi))
            Adicionar(erros, "chassi", "Chassi deve ter 17 caracteres alfanuméricos, sem as letras I, O ou Q.");

        if (dados.Placa is not null && !RegexPlaca().IsMatch(dados.Placa))
            Adicionar(erros, "placa", "Placa deve ter 7 caracteres alfanuméricos.");

        if (dados.Status == StatusVeiculoCopia.Reservado)
            Adicionar(erros, "status", "Status 'reservado' é local a vendas-api e não pode vir do catálogo.");

        if (erros.Count > 0)
            throw new SnapshotInvalidoException(erros.ToDictionary(par => par.Key, par => par.Value.ToArray()));
    }

    private static StatusVeiculoCopia ResolverStatus(StatusVeiculoCopia atual, StatusVeiculoCopia recebido)
    {
        if (recebido == StatusVeiculoCopia.Vendido)
            return StatusVeiculoCopia.Vendido;

        return atual == StatusVeiculoCopia.Reservado ? StatusVeiculoCopia.Reservado : StatusVeiculoCopia.Disponivel;
    }

    private static void ValidarTexto(Dictionary<string, List<string>> erros, string campo, string valor, int tamanhoMaximo, string rotulo)
    {
        if (valor.Length is 0 || valor.Length > tamanhoMaximo)
            Adicionar(erros, campo, $"{rotulo} é obrigatória e deve ter até {tamanhoMaximo} caracteres.");
    }

    private static void Adicionar(Dictionary<string, List<string>> erros, string campo, string mensagem)
    {
        if (!erros.TryGetValue(campo, out var lista))
            erros[campo] = lista = [];
        lista.Add(mensagem);
    }

    [GeneratedRegex("^[A-HJ-NPR-Z0-9]{17}$", RegexOptions.None, matchTimeoutMilliseconds: 100)]
    private static partial Regex RegexChassi();

    [GeneratedRegex("^[A-Z0-9]{7}$", RegexOptions.None, matchTimeoutMilliseconds: 100)]
    private static partial Regex RegexPlaca();
}
