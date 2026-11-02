# vendas-api

Serviço de venda de veículos para uma plataforma de revenda de veículos automotores
(Tech Challenge Fase 4 — SOAT/PósTech).

**O que faz**: lista os veículos à venda e os vendidos, efetua a venda (CPF e data) e recebe a
confirmação de pagamento por webhook. Tem banco de dados próprio, separado do
[`veiculos-api`](https://github.com/andersonnunes/veiculos-api), que é dono do cadastro e da
edição de veículos.

**O que não faz**: não cadastra nem edita veículos. Mantém só uma cópia local dos veículos,
recebida do `veiculos-api`.

Isolado de propósito: as listagens e a compra podem receber picos de chamadas sem depender da
disponibilidade do `veiculos-api`, porque leem a cópia local.

## Como foi implementado

Organizado em camadas, com dependências apontando para dentro:

| Projeto | Responsabilidade |
|---|---|
| `VendasApi.Domain` | Veículo da cópia local, venda, transições de status e regras de CPF e data. Sem dependências externas. |
| `VendasApi.Application` | Casos de uso e portas (`IVeiculoCopiaRepositorio`, `IListagemVeiculosConsulta`, entre outras). |
| `VendasApi.Infrastructure` | EF Core com PostgreSQL, migrações, consultas de listagem, cliente HTTP para o `veiculos-api` e verificação de assinatura do webhook. |
| `VendasApi` | API: controllers, autorização, `Program.cs` e `Dockerfile`. |

**Cópia local de veículos**: o `veiculos-api` envia cada cadastro e edição para
`PUT /internal/veiculos/{id}`. Um snapshot é aplicado conforme a ordem de atualização, e não tira um
veículo `reservado` ou `vendido` de volta para `disponivel`.

**Reserva e venda**: `POST /vendas` reserva o veículo, em `SELECT ... FOR UPDATE`, e devolve um
`codigoPagamento`. Um veículo já reservado ou vendido não pode ser reservado de novo (`409`).

**Webhook de pagamento**: o processador de pagamento informa o resultado pelo `codigoPagamento`.
Quando a venda é concluída ou cancelada, o `vendas-api` atualiza a cópia local e notifica o
`veiculos-api`. Repetir o mesmo resultado não altera nada e responde `200` com `alterou: false`.

**Autorização**: tokens do Keycloak. Os papéis `administrador` e `vendedor` podem efetuar vendas. A
listagem de vendidos é só para `administrador`. A rota interna de snapshot aceita só o serviço
`veiculos-api`, com papel `servico`.

## Endpoints

| Método | Rota | Quem chama | Respostas |
|---|---|---|---|
| `GET` | `/veiculos` | anônimo | `200`, `400` |
| `GET` | `/veiculos/vendidos` | administrador | `200`, `401`, `403`, `400` |
| `POST` | `/vendas` | administrador, vendedor | `201`, `400`, `401`, `403`, `404`, `409` |
| `POST` | `/webhooks/pagamentos` | processador de pagamento (anônimo, com assinatura) | `200`, `400`, `401`, `404`, `409` |
| `PUT` | `/internal/veiculos/{id}` | serviço `veiculos-api` | `200`, `400`, `409`, uso interno entre serviços |

**`GET /veiculos`** lista só os veículos `disponivel`, do mais barato para o mais caro (empate por
`id`). Parâmetros opcionais: `pagina` (padrão 1) e `tamanho` (padrão 20, máximo 100). A resposta
expõe `id`, `marca`, `modelo`, `ano`, `cor` e `preco`, e não expõe `chassi` nem `placa`.

**`GET /veiculos/vendidos`** lista os veículos vendidos, com a mesma paginação. Para o administrador,
a resposta inclui `chassi` e `placa`.

**`POST /vendas`** recebe o veículo, o CPF do comprador e a data da venda:

```json
{ "veiculoId": "<guid>", "cpf": "52998224725", "dataVenda": "2026-11-01" }
```

- O CPF precisa ter dígitos verificadores válidos. A resposta mostra o CPF mascarado (`***.982.247-**`).
- A data não pode ser futura nem anterior a 30 dias.
- `409` com código `veiculo_indisponivel` quando o veículo já está reservado ou vendido.
- `404` quando o veículo não existe na cópia local.

**`POST /webhooks/pagamentos`** recebe o resultado do pagamento. O corpo é o JSON exato assinado:

```json
{ "codigoPagamento": "<código devolvido por POST /vendas>", "resultado": "aprovado" }
```

- `resultado` é `aprovado` ou `cancelado`.
- O cabeçalho `X-Assinatura` leva o HMAC-SHA256 em hexadecimal, calculado sobre o corpo bruto com o
  segredo `Pagamentos:SegredoWebhook`. Assinatura ausente ou inválida responde `401`, antes de
  qualquer processamento.
- `200` com `{"alterou": true}` na primeira aplicação do resultado, e `{"alterou": false}` em
  repetições.
- `409` com código `transicao_invalida` quando o resultado contraria o estado atual (por exemplo,
  cancelar uma venda já concluída).
- `404` quando o `codigoPagamento` não existe.

**`PUT /internal/veiculos/{id}`** é o snapshot enviado pelo `veiculos-api`. Responde `200` ou `400`
com `ValidationProblem`.

## Configuração

Valores de desenvolvimento, já presentes em `appsettings.Development.json`. Não são de produção.

| Chave | Padrão de dev | Para que serve |
|---|---|---|
| `ConnectionStrings:VendasDb` | sem padrão | PostgreSQL deste serviço. Informar por variável de ambiente. |
| `Keycloak:EmissorPublico` | `http://localhost:8180/realms/revendax` | Emissor aceito nos tokens (o Keycloak do `veiculos-api`) |
| `Keycloak:EndpointToken` | `http://localhost:8180/.../token` | Obter token de serviço para chamar o `veiculos-api` |
| `Pagamentos:SegredoWebhook` | `dev-webhook-secret` | Segredo do HMAC do webhook. **Obrigatório** para iniciar. |
| `VeiculosApi:BaseUrl` | `http://localhost:5001` | Endereço do `veiculos-api`. **Obrigatório** para iniciar. |

## Como rodar localmente

### Dependências do serviço

O PostgreSQL sobe pelo compose deste repositório. O Keycloak **não** é deste repositório: os tokens
são emitidos pelo Keycloak do `veiculos-api` (`http://localhost:8180`, realm `revendax`). Suba-o
antes, pelo compose daquele repositório.

```bash
docker compose -f infra/docker-compose.yml up -d vendas-db
```

- PostgreSQL: `localhost:5434`.

### Rodando o serviço

Requer o SDK do .NET pinado em `global.json` (`10.0.401`). Como a string de conexão não tem padrão
nos arquivos de configuração, informe por variável de ambiente:

```bash
dotnet restore
ConnectionStrings__VendasDb='Host=localhost;Port=5434;Database=vendas;Username=vendas;Password=vendas' \
  dotnet run --project src/VendasApi --urls http://localhost:5002
```

Verificar:

```bash
curl http://localhost:5002/health
# {"status":"healthy"}
```

Em `Development`, a documentação interativa (Scalar) fica em `http://localhost:5002/scalar`.

**Observação:** sem o `veiculos-api` de pé, a listagem fica vazia, porque os veículos chegam por ele.
Para o fluxo completo, veja **Rodando junto com o `veiculos-api`**.

### Usuários e tokens de desenvolvimento

Credenciais de dev, não são de produção.

```bash
# Token de vendedor (usuário vendedor-dev / dev-vendedor)
curl -s -d grant_type=password -d client_id=revendax-dev-cli \
  -d username=vendedor-dev -d password=dev-vendedor \
  http://localhost:8180/realms/revendax/protocol/openid-connect/token
```

### Exemplo: listar veículos à venda

```bash
curl "http://localhost:5002/veiculos?pagina=1&tamanho=20"
```

### Exemplo: efetuar uma venda

```bash
TOKEN=$(curl -s -d grant_type=password -d client_id=revendax-dev-cli \
  -d username=vendedor-dev -d password=dev-vendedor \
  http://localhost:8180/realms/revendax/protocol/openid-connect/token | jq -r .access_token)

curl -i -X POST http://localhost:5002/vendas \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"veiculoId":"<guid>","cpf":"52998224725","dataVenda":"2026-11-01"}'
```

### Exemplo: confirmar o pagamento pelo webhook

O corpo precisa ser exatamente o que foi assinado. O `printf '%s'` evita quebra de linha extra.

```bash
CORPO='{"codigoPagamento":"<codigo>","resultado":"aprovado"}'
ASSINATURA=$(printf '%s' "$CORPO" | openssl dgst -sha256 -hmac dev-webhook-secret -hex | awk '{print $NF}')

curl -i -X POST http://localhost:5002/webhooks/pagamentos \
  -H "Content-Type: application/json" -H "X-Assinatura: $ASSINATURA" \
  --data-binary "$CORPO"
```

## Rodando junto com o `veiculos-api`

Os dois serviços se comunicam por HTTP e usam **um único Keycloak**, o do `veiculos-api`. Para o
fluxo completo (cadastro, venda e confirmação):

1. Suba o `veiculos-api` pelo compose dele (inclui o Keycloak).
2. Suba este serviço pelo compose deste repositório: `docker compose -f infra/docker-compose.yml up --build`.

O compose deste serviço já aponta para o Keycloak e para o `veiculos-api` pela máquina
(`host.docker.internal`), então não é preciso configurar nada a mais.

## Como testar

```bash
dotnet test
```

Os testes de integração sobem um PostgreSQL e um Keycloak reais via Testcontainers. Por isso é
preciso ter Docker disponível. Os contêineres são criados a cada execução.

Com cobertura, pelo mesmo gate usado no CI (mínimo 80% de linhas):

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
├── src/
│   ├── VendasApi.Domain/           # veículo da cópia local, venda, transições, CPF e data
│   ├── VendasApi.Application/      # casos de uso e portas
│   ├── VendasApi.Infrastructure/   # EF Core, migrações, consultas, clientes HTTP, assinatura
│   └── VendasApi/                  # API: controllers, autorização, Program.cs, Dockerfile
├── tests/VendasApi.Tests/          # testes de domínio, aplicação e integração (xUnit)
├── infra/docker-compose.yml        # serviço e PostgreSQL (Keycloak fica no veiculos-api)
└── .github/workflows/ci.yml        # build, lint, testes com cobertura ≥80% e scan de imagem
```

## Limitações conhecidas

- **Notificação ao `veiculos-api` sem nova tentativa e sem fila.** Se a notificação de venda
  concluída falhar, a venda fica concluída aqui e o veículo continua disponível lá. O erro aparece
  no log, e o webhook responde `200` mesmo assim.
- **Reserva sem expiração.** Uma venda reservada que nunca recebe o webhook segura o veículo
  indefinidamente.
- **CPF armazenado em texto.** A criptografia ou a tokenização fica para antes de qualquer uso em
  produção. A resposta da API já mascara o CPF.
- **Assinatura do webhook provisória.** O esquema (HMAC-SHA256 em `X-Assinatura`) vale até que o
  provedor de pagamento seja definido.
- **Depende do Keycloak do `veiculos-api`.** Os tokens só são aceitos se esse Keycloak estiver de
  pé em `localhost:8180`.

## CI/CD

Todo Pull Request roda `dotnet restore`, `dotnet format --verify-no-changes`, `dotnet build`,
`dotnet test` com gate de cobertura de linha ≥80%, build da imagem Docker e scan de vulnerabilidades
com Trivy, que falha em qualquer HIGH ou CRITICAL. A branch `main` é protegida: só aceita merge com
o workflow de CI verde.

### Deploy no Kubernetes (kind)

Os manifests ficam em `deploy/k8s/` (namespace `vendas`, API, banco próprio com `StatefulSet`,
`ConfigMap` e `Secret` de dev). O cluster de teste fica em `deploy/kind/cluster.yaml`.

Testado localmente: o `Deployment` sobe, o pod fica `Ready` sem reinícios e `GET /health` responde
`{"status":"healthy"}` dentro do cluster. Para reproduzir (o registry local e o cluster são os mesmos
do `veiculos-api`):

```bash
docker build -t localhost:5000/vendas-api:dev -f src/VendasApi/Dockerfile .
docker push localhost:5000/vendas-api:dev
kubectl apply -k deploy/k8s
kubectl -n vendas rollout status deployment/vendas-api --timeout=300s
```

### Pipeline no GitHub Actions

O workflow `.github/workflows/ci.yml` tem quatro jobs:

- **`build-test`** (PR e `main`): build, testes com cobertura ≥80%, scan da imagem com Trivy.
- **`validar-manifests`** (PR e `main`): renderiza o kustomize e valida o schema com kubeconform.
- **`publicar-imagem`** (só merge em `main`): publica em `ghcr.io/andersonnunes/vendas-api` com tag do SHA.
- **`deploy`** (só merge em `main`): cria um `kind` efêmero, aponta a imagem para o digest, aplica
  `deploy/k8s`, espera o rollout e faz smoke test em `/health`.

**Estado:** o workflow rodou no GitHub com sucesso (execução manual via `workflow_dispatch`): as quatro
jobs passaram, incluindo a publicação no GHCR e o deploy no `kind` com smoke test em `/health`.
