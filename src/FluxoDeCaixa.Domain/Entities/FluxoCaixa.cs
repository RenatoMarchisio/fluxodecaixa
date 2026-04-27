
namespace FluxoDeCaixa.Domain.Entities
{
    public abstract class FluxoDeCaixaBase 
    {
        public required Guid ID { get; set; }
        public DateOnly dataFC { get; set; }
        public string descricao { get; set; } 
//        public DateTime criadoEm { get; set; }

    }

    public class FluxoDeCaixaCredito : FluxoDeCaixaBase
    {
        public decimal credito { get; set; }
    }

    public class FluxoDeCaixaDebito : FluxoDeCaixaBase
    {
        public decimal debito { get; set; }
    }
}

