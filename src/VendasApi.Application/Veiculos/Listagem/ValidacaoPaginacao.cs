namespace VendasApi.Application.Veiculos.Listagem;

/// <summary>Regras de paginação compartilhadas pelas listagens (disponíveis e vendidos).</summary>
public static class ValidacaoPaginacao
{
    public const int TamanhoPadrao = 20;
    public const int TamanhoMaximo = 100;

    public static void Validar(int pagina, int tamanho)
    {
        if (pagina < 1)
            throw new ParametrosPaginacaoInvalidosException("A página deve ser maior ou igual a 1.");
        if (tamanho < 1 || tamanho > TamanhoMaximo)
            throw new ParametrosPaginacaoInvalidosException($"O tamanho deve estar entre 1 e {TamanhoMaximo}.");
    }
}
