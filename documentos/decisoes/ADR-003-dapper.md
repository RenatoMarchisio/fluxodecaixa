# ADR-003 | Persistência com Dapper (em vez de Entity Framework Core)

- **Status:** Aceita
- **Data:** 2026-04-26
- **Tags:** persistência, performance

## Contexto

O Relatório precisa absorver **50 req/s** com no máximo 5% de perda. O caminho de leitura é uma `SELECT ... WHERE dataFC BETWEEN ...` simples sobre uma tabela pré-agregada.

Para o caminho de escrita, os comandos são `INSERT` simples sem grafos de objetos.

## Opções consideradas

| Critério | **Dapper** (escolhida) | EF Core | ADO.NET puro |
|---|---|---|---|
| Performance bruta | ⭐⭐⭐⭐⭐ (≈ ADO.NET) | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| SQL explícito (DBA-friendly) | ⭐⭐⭐⭐⭐ | ⭐⭐ (LINQ) | ⭐⭐⭐⭐⭐ |
| Produtividade | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐ |
| Migrations / Code-first | ❌ não | ✅ sim | ❌ não |
| Cold-start | ⭐⭐⭐⭐⭐ | ⭐⭐ (model build inicial) | ⭐⭐⭐⭐⭐ |
| Footprint de memória | Baixo | Médio-alto | Baixo |
| Curva de aprendizado | Baixa | Média | Baixa |

## Decisão

**Dapper 2.1.x** sobre `System.Data.SqlClient`. Justificativas:

1. **Performance**: micro-ORM com overhead próximo de zero. Cada handler roda em <10ms com folga (limiar do `PerformanceBehaviour`).
2. **SQL explícito**: o time financeiro precisa **revisar e tunar queries** LINQ-to-SQL torna isso opaco.
3. **Cold-start desprezível**: importante em serverless / autoscale.
4. **Mapeamentos customizados** (ex.: `DateOnly` ↔ `DATE`) são triviais via `SqlMapper.AddTypeHandler`.

## Como atenuamos as desvantagens

| Desvantagem | Mitigação |
|---|---|
| Sem migrations automáticas | Schema versionado em `Sql/*.sql` + uso de **DbUp** ou **DACPAC** no CI/CD (roadmap). |
| Risco de SQL injection | **Sempre parametrizar** via `@param` (Dapper bloqueia concatenação). Já implementado em todos os repositórios. |
| Boilerplate por entidade | Usamos `IGenericRepository<T>` para CRUD básico + repositórios especializados só para queries customizadas. |
| Sem change tracking | Não é necessário — comandos são pequenos e explícitos (Insert/Update direto). |

## Implementação relevante

```csharp
// FluxoDeCaixa.Infrastructure/Dapper/DateOnlyTypeHandler.cs
public class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly value) {
        parameter.Value = value.ToDateTime(TimeOnly.MinValue);
        parameter.DbType = DbType.Date;
    }
    public override DateOnly Parse(object value) =>
        value is DateOnly d ? d : DateOnly.FromDateTime((DateTime)value);
}

// Registrado no Program.cs antes do builder:
SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
```

Sem isso, `DateOnly` daria `InvalidCastException` ao mapear para `DATE` do SQL Server. Esse é exatamente o tipo de detalhe que **Dapper expõe** e EF Core esconderia até quebrar em runtime de outra forma.

## Consequências

- ✅ Performance e previsibilidade.
- ✅ DBA pode revisar e otimizar SQL diretamente.
- ⚠️ Migrations precisam ser disciplinadas (versionadas em `Sql/`).
- ⚠️ Repos precisam ser testados (integração com banco) recomendado **Testcontainers** com container SQL Server efêmero (roadmap).
