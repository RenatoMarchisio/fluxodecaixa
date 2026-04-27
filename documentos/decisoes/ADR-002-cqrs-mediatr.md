# ADR-002 | Adotar CQRS leve com MediatR

- **Status:** Aceita
- **Data:** 2026-04-26
- **Tags:** padrão, application-layer, testabilidade

## Contexto

O sistema tem dois padrões de uso fundamentalmente diferentes:

| Caminho | Operação | Frequência | Tolerância a latência |
|---|---|---|---|
| Lançamento | Escrita transacional | Baixa-média | Estrita (UX em tempo real) |
| Relatório | Leitura agregada | Alta (pico 50 rps) | Mais flexível (cacheável) |

Forçar o mesmo modelo para os dois (anti-pattern *one-model-rules-them-all*) leva a:
- DTOs poluídos (campos só de leitura ou só de escrita).
- Validações fracas (mistura de regras de domínio com filtros de query).
- Acoplamento entre serviços que evoluirão em ritmos diferentes.

## Opções consideradas

| Opção | Pró | Contra |
|---|---|---|
| **Service Layer monolítico** (1 `IFluxoDeCaixaService`) | Simples; familiar | Mistura escrita e leitura; difícil testar Behaviours globais |
| **CQRS com MediatR** (escolhida) | Commands ≠ Queries; Behaviours globais (validação, log, perf); altamente testável | Curva inicial; mais arquivos |
| **CQRS pesado** (Event Sourcing + Read DB separado) | Auditabilidade total | Overkill para o desafio |

## Decisão

**CQRS leve com MediatR**:
- Comandos = `IRequest<BaseResponse<T>>` (mutam estado).
- Queries = `IRequest<BaseResponse<IEnumerable<T>>>` (apenas leem).
- **Pipeline Behaviours** transversais (`ValidationBehaviour`, `LoggingBehaviour`, `PerformanceBehaviour`).
- Endpoints ASP.NET Minimal API são **finos** — só chamam `mediator.Send`.

## Consequências

### Positivas
- **Cross-cutting** (validação/log/perf) acontece **uma vez no pipeline**, não em cada handler.
- **Handlers são puros**: dependências são interfaces (UoW, IMapper) → mock trivial em testes.
- **Read model é separável**: a Query do Relatório não toca em entidade de domínio — devolve `FluxoDeCaixaRelatorioDto` direto.
- Facilita evolução para **CQRS físico** (read DB separado) sem reescrever handlers.

### Negativas
- Time precisa entender a convenção (`Command` vs `Query`, `IRequest<T>`, `IPipelineBehavior<,>`).
- Mais arquivos por feature (Command + Validator + Handler + DTO).
  - Mitigado por **template** em `src/FluxoDeCaixa.Application.UseCases/FluxoDeCaixa/Commands/CreateFluxoDeCaixaCommand/` que serve de molde.

## Notas sobre reúso de assemblies entre serviços

Hoje, **Lançamentos e Relatório referenciam o mesmo `Application.UseCases`**. Isso é **proposital** porque:
- Os modelos ainda são pequenos.
- Ambos validators/handlers vivem juntos.

**Sinal para separar:** quando os modelos divergirem (read model muito diferente do write model) ou quando os times forem fisicamente separados, criar `FluxoDeCaixa.Lancamentos.Application` e `FluxoDeCaixa.Relatorio.Application` distintos.
