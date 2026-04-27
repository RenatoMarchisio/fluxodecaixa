namespace FluxoDeCaixa.Tests.Infrastructure.Fixtures
{
    [CollectionDefinition(Name)]
    public sealed class RabbitMqCollection : ICollectionFixture<RabbitMqFixture>
    {
        public const string Name = "RabbitMQ";
    }
}
