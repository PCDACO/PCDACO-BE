using System.Transactions;
using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using Persistance.Data;
using Testcontainers.PostgreSql;
using UseCases.DTOs;

namespace UseCases.UnitTests.TestBases;

public abstract class DatabaseTestBase : IAsyncLifetime
{
    protected AppDBContext _dbContext { get; private set; }
    protected CurrentUser _currentUser { get; private set; }
    private readonly PostgreSqlContainer _postgresContainer;

    // private TransactionScope _transactionScope;

    protected DatabaseTestBase()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgis/postgis:13-3.1")
            // .WithReuse(true) // Enable container reuse
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithPortBinding(5432, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .WithCleanUp(true) // Clean up the container after the test
            .Build();

        _currentUser = new CurrentUser();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync().ConfigureAwait(false);

        var options = new DbContextOptionsBuilder<AppDBContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString(), o => o.UseNetTopologySuite())
            .EnableSensitiveDataLogging()
            .Options;

        _dbContext = new AppDBContext(options);
        await _dbContext.Database.MigrateAsync();

        // Rollback transaction after each test
        // _transactionScope = new TransactionScope(
        //     TransactionScopeOption.RequiresNew,
        //     new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
        //     TransactionScopeAsyncFlowOption.Enabled
        // );
    }

    public async Task DisposeAsync()
    {
        // _transactionScope?.Dispose(); // Rollback transaction
        await _dbContext.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }
}
