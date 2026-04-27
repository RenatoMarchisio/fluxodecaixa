
namespace FluxoDeCaixa.Application.Dto
{
    public class FluxoDeCaixaBaseDto
    {
        public required Guid ID { get; set; }
        public DateOnly DataFC { get; set; }
        public string Descricao { get; set; }
        public DateTime CriadoEm { get; set; }
    }
    public class FluxoDeCaixaCreditoDTO : FluxoDeCaixaBaseDto
    {
        public decimal Credito { get; set; }
    }
    public class FluxoDeCaixaDebitoDTO : FluxoDeCaixaBaseDto
    {
        public decimal Debito { get; set; }
    }
}
