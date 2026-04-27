using Dapper;
using FluxoDeCaixa.Application.Dto;
using FluxoDeCaixa.Application.Interface.Persistence;
using FluxoDeCaixa.Domain.Entities;
using FluxoDeCaixa.Persistence.Contexts;
using System.Data;

namespace FluxoDeCaixa.Persistence.Repositories
{
    public class FluxoDeCaixaCreditoRepository : IFluxoDeCaixaCreditoRepository
    {
        private readonly DapperContextFC _applicationContext;
        public FluxoDeCaixaCreditoRepository(DapperContextFC applicationContext) 
        {
            _applicationContext = applicationContext ?? throw new ArgumentNullException(nameof(applicationContext));
        }

        public async Task<bool> InsertAsync(FluxoDeCaixaCredito fluxoDeCaixaCredito)
        {
            using var connection = _applicationContext.CreateConnection();
            var query = "INSERT INTO FluxoDeCaixa ([ID],[dataFC],[credito],[descricao]) " +
                        "            VALUES (@ID, @dataFC, @credito,@descricao)";

            var recordsAffected = await connection.ExecuteAsync(query, fluxoDeCaixaCredito);
            return recordsAffected > 0;
        }

        public async Task<bool> UpdateAsync(FluxoDeCaixaCredito fluxoDeCaixaCredito)
        {
            using var connection = _applicationContext.CreateConnection();
            var query = "UPDATE FluxoDeCaixa SET dataFC = @dataFC," +
                        "       credito = @credito,descricao = @descricao " +
                        "WHERE ID = @ID";

            var recordsAffected = await connection.ExecuteAsync(query, fluxoDeCaixaCredito);
            return recordsAffected > 0;
        }
        public Task<bool> DeleteAsync(string id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<FluxoDeCaixaCredito>> GetAllAsync()
        {
            throw new NotImplementedException();
        }

        public Task<FluxoDeCaixaCredito> GetAsync(string id)
        {
            throw new NotImplementedException();
        }
    }

    public class FluxoDeCaixaDebitoRepository : IFluxoDeCaixaDebitoRepository
    {
        private readonly DapperContextFC _applicationContext;
        public FluxoDeCaixaDebitoRepository(DapperContextFC applicationContext)
        {
            _applicationContext = applicationContext ?? throw new ArgumentNullException(nameof(applicationContext));
        }

        public async Task<bool> InsertAsync(FluxoDeCaixaDebito fluxoDeCaixaDebito)
        {
            using var connection = _applicationContext.CreateConnection();
            var query = "INSERT INTO FluxoDeCaixa ([ID],[dataFC],[debito],[descricao]) " +
                        "            VALUES (@ID, @dataFC, @debito,@descricao)";

            var recordsAffected = await connection.ExecuteAsync(query, fluxoDeCaixaDebito);
            return recordsAffected > 0;
        }

        public async Task<bool> UpdateAsync(FluxoDeCaixaDebito fluxoDeCaixaDebito)
        {
            using var connection = _applicationContext.CreateConnection();
            var query = "UPDATE FluxoDeCaixa SET dataFC = @dataFC," +
                        "       debito = @debito,descricao = @descricao " +
                        "WHERE ID = @ID";

            var recordsAffected = await connection.ExecuteAsync(query, fluxoDeCaixaDebito);
            return recordsAffected > 0;
        }
        public Task<bool> DeleteAsync(string id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<FluxoDeCaixaDebito>> GetAllAsync()
        {
            throw new NotImplementedException();
        }

        public Task<FluxoDeCaixaDebito> GetAsync(string id)
        {
            throw new NotImplementedException();
        }
    }

    public class FluxoDeCaixaRelatorioRepository : IFluxoDeCaixaRelatorioRepository
    {
        private readonly DapperContextFC _applicationContext;

        public FluxoDeCaixaRelatorioRepository(DapperContextFC applicationContext)
        {
            _applicationContext = applicationContext ?? throw new ArgumentNullException(nameof(applicationContext));
        }

        public async Task<IEnumerable<FluxoDeCaixaRelatorioDto>> GetFluxoDeCaixaRelatorioAsync(DateTime inicio, DateTime fim) 
        {
            using var connection = _applicationContext.CreateConnection();
            var query = "Select * From FluxoDeCaixaConsolidado where DataFC between @inicio AND @fim";

            var result = await connection.QueryAsync<FluxoDeCaixaRelatorioDto>(query,new { inicio = inicio.ToString("yyyy-MM-dd"), fim = fim.ToString("yyyy-MM-dd") });
            return result;
        }

        public Task<bool> DeleteAsync(string id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<FluxoDeCaixaRelatorio>> GetAllAsync()
        {
            throw new NotImplementedException();
        }

        public Task<FluxoDeCaixaRelatorio> GetAsync(string id)
        {
            throw new NotImplementedException();
        }
        public Task<bool> InsertAsync(FluxoDeCaixaRelatorio entity)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateAsync(FluxoDeCaixaRelatorio entity)
        {
            throw new NotImplementedException();
        }
    }
}
