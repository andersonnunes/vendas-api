using VendasApi.Tests.Seguranca;

namespace VendasApi.Tests.Infraestrutura;

[CollectionDefinition(NomeColecao)]
public sealed class AmbienteCollection : ICollectionFixture<KeycloakFixture>, ICollectionFixture<PostgresFixture>
{
    public const string NomeColecao = "ambiente";
}
