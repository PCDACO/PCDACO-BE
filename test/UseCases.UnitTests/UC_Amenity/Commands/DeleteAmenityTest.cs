using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared;
using Infrastructure.Encryption;
using Microsoft.EntityFrameworkCore;
using Persistance.Data;
using Testcontainers.PostgreSql;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.UC_Amenity.Commands;
using UUIDNext;

namespace UseCases.UnitTests.UC_Amenity.Commands;

public class DeleteAmenityTests : IAsyncLifetime
{
    private AppDBContext _dbContext;
    private readonly CurrentUser _currentUser;
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly EncryptionSettings _encryptionSettings;
    private readonly IKeyManagementService _keyService;

    public DeleteAmenityTests()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgis/postgis:latest")
            .WithCleanUp(true)
            .Build();

        _currentUser = new CurrentUser();
        _encryptionSettings = new EncryptionSettings
        {
            Key = "dnjGHqR9O/2hKCQUgImXcEjZ9YPaAVcfz4l5VcTBLcY="
        };
        _keyService = new KeyManagementService();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        var options = new DbContextOptionsBuilder<AppDBContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString(), o => o.UseNetTopologySuite())
            .EnableSensitiveDataLogging()
            .Options;

        _dbContext = new AppDBContext(options);
        await _dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    private async Task<User> CreateTestUser(UserRole role)
    {
        var (key, iv) = await _keyService.GenerateKeyAsync();
        var encryptedKey = _keyService.EncryptKey(key, _encryptionSettings.Key);

        var encryptionKey = new EncryptionKey
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            EncryptedKey = encryptedKey,
            IV = iv
        };

        _dbContext.EncryptionKeys.Add(encryptionKey);
        await _dbContext.SaveChangesAsync();

        var user = new User
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            EncryptionKeyId = encryptionKey.Id,
            Name = "Test User",
            Email = "test@example.com",
            Password = "password",
            Role = role,
            Address = "Test Address",
            DateOfBirth = DateTime.UtcNow.AddYears(-30),
            Phone = "1234567890"
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        return user;
    }

    private async Task<Amenity> CreateTestAmenity(bool isDeleted = false)
    {
        var amenity = new Amenity
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            Name = "WiFi",
            Description = "High-speed internet",
            IsDeleted = isDeleted
        };

        _dbContext.Amenities.Add(amenity);
        await _dbContext.SaveChangesAsync();

        return amenity;
    }

    [Fact]
    public async Task Handle_UserNotAdmin_ReturnsForbidden()
    {
        // Arrange
        var testUser = await CreateTestUser(UserRole.Driver);
        _currentUser.SetUser(testUser);

        var handler = new DeleteAmenity.Handler(_dbContext, _currentUser);
        var command = new DeleteAmenity.Command(Uuid.NewDatabaseFriendly(Database.PostgreSql));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains("Bạn không có quyền xóa tiện nghi", result.Errors);
    }

    [Fact]
    public async Task Handle_AmenityNotFound_ReturnsError()
    {
        // Arrange
        var testUser = await CreateTestUser(UserRole.Admin);
        _currentUser.SetUser(testUser);

        var handler = new DeleteAmenity.Handler(_dbContext, _currentUser);
        var command = new DeleteAmenity.Command(Uuid.NewDatabaseFriendly(Database.PostgreSql));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Không tìm thấy tiện nghi", result.Errors);
    }

    [Fact]
    public async Task Handle_ValidRequest_DeletesAmenitySuccessfully()
    {
        // Arrange
        var testUser = await CreateTestUser(UserRole.Admin);
        _currentUser.SetUser(testUser);
        var testAmenity = await CreateTestAmenity();

        var handler = new DeleteAmenity.Handler(_dbContext, _currentUser);
        var command = new DeleteAmenity.Command(testAmenity.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Contains("Xóa tiện nghi thành công", result.SuccessMessage);

        // Verify soft delete
        var deletedAmenity = await _dbContext
            .Amenities.IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.Id == testAmenity.Id);
        Assert.True(deletedAmenity?.IsDeleted);
    }

    [Fact]
    public async Task Handle_AmenityAlreadyDeleted_ReturnsError()
    {
        // Arrange
        var testUser = await CreateTestUser(UserRole.Admin);
        _currentUser.SetUser(testUser);
        var testAmenity = await CreateTestAmenity(isDeleted: true);

        var handler = new DeleteAmenity.Handler(_dbContext, _currentUser);
        var command = new DeleteAmenity.Command(testAmenity.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Không tìm thấy tiện nghi", result.Errors);
    }
}
