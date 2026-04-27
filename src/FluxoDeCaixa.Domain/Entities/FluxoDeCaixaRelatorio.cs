
namespace FluxoDeCaixa.Domain.Entities
{
    public class FluxoDeCaixaRelatorio
    {
        public DateOnly dataFC { get; set; }
        public decimal credito { get; set; }
        public decimal debito { get; set; }
        public DateTime criadoEm { get; set; }
    }

}

