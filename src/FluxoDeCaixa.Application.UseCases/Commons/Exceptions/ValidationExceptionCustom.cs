using FluxoDeCaixa.Application.UseCases.Commons.Bases;


namespace FluxoDeCaixa.Application.UseCases.Commons.Exceptions
{
    public class ValidationExceptionCustom : Exception
    {
        public IEnumerable<BaseError> Errors { get; }

        public ValidationExceptionCustom()
            : base("Uma o mais falhas de validações ocorreu.")
        {
            Errors = new List<BaseError>();
        }

        public ValidationExceptionCustom(IEnumerable<BaseError> errors)
            : this()
        {
            Errors = errors;
        }
    }
}
