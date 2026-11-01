namespace VendasApi.Domain.Vendas;

/// <summary>Regras de CPF: 11 dígitos com os dígitos verificadores corretos.</summary>
public static class CpfValidacao
{
    private const int Tamanho = 11;

    /// <summary>Remove pontos e traço, se houver. Não valida.</summary>
    public static string Normalizar(string cpf) => cpf.Replace(".", string.Empty).Replace("-", string.Empty);

    public static bool EhValido(string cpf)
    {
        if (cpf.Length != Tamanho || cpf.Any(c => c < '0' || c > '9'))
            return false;
        if (cpf.Distinct().Count() == 1)
            return false;

        var digitos = cpf.Select(c => c - '0').ToArray();
        return digitos[9] == DigitoVerificador(digitos, 9) && digitos[10] == DigitoVerificador(digitos, 10);
    }

    /// <summary>Mostra só os dígitos do meio (***.456.789-**), para a resposta da API e os logs.</summary>
    public static string Mascarar(string cpf) => $"***.{cpf.Substring(3, 3)}.{cpf.Substring(6, 3)}-**";

    private static int DigitoVerificador(int[] digitos, int posicao)
    {
        var soma = 0;
        for (var i = 0; i < posicao; i++)
            soma += digitos[i] * (posicao + 1 - i);

        var resto = soma * 10 % 11;
        return resto == 10 ? 0 : resto;
    }
}
