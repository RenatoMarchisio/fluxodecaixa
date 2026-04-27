using Dapper;
using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace FluxoDeCaixa.Infrastructure.Dapper
{
    public class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
    {
        // Executado ao ENVIAR o parâmetro para o Banco de Dados
        public override void SetValue([DisallowNull] IDbDataParameter parameter, DateOnly value)
        {
            parameter.Value = value.ToDateTime(TimeOnly.MinValue);
            parameter.DbType = DbType.Date;
        }

        // Executado ao LER o valor do Banco de Dados para o C#
        public override DateOnly Parse(object value)
        {
            return value is DateOnly dateOnly
                ? dateOnly
                : DateOnly.FromDateTime((DateTime)value);
        }
    }
}