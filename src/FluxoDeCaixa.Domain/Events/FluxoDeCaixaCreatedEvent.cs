
using FluxoDeCaixa.Domain.Commons;

namespace FluxoDeCaixa.Domain.Events
{
    public abstract class FluxoDeCaixaCreatedBaseEvent : BaseEvent
    {
        public Guid ID { get; set; }
        public DateOnly dataFC { get; set; }
        public string descricao { get; set; }
//        public DateTime criadoEm { get; set; }
    }


    public abstract class FluxoDeCaixaCreatedCreditoEvent : BaseEvent
    {
        public decimal credito { get; set; }
    }

    public abstract class FluxoDeCaixaCreatedDebitoEvent : BaseEvent
    {
        public decimal debito { get; set; }
    }
}
