================================================================================
  FLUXO DE CAIXA | SOLUÇÃO DE ARQUITETURA CORPORATIVA
  Desafio do Arquiteto Corporativo de TI
================================================================================

Implementação de referência em .NET 8 / C# para o problema de Fluxo de Caixa
diário do comerciante (lançamentos de débito/crédito + consolidado diário),
estruturada em microsserviços com API Gateway e Clean Architecture + CQRS.

--------------------------------------------------------------------------------
1) PROBLEMA DE NEGÓCIO
--------------------------------------------------------------------------------
Um comerciante precisa:
  - Lançar movimentações diárias (créditos e débitos);
  - Consultar um relatório consolidado por dia (saldo agregado).

Restrição não-funcional crítica:
  - O serviço de Lançamentos NÃO PODE ficar indisponível se o Relatório cair.
  - O serviço de Relatório precisa absorver picos de 50 req/s com no máximo
    5% de perda.

--------------------------------------------------------------------------------
2) VISÃO GERAL DA ARQUITETURA
--------------------------------------------------------------------------------

     +------------------+       +-----------------------+
     |  Comerciante /   | HTTPS |   API Gateway (YARP)  |
     |  Sistema POS     |------>|   :5000               |
     +------------------+       +-----------+-----------+
                                            |
                       +--------------------+--------------------+
                       |                                         |
                       v                                         v
           +---------------------------+         +---------------------------+
           | FluxoDeCaixa.WebApi       |         | FluxoDeCaixaRelatorio     |
           | (Lançamentos)  :8000      |         | .WebApi (Relatório) :8500 |
           +---------------------------+         +---------------------------+
                       |                                         |
                       +-------------------+---------------------+
                                           |
                                           v
                              +-----------------------+
                              |   SQL Server          |
                              |   FluxoCaixa DB       |
                              +-----------------------+

Estilo:
  - MICROSSERVIÇOS independentes (isolamento de falhas).
  - API GATEWAY com YARP (roteamento + futura agregação/cross-cutting).
  - CLEAN ARCHITECTURE em cada serviço (Domain → App → Infra → Presentation).
  - CQRS via MediatR (Commands separados de Queries).
  - Pipeline Behaviours para Validação, Logging e Performance.
  - DAPPER (micro-ORM) sobre SQL Server, com Repository + Unit of Work.

--------------------------------------------------------------------------------
3) ESTRUTURA DE PASTAS
--------------------------------------------------------------------------------

FluxoDeCaixa/
  FluxoDeCaixa.sln              Solução .NET 
  docker/Sql/init.sql           DDL das tabelas
  docker/                       Imagens Docker + docker-compose
  docs/                         Documentação completa
    arquitetura/c4/             Modelo C4 (Contexto, Containers, Componentes, Código, Deploy)
    arquitetura/uml/            Diagramas UML (Classe, Sequência, Componentes, Atividade, Casos de Uso)
    requisitos/                 Funcionais e não-funcionais
    dominio/                    Mapa de domínio funcional
    decisoes/                   ADRs (Architecture Decision Records)
    operacao/                   Custos, observabilidade, segurança, transição, futuro
  src/
    FluxoDeCaixa.Domain/                  Entidades + Eventos
    FluxoDeCaixa.Application.Dto/         DTOs
    FluxoDeCaixa.Application.Interface/   Contratos (IRepository, IUnitOfWork)
    FluxoDeCaixa.Application.UseCases/    Commands, Queries, Handlers, Behaviours
    FluxoDeCaixa.Infrastructure/          Helpers (Dapper TypeHandler)
    FluxoDeCaixa.Persistence/             DapperContext, Repositórios, UoW
    FluxoDeCaixa.WebApi/                  Microsserviço LANÇAMENTOS  (porta 8000)
    FluxoDeCaixaRelatorio.WebApi/         Microsserviço RELATÓRIO    (porta 8500)
    FluxoDeCaixa.Gateway/                 API Gateway YARP           (porta 5000)
    FluxoDeCaixaDLQ/                      BackEnd DLQ                

--------------------------------------------------------------------------------
4) COMO EXECUTAR — DOCKER (RECOMENDADO)
--------------------------------------------------------------------------------

Pré-requisitos: Docker Desktop / Docker Engine 24+ e Docker Compose v2.

   $ cd FluxoDeCaixa/docker
   $ docker compose up -d --build

Endereços:
   - Gateway (Swagger agregado) ............ http://localhost:5000/swagger
   - Lançamentos (Swagger direto) .......... http://localhost:8000
   - Relatório (Swagger direto)  ........... http://localhost:8500
   - SQL Server ............................. localhost,1433  (sa / Fluxo@2026Dev!)

--------------------------------------------------------------------------------
5) COMO EXECUTAR — LOCAL (.NET 8 SDK + SQL SERVER)
--------------------------------------------------------------------------------

   $ dotnet restore FluxoDeCaixa.sln
   $ sqlcmd -S "(localdb)\mssqllocaldb" -i "docker/Sql/init.sql"

   # Em três terminais separados (ou Multi-startup do Visual Studio):
   $ dotnet run --project src/FluxoDeCaixa.WebApi
   $ dotnet run --project src/FluxoDeCaixaRelatorio.WebApi
   $ dotnet run --project src/FluxoDeCaixa.Gateway
   $ dotnet run --project src/FluxoDeCaixaDLQ

Detalhes em SETUP.md.

--------------------------------------------------------------------------------
6) ENDPOINTS PRINCIPAIS
--------------------------------------------------------------------------------

POST /api/FluxoDeCaixa/InsertCredito
   { "dataFC": "2026-04-26", "descricao": "Venda balcão", "credito": 250.00 }

POST /api/FluxoDeCaixa/InsertDebito
   { "dataFC": "2026-04-26", "descricao": "Compra fornecedor", "debito": 100.00 }

GET  /api/FluxoDeCaixaRelatorio/Relatorio?inicio=2026-01-01&fim=2026-12-31

Resposta padrão (envelope BaseResponse<T>):
   { "succcess": true, "data": <T>, "message": "...", "errors": [...] }

--------------------------------------------------------------------------------
7) DOCUMENTAÇÃO DE ARQUITETURA
--------------------------------------------------------------------------------

Modelo C4 (Simon Brown):
  docs/arquitetura/c4/01-contexto.md       Nível 1 Contexto do sistema
  docs/arquitetura/c4/02-containers.md     Nível 2 Containers
  docs/arquitetura/c4/03-componentes.md    Nível 3 Componentes internos
  docs/arquitetura/c4/04-codigo.md         Nível 4 Detalhamento de classes
  docs/arquitetura/c4/05-deploy.md         Diagrama de deployment

UML:
  docs/arquitetura/uml/classes.md
  docs/arquitetura/uml/sequencia-lancamento.md
  docs/arquitetura/uml/sequencia-relatorio.md
  docs/arquitetura/uml/componentes.md
  docs/arquitetura/uml/atividade.md
  docs/arquitetura/uml/casos-de-uso.md

Decisões Arquiteturais (ADRs):
  ADR-001  Microsserviços
  ADR-002  CQRS com MediatR
  ADR-003  Dapper (em vez de EF Core)
  ADR-004  YARP como API Gateway
  ADR-005  UUIDv7 como identificador
  ADR-006  Estratégia de Resiliência (50 req/s, 95% uptime)

--------------------------------------------------------------------------------
8) PADRÕES APLICADOS
--------------------------------------------------------------------------------

  Clean Architecture (Onion)    Mediator (MediatR)
  CQRS                          Pipeline Behaviours (cross-cutting)
  Repository                    Unit of Work
  DTO + AutoMapper              FluentValidation (Specification)
  API Gateway                   Database per Service (lógico)
  Outbox Pattern (roadmap)      Strangler Fig (transição de legado)

--------------------------------------------------------------------------------
9) LICENÇA
--------------------------------------------------------------------------------

MIT — uso livre para fins educacionais e profissionais.

================================================================================
