# Desafio Técnico — API de Empréstimos Concorrentes e Auditáveis

API REST desenvolvida em **.NET 10 LTS** para gerenciamento de catálogo e empréstimos de uma biblioteca, com suporte a alta concorrência de empréstimos, rastreabilidade completa de auditoria, cache distribuído e tolerância a múltiplas réplicas.

---

## 🛠 Stack Tecnológica

- **Linguagem & Framework:** C# / .NET 10 LTS (ASP.NET Core Web API)
- **Banco de Dados Relacional:** PostgreSQL 17 (Entity Framework Core + provedor Npgsql)
- **Cache Distribuído:** Redis 7 (StackExchange.Redis)
- **Mensageria & Padrões:** MediatR (Mediator / CQRS), FluentValidation, CSharpFunctionalExtensions
- **Observabilidade:** OpenTelemetry (Métricas e Traces customizados), Serilog (Logs estruturados com CorrelationId), Aspire Dashboard
- **Contêineres e Orquestração:** Docker Compose e Manifests Kubernetes (Deployment, Service, ConfigMap, Secret e HPA)
- **Testes Automatizados:** xUnit, FluentAssertions, NSubstitute e Testcontainers (PostgreSQL e Redis efêmeros)

---

## ⚙️ Variáveis de Ambiente Necessárias

A aplicação pode ser configurada via `appsettings.json` ou sobrescrita através de variáveis de ambiente. Segredos reais não são versionados no repositório.

| Variável | Descrição | Exemplo de Valor (Local/Docker) |
| :--- | :--- | :--- |
| `ConnectionStrings__PostgreSql` | String de conexão com o PostgreSQL | `Host=postgres;Port=5432;Database=library_db;Username=postgres;Password=postgres` |
| `ConnectionStrings__Redis` | Host e porta de conexão com o Redis | `redis:6379` |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Endpoint para envio de métricas e traces via protocolo OTLP | `http://aspire-dashboard:4317` |
| `ASPNETCORE_ENVIRONMENT` | Ambiente de execução (`Development`, `Staging`, `Production`, `Testing`) | `Development` |
| `ASPNETCORE_HTTP_PORTS` | Porta HTTP interna exposta pela aplicação | `8080` |

---

## 🚀 Como Executar Localmente

### Pré-requisitos
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker & Docker Compose](https://www.docker.com/)

### Opção 1: Subir todo o ambiente com Docker Compose (Recomendado)
Na pasta `src`, execute:
```bash
docker compose up -d --build
```

Serviços iniciados:
- **Library.API:** `http://localhost:8080`
  - Documentação Scalar UI: `http://localhost:8080/scalar/v1`
  - Especificação OpenAPI: `http://localhost:8080/openapi/v1.json`
  - Health checks: `http://localhost:8080/health/live` e `http://localhost:8080/health/ready`
- **PostgreSQL 17:** `localhost:5432` (banco `library_db`, usuário `postgres`, senha `postgres`)
- **Redis 7:** `localhost:6379`
- **Aspire Dashboard (OTEL Traces & Metrics):** `http://localhost:18888`
- **Adminer (UI PostgreSQL):** `http://localhost:8085`
- **Redis Commander (UI Redis):** `http://localhost:8086`

### Opção 2: Executar a infraestrutura no Docker e a API pelo .NET CLI / IDE
1. Na pasta `src`, inicie apenas os contêineres de suporte:
   ```bash
   docker compose up postgres redis aspire-dashboard -d
   ```
2. Na raiz da solução ou na pasta `src`, execute a API:
   ```bash
   dotnet run --project src/Library.API/Library.API.csproj
   ```

---

## 🗄️ Como Executar Migrations

A aplicação aplica as migrations pendentes do Entity Framework Core automaticamente durante a inicialização (`db.Database.MigrateAsync()`).

Para inspecionar, criar ou aplicar migrations manualmente pelo terminal:

```bash
# Instale a ferramenta dotnet-ef (caso ainda não possua)
dotnet tool install --global dotnet-ef

# Aplicar migrations pendentes no banco
dotnet ef database update --project src/Library.Infra.EF/Library.Infra.EF.csproj --startup-project src/Library.API/Library.API.csproj

# Criar uma nova migration (se alterar o modelo de dados)
dotnet ef migrations add NomeDaMigration --project src/Library.Infra.EF/Library.Infra.EF.csproj --startup-project src/Library.API/Library.API.csproj
```

---

## 🧪 Testes Automatizados

A suíte de testes é dividida em testes unitários e de integração:

* **Testes Unitários:** executam em memória pura com execução ultra-rápida (menos de 200 ms), validando regras de domínio, transições de estado de empréstimo e comandos isolados.
* **Testes de Integração:** utilizam o **Testcontainers**, inicializando contêineres reais de PostgreSQL e Redis sob demanda durante os testes, sem exigir bancos previamente configurados.

### Executar todos os testes:
```bash
dotnet test
```

### Executar projetos isoladamente:
```bash
# Apenas testes unitários
dotnet test tests/Library.UnitTests/Library.UnitTests.csproj

# Apenas testes de integração (PostgreSQL + Redis via Testcontainers)
dotnet test tests/Library.IntegrationTests/Library.IntegrationTests.csproj
```

### Cenários cobertos:
1. **Concorrência do último exemplar (`ConcurrencyLoanTests.cs`):** Cadastra um livro com 1 exemplar disponível e dispara 10 requisições simultâneas via `Task.WhenAll`. Exatamente 1 requisição retorna `201 Created` e as 9 restantes são rejeitadas por conflito/estoque. O saldo final do livro permanece 0 (nunca negativo).
2. **Idempotência (`IdempotencyLoanTests.cs`):** Dispara duas requisições idênticas com o mesmo cabeçalho `Idempotency-Key`. A segunda chamada devolve a resposta previamente gerada (`200 OK`) sem criar um segundo empréstimo e sem descontar estoque novamente.
3. **Preservação de Histórico e Auditoria (`LoanHistoryAndAuditTests.cs`):** Comprova que devoluções e cancelamentos restauram a quantidade disponível, mantêm o histórico do livro e do usuário preservados (sem exclusão física) e registram a trilha de auditoria em `GET /audit-events`.
4. **Catálogo e Unicidade de ISBN (`BooksControllerTests.cs`):** Validação de campos obrigatórios, bloqueio de ISBN duplicado e desativação protegida de livros com histórico (*soft delete*).
5. **Invalidação de Cache (`CacheInvalidationTests.cs`):** Valida que leituras em `/books/{id}/availability` são servidas pelo Redis e invalidadas automaticamente após qualquer empréstimo.
6. **Health Checks (`HealthChecksTests.cs`):** Respostas de status para `/health/live` e `/health/ready`.

---

## 📐 Decisões Arquiteturais e Trade-offs

### 1. Concorrência: Concorrência Otimista com ConcurrencyToken
Para gerenciar empréstimos simultâneos de um mesmo livro entre múltiplas instâncias da API, utilizamos concorrência otimista com a propriedade `ConcurrencyToken` (tipo `Guid`) mapeada na entidade `Book` com `.IsConcurrencyToken()` no Entity Framework Core:

* **Como funciona:** Toda vez que um exemplar é debitado ou creditado, um novo `Guid` é gerado para o `ConcurrencyToken`. Ao persistir via `SaveChangesAsync()`, o EF Core executa um `UPDATE` condicional que valida se o token atual no banco corresponde ao lido pela transação:
  ```sql
  UPDATE "Books"
  SET "AvailableCopies" = @copies, "ConcurrencyToken" = @newToken
  WHERE "Id" = @id AND "ConcurrencyToken" = @oldToken;
  ```
  Se outra requisição comitar uma reserva milissegundos antes, o token terá mudado, resultando em 0 linhas afetadas e disparando uma `DbUpdateConcurrencyException`. O `UnitOfWork` intercepta essa exceção, converte para `ConcurrencyConflictException` e a API responde ao cliente com `409 Conflict`.
* **Trade-off:**
  * *Vantagens:* Não bloqueia leituras no PostgreSQL, dispensa locks distribuídos em memória e oferece alta vazão com baixo overhead de conexões no banco.
  * *Desvantagens:* Em cenários de altíssima contenção sobre um único exemplar, requisições concorrentes perdedoras falham de imediato, transferindo para o cliente ou para uma política de retentativas a decisão de tentar novamente.

### 2. Idempotência: Persistência Centralizada por Chave de Idempotência
O endpoint `POST /loans` exige o cabeçalho HTTP `Idempotency-Key`.
* **Como funciona:** Antes de consultar o catálogo ou debitar estoque, o handler consulta o repositório de persistência (`loanRepository.GetByIdempotencyKeyAsync`). Se a chave já existir, a requisição é identificada como repetida, a métrica `library.loans.idempotent.hits` é incrementada e o objeto original do empréstimo é retornado imediatamente com status `200 OK`, sem tocar no estoque do livro.
* **Trade-off:** Manter a chave gravada diretamente na tabela `Loans` garante consistência imediata com zero dependências externas adicionais, mas atrela a chave de idempotência ao ciclo de vida da entidade (sem expiração automática por TTL).

### 3. Cache Distribuído no Redis (Padrão Cache-Aside)
* **Como funciona:** O endpoint `GET /books/{id}/availability` verifica primeiro o Redis. Se a chave existir, devolve os dados em cache. Em caso de *cache miss*, busca no PostgreSQL, preenche o Redis com TTL de 10 minutos e retorna. Qualquer mutação que afete o estoque (empréstimo, devolução ou edição de livro) remove explicitamente a chave correspondente (`books:{bookId}:availability`) e invalida o catálogo consolidado.
* **Trade-off:** O PostgreSQL continua como fonte única da verdade para a decisão de empréstimo. O Redis atua exclusivamente como acelerador de leituras.

### 4. Auditoria de Domínio: Registro Síncrono e Atômico
* **Como funciona:** Todas as ações de negócio relevantes (criação, edição e desativação de livro, bem como criação, devolução e cancelamento de empréstimo) criam um registro na tabela `AuditEvents`. Esse registro captura a entidade, o ID, a ação executada, o autor (`Actor`), o `CorrelationId`, o timestamp UTC e o snapshot da mudança serializado em JSON.
* **Trade-off:** O evento de auditoria é persistido dentro da mesma transação do comando via `UnitOfWork.CommitAsync()`. Isso garante atomicidade (se a criação do empréstimo falhar, nenhum log de auditoria falso é gravado), com o custo de uma inserção relacional adicional na mesma transação.

---

## ☸️ Orquestração e Kubernetes (2 a 11 Réplicas)

Os manifests estão organizados no diretório `/k8s`:

```
k8s/
├── configmap.yaml       # Configurações de ambiente (OTEL, log, hosts)
├── secret.yaml          # Template de segredos e connection strings
├── deployment.yaml      # Pods, réplicas, estratégia de deploy, probes e limites
├── service.yaml         # Service ClusterIP balanceando a porta 80 para a 8080 dos pods
├── hpa.yaml             # Horizontal Pod Autoscaler (escala automática de 2 a 11 pods)
└── kustomization.yaml   # Agrupador Kustomize
```

### Como a aplicação permanece correta entre 2 e 11 réplicas:

1. **Camada de Aplicação Stateless:** A `Library.API` não armazena sessão, contadores locais ou estado em memória de processo. Qualquer requisição pode ser atendida por qualquer um dos pods sem divergência de estado.
2. **PostgreSQL como Ponto Central de Concorrência:** O controle de estoque nunca é baseado em memória de instância local. O `ConcurrencyToken` no PostgreSQL rejeita qualquer tentativa concorrente de débito do mesmo exemplar que ocorra em pods distintos simultaneamente.
3. **Cache Centralizado e Invalidação Imediata:** Como todas as réplicas utilizam o mesmo cluster Redis, quando o Pod A conclui um empréstimo e remove a chave de cache, os Pods B a K passam a ler o valor atualizado na consulta seguinte.
4. **Idempotência Compartilhada:** Se um cliente reenviar uma requisição idêntica e o balanceador encaminhá-la para um pod diferente do que processou a primeira chamada, a consulta no banco identifica a `IdempotencyKey` e reproduz o retorno sem duplicidade.
5. **Roteamento Seguro com Probes:** O `Service` direciona tráfego apenas para pods com a **Readiness Probe** (`/health/ready`) respondendo `200 OK`. Caso uma réplica perca temporariamente contato com o banco ou com o Redis, ela é removida do balanceamento pelo Kubernetes sem impactar os demais pods.

Para aplicar os manifests no cluster:
```bash
kubectl apply -k ./k8s
```

---

## ⚠️ Limitações Conhecidas

1. **Contexto de Usuário Simulado:** O ator das operações é obtido via `ICurrentUserContext`, que atualmente retorna `Anonymous/System` com o CorrelationId da requisição. Não foi integrado um Identity Provider (OpenID Connect / JWT) neste desafio.
2. **Idempotência sem Expiração Temporal (TTL):** A checagem de idempotência depende de consultar a chave na tabela de empréstimos do PostgreSQL. Isso significa que chaves antigas permanecem indexadas por tempo indeterminado, sem uma política de expiração automática (TTL de 24h a 72h).
3. **Contenção sob Rajadas Extremas no Mesmo Exemplar:** Em casos de milhares de pessoas disputando o mesmo exemplar no mesmo milissegundo, a concorrência otimista gera muitas rejeições imediatas com `409 Conflict`. A aplicação não enfileira requisições internamente para reprocessamento sequencial.
4. **Auditoria na Mesma Base Transacional:** Gravar auditoria diretamente no banco relacional operacional (OLTP) aumenta o volume de dados e o tempo de transação em tabelas centrais à medida que a aplicação cresce.

---

## 🚀 Como Evoluir a Solução para Produção

Para elevar a arquitetura a um ambiente corporativo de grande escala, as seguintes melhorias seriam priorizadas:

1. **Autenticação e Autorização (OAuth2 / OIDC):**
   - Integrar autenticação baseada em JWT Bearer com Azure AD, Keycloak ou Auth0.
   - Implementar controle de acesso baseado em papéis (RBAC) para restringir endpoints administrativos (`POST /books`, `PATCH /books/{id}`, `DELETE /books/{id}`) aos operadores da biblioteca, permitindo aos leitores apenas consultar e solicitar empréstimos.
2. **Camada de Idempotência em Memória (Redis):**
   - Armazenar a `Idempotency-Key` no Redis com lock distribuído temporário durante o processamento e retenção da resposta original com TTL de 24 horas. Isso evita consultas ao PostgreSQL para requisições repetidas frequentes.
3. **Padrão Outbox Transacional e Mensageria (Kafka / RabbitMQ):**
   - Desacoplar a auditoria e as notificações (e-mails de aviso de devolução, lembretes de atraso) através do padrão Outbox. O handler grava o evento em uma tabela local leve, e um relay assíncrono (como Debezium ou um worker em background) publica os eventos para um broker de mensageria.
4. **Réplicas de Leitura (Read Replicas no PostgreSQL):**
   - Separar as conexões de leitura e escrita. Consultas volumosas como catálogo (`GET /books`), disponibilidade e histórico de auditoria seriam direcionadas a réplicas de leitura com balanceamento, reservando o nó primário para transações de escrita e locks otimistas.
5. **Rate Limiting e Resiliência:**
   - Adicionar middleware de Rate Limiting (`System.Threading.RateLimiting` nativo do .NET) ou proteção no Ingress/Cloudflare para limitar disparos abusivos por IP ou usuário.
   - Implementar políticas de resiliência com Polly (retry exponencial e circuit breaker) para operações de leitura e escrita no Redis.
6. **Segurança Avançada no Kubernetes:**
   - Implementar Ingress Controller com encerramento TLS automático (cert-manager com Let's Encrypt).
   - Gerenciar connection strings e credenciais através do External Secrets Operator integrado a cofres como HashiCorp Vault ou AWS Secrets Manager, eliminando segredos em arquivos de configuração.
   - Definir `NetworkPolicies` para restringir a comunicação de rede no cluster, garantindo que apenas a API acesse o banco de dados e o Redis.
