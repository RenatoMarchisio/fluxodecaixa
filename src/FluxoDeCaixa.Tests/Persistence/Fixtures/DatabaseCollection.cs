namespace FluxoDeCaixa.Tests.Persistence.Fixtures
{
    /// <summary>
    /// Coleção xUnit que compartilha o container SQL Server entre todas
    /// as classes de testes de repositório (um único startup/teardown).
    /// </summary>
    [CollectionDefinition(Name)]
    public sealed class DatabaseCollection : ICollectionFixture<SqlServerFixture>
    {
        public const string Name = "fluxocaixa";
    }
}
