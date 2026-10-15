# vendas-api

Serviço de venda de veículos para uma plataforma de revenda de veículos automotores
(Tech Challenge Fase 4 — SOAT/PósTech). Repositório físico independente e isolado,
responsável por listagem de veículos à venda, listagem de veículos vendidos, efetuação da
venda (CPF + data) e o webhook de confirmação de pagamento — com **banco de dados próprio**,
separado do repositório irmão
[`veiculos-api`](https://github.com/andersonnunes/veiculos-api) (cadastro/edição de veículo).
Isolamento pensado para suportar picos de chamadas nos endpoints de listagem/compra sem
depender da disponibilidade do outro serviço.

Os dois serviços se comunicam apenas via HTTP: este serviço recebe de `veiculos-api` os
veículos cadastrados/editados (mantendo uma cópia local para listar/vender), e notifica de
volta o status final de uma venda (vendido/cancelado).

## Stack

- .NET 10 (ASP.NET Core Web API, controllers)
- PostgreSQL (banco próprio deste serviço, isolado do `veiculos-api`)
- xUnit + `WebApplicationFactory` para testes de integração
- Docker + GitHub Actions (CI: build, lint, testes com gate de cobertura ≥80%, scan de
  vulnerabilidades com Trivy)

## Como rodar localmente

```bash
docker compose -f infra/docker-compose.yml up --build
```

Sobe o serviço (`http://localhost:5002`) e seu Postgres (`localhost:5434`). Verificar:

```bash
curl http://localhost:5002/health
# {"status":"healthy"}
```

Em `Development`, a documentação interativa (Scalar) fica em `http://localhost:5002/scalar`.

### Rodando fora de container

Requer o SDK do .NET pinado em `global.json` (`10.0.401`):

```bash
dotnet restore
dotnet run --project src/VendasApi
```

## Como testar

```bash
dotnet test
```

Com cobertura (mesmo gate usado no CI, mínimo 80% de linhas):

```bash
dotnet test tests/VendasApi.Tests/VendasApi.Tests.csproj -c Release \
  /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura \
  /p:ExcludeByFile="**/obj/**/*.cs" \
  /p:Threshold=80 /p:ThresholdType=line /p:ThresholdStat=total
```

## Estrutura

```
vendas-api/
├── global.json              # pina o SDK do .NET
├── vendas-api.slnx
├── src/VendasApi/           # API (controllers, Program.cs, Dockerfile)
├── tests/VendasApi.Tests/   # testes xUnit + WebApplicationFactory
├── infra/docker-compose.yml # serviço + Postgres próprio
├── docs/                    # modelagem de dados / arquitetura
└── .github/workflows/ci.yml # build + lint + testes (cobertura ≥80%) + scan de imagem
```

## CI/CD

Todo Pull Request roda `dotnet restore` + `dotnet format --verify-no-changes` + `dotnet build`
+ `dotnet test` (com gate de cobertura de linha ≥80%) + build da imagem Docker + scan de
vulnerabilidades (Trivy, falha em qualquer HIGH/CRITICAL). A branch `main` é protegida: só
aceita merge com o workflow de CI verde.

Deploy automatizado disparado por merge em `main` — detalhado quando a história de deploy
deste serviço for implementada.
