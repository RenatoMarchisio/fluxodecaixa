using Dapper;
using FluxoDeCaixa.Application.Interface.Persistence;
using FluxoDeCaixa.Persistence.Contexts;

namespace FluxoDeCaixa.Persistence.Repositories
{
    public class FluxoDeCaixaConsolidadoRepository : IFluxoDeCaixaConsolidadoRepository
    {
        private readonly DapperContextFC _ctx;

        public FluxoDeCaixaConsolidadoRepository(DapperContextFC ctx)
        {
            _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
        }

        /// <summary>
        /// UPSERT atômico via MERGE:
        ///  - dia já existe  → soma credito/debito ao acumulado
        ///  - dia não existe → cria a linha (para datas fora do pré-agendamento)
        /// </summary>
        public async Task UpsertAsync(DateOnly dataFc, decimal credito, decimal debito)
        {
            const string sql = @"
MERGE INTO FluxoDeCaixaConsolidado WITH (HOLDLOCK) AS target
USING (SELECT @dataFc AS dataFC, @credito AS credito, @debito AS debito) AS source
   ON target.dataFC = source.dataFC
WHEN MATCHED THEN
    UPDATE SET
        credito  = target.credito + source.credito,
        debito   = target.debito  + source.debito
WHEN NOT MATCHED THEN
    INSERT (dataFC, credito, debito, criadoEm)
    VALUES (source.dataFC, source.credito, source.debito, SYSUTCDATETIME());
";
            using var connection = _ctx.CreateConnection();
            await connection.ExecuteAsync(sql, new { dataFc, credito, debito });
        }
    }
}
