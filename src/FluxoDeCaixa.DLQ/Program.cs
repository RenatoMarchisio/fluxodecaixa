using Dapper;
using FluxoDeCaixa.DLQ.Messaging;
using FluxoDeCaixa.Infrastructure.Dapper;
using FluxoDeCaixa.Infrastructure.Messaging;
using FluxoDeCaixa.Persistence;
using FluxoDeCaixa.Application.UseCases;

// Utilizado para mapear DateOnly para SqlServer DATE Type
SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Persistence + Application (CQRS / AutoMapper)
builder.Services.AddInjectionPersistence();
builder.Services.AddInjectionApplication();

// MediatR — registra todos os handlers do assembly
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());

// RabbitMQ — configurações e publisher como Singleton
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.AddHostedService<FluxoDeCaixaDlqConsumer>();

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FluxoDeCaixa DLQ Consumer V.1");
        c.RoutePrefix = string.Empty;
    });
//}

app.UseCors();
app.Run();

