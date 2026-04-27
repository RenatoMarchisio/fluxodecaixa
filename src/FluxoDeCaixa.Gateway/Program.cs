/** API Gateway para o Fluxo de Caixa
 * 
 * Responsável por rotear as requisições para os microsserviços correspondentes
 * e agregar as respostas quando necessário.
 * 
 * Utiliza o Ocelot para gerenciamento de rotas e balanceamento de carga.
 */


using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Adicionar serviços do YARP
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddSwaggerGen( c => 
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "API Fluxo de Caixa",
        Version = "v1",
        Description = "API Gateway para o sistema de Fluxo de Caixa, responsável por rotear as requisições para os microsserviços correspondentes e agregar as respostas quando necessário."
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Gateway V1");
        c.SwaggerEndpoint("/fluxodecaixa/swagger/v1/swagger.json", "API Fluxo de Caixa V1");
        c.SwaggerEndpoint("/relatorio/swagger/v1/swagger.json", "API Relatorio V1");

        c.RoutePrefix = "swagger";
    });

}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.UseEndpoints(endpoints => { 
    endpoints.MapControllers();
    endpoints.MapReverseProxy();
});

app.Run();
