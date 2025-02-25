using System.Data.Common;
using Domain.Shared;
using DotNet.Testcontainers.Builders;
using Hangfire;
using Hangfire.MemoryStorage;
using Infrastructure.Encryption;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Persistance.Data;
using Respawn;
using Testcontainers.PostgreSql;
using UseCases.DTOs;

namespace UseCases.UnitTests.TestBases;

public class DatabaseTestBase : IAsyncLifetime
{
    public AppDBContext DbContext { get; private set; } = null!;
    public CurrentUser CurrentUser { get; private set; } = null!;
    public AesEncryptionService AesEncryptionService { get; private set; } = null!;
    public KeyManagementService KeyManagementService { get; private set; } = null!;
    public EncryptionSettings EncryptionSettings { get; private set; } = null!;
    private readonly PostgreSqlContainer _postgresContainer;
    private DbConnection _dbConnection = default!;
    private Respawner _respawner = default!;

    public DatabaseTestBase()
    {
        CurrentUser = new CurrentUser();
        AesEncryptionService = new AesEncryptionService();
        KeyManagementService = new KeyManagementService();
        EncryptionSettings = new EncryptionSettings { Key = TestConstants.MasterKey };

        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgis/postgis:13-3.1")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithPortBinding(5432, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .WithCleanUp(true)
            .Build();

        // Configure Hangfire to use in-memory storage
        GlobalConfiguration.Configuration.UseMemoryStorage();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync().ConfigureAwait(false);

        var options = new DbContextOptionsBuilder<AppDBContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString(), o => o.UseNetTopologySuite())
            .EnableSensitiveDataLogging()
            .Options;

        DbContext = new AppDBContext(options);
        await DbContext.Database.MigrateAsync();

        await InitializeRespawner();
    }

    private async Task InitializeRespawner()
    {
        // Initialize connection for Respawner
        _dbConnection = DbContext.Database.GetDbConnection();

        // Check if connection is already open
        if (_dbConnection.State != System.Data.ConnectionState.Open)
        {
            await _dbConnection.OpenAsync();
        }

        // Initialize Respawner
        _respawner = await Respawner.CreateAsync(
            _dbConnection,
            new RespawnerOptions { DbAdapter = DbAdapter.Postgres, SchemasToInclude = ["public"] }
        );
    }

    public async Task ResetDatabaseAsync()
    {
        await _respawner.ResetAsync(_dbConnection);
    }

    public async Task DisposeAsync()
    {
        if (_dbConnection != null)
        {
            await _dbConnection.DisposeAsync();
        }

        await DbContext.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }
}
