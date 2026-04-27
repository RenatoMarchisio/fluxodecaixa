using FluxoDeCaixa.Application.UseCases.Commons.Bases;
using FluxoDeCaixa.Application.UseCases.Commons.Exceptions;
using System.Text.Json;

namespace FluxoDeCaixa.WebApi.Extensions.Middleware
{
    public class ValidationMiddleware
    {
        private readonly RequestDelegate _next;

        public ValidationMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next.Invoke(context);
            }
            catch (ValidationExceptionCustom ex)
            {
                context.Response.ContentType = "application/json";
                await JsonSerializer.SerializeAsync(context.Response.Body, new BaseResponse<object> { Message = "Erros de validacao", Errors = ex.Errors });
            }
        }
    }
}
