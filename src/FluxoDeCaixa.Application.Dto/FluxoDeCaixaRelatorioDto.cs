
namespace FluxoDeCaixa.Application.Dto
{
    public class FluxoDeCaixaRelatorioDto
    {
        public DateOnly DataFC { get; set; }
        public DateTime CriadoEm { get; set; }
        public decimal Credito { get; set; }
        public decimal Debito { get; set; }
    }
}



