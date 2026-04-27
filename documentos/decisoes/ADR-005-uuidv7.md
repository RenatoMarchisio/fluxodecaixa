# ADR-005 — Usar UUIDv7 como identificador da entidade FluxoDeCaixa

- **Status:** Aceita
- **Data:** 2026-04-26
- **Tags:** modelagem, performance, idempotência

## Contexto

A tabela `FluxoDeCaixa` usa `[ID] uniqueidentifier` como PK clusterizada. UUIDv4 (aleatório) gera **fragmentação** severa em índices clustered B-tree, porque cada novo INSERT cai em página aleatória do índice.

Por outro lado, **GUIDs sequenciais** (`NEWSEQUENTIALID()` do SQL) só funcionam se o ID for gerado no servidor — o que impede o cliente de saber o ID antes do round-trip e dificulta idempotência (re-tentativa de INSERT após timeout duplica).

## Opções consideradas

| Opção | Geração | Ordenável? | Idempotência | Fragmentação |
|---|---|---|---|---|
| **UUIDv4** (random) | qualquer lado | não | ⭐⭐⭐ | ⭐ ruim |
| **NEWSEQUENTIALID** (SQL) | só no servidor | sim | ⭐ | ⭐⭐⭐⭐⭐ ótima |
| **BIGINT IDENTITY** | só no servidor | sim | ⭐ | ⭐⭐⭐⭐⭐ |
| **UUIDv7** (RFC 9562) | qualquer lado | **sim** (timestamp embutido nos primeiros 48 bits) | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ |

## Decisão

**UUIDv7 gerado no comando** via lib `Medo.Uuid7 3.x`:

```csharp
public abstract class CreateFluxoDeCaixaBaseCommand : IRequest<BaseResponse<bool>>
{
    public Guid ID
    {
        get => ID;
        internal set
        {
            // RFC 9562 — UUIDv7 baseado em timestamp, ordenável → bom para B-tree
            var uuid = Uuid7.NewUuid7();
            ID = uuid.ToGuid();
        }
    }
    ...
}
```

## Por quê

1. **Cluster-friendly** — ordenado por tempo, INSERTs vão sempre na "ponta direita" do B-tree, eliminando fragmentação.
2. **Idempotência** — o cliente recebe o `ID` no `BaseResponse` (ou ele mesmo gera no client-side em retry); se o cliente fizer retry com mesmo `ID`, o INSERT falha pela PK e o sistema responde "já existe" sem duplicar.
3. **Cliente-side friendly** — não precisa esperar o servidor para saber o ID (útil para *eventually consistent* e command-bus async).
4. **Padrão moderno** — RFC 9562 publicada em 2024, suportada por bibliotecas em todas as stacks.

## Consequências

- ✅ Performance de INSERT estável mesmo em milhões de linhas.
- ✅ Logs e índices ordenam naturalmente por tempo (debugging mais fácil).
- ⚠️ Tipo `Guid` no SQL = 16 bytes — não economiza espaço vs `BIGINT` (8 bytes), mas dá distribuição global (ID único entre serviços/regiões).
- ⚠️ Bibliotecas mais antigas podem não reconhecer o "subtipo" v7 — tratam como v4 (sem prejuízo, só perdem a ordenação semântica).

## Referências

- [RFC 9562 — Universally Unique IDentifiers (UUIDs)](https://www.rfc-editor.org/rfc/rfc9562)
- [Medo.Uuid7 (NuGet)](https://www.nuget.org/packages/Medo.Uuid7/)
- "*The Pain of Indexing GUIDs*" — Kimberly Tripp.
