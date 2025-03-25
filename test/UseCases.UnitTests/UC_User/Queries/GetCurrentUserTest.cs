using Ardalis.Result;
using Domain.Constants;
using Domain.Constants.EntityNames;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared;
using Infrastructure.Encryption;
using Microsoft.EntityFrameworkCore;
using Persistance.Data;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.UC_User.Queries;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_User.Queries;

[Collection("Test Collection")]
public class GetCurrentUserTest(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly CurrentUser _currentUser = fixture.CurrentUser;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;
    private readonly IAesEncryptionService _aesService = fixture.AesEncryptionService;
    private readonly IKeyManagementService _keyService = fixture.KeyManagementService;
    private readonly EncryptionSettings _encryptionSettings = fixture.EncryptionSettings;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_UserNotLoggedIn_ReturnsUnauthorized()
    {
        // Arrange
        _currentUser.SetUser(null!);
        var handler = new GetCurrentUser.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );
        var query = new GetCurrentUser.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Unauthorized, result.Status);
        Assert.Contains("Bạn chưa đăng nhập", result.Errors);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        // Create a user but don't add them to the database
        var role = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Test User",
            Email = "test@example.com",
            Password = "password",
            Address = "Test Address",
            DateOfBirth = DateTimeOffset.UtcNow.AddYears(-20),
            Phone = "1234567890",
            RoleId = role.Id,
            EncryptionKeyId = Guid.NewGuid(),
        };

        _currentUser.SetUser(user);

        var handler = new GetCurrentUser.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );
        var query = new GetCurrentUser.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains("Không tìm thấy thông tin người dùng", result.Errors);
    }

    [Fact]
    public async Task Handle_DriverRole_ReturnsCorrectInfo()
    {
        // Arrange
        // Create driver role
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");

        // Create encryption keys
        (string key, string iv) = await _keyService.GenerateKeyAsync();
        string encryptedKey = _keyService.EncryptKey(key, _encryptionSettings.Key);

        // Create phone number
        string phoneNumber = "0987654321";
        string encryptedPhone = await _aesService.Encrypt(phoneNumber, key, iv);

        // Create encryption key record
        var encryptionKey = new EncryptionKey { EncryptedKey = encryptedKey, IV = iv };
        await _dbContext.EncryptionKeys.AddAsync(encryptionKey);
        await _dbContext.SaveChangesAsync();

        // Create user with driver role
        var user = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            driverRole,
            "driver@test.com",
            "Test Driver",
            encryptedPhone,
            "avatar.jpg"
        );

        // Update user's encryption key ID
        user.EncryptionKeyId = encryptionKey.Id;
        await _dbContext.SaveChangesAsync();

        // Create car owner
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            ownerRole,
            "owner@test.com"
        );

        // Create car
        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmissionType = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        var car = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: owner.Id,
            modelId: model.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: CarStatusEnum.Available
        );

        // Create bookings for the driver
        await TestDataCreateBooking.CreateTestBooking(
            _dbContext,
            user.Id,
            car.Id,
            BookingStatusEnum.Completed
        );
        await TestDataCreateBooking.CreateTestBooking(
            _dbContext,
            user.Id,
            car.Id,
            BookingStatusEnum.Completed
        );
        await TestDataCreateBooking.CreateTestBooking(
            _dbContext,
            user.Id,
            car.Id,
            BookingStatusEnum.Pending
        );

        _currentUser.SetUser(user);

        var handler = new GetCurrentUser.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );
        var query = new GetCurrentUser.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Contains("Lấy thông tin người dùng thành công", result.SuccessMessage);

        var response = result.Value;
        Assert.Equal(user.Id, response.Id);
        Assert.Equal(user.Name, response.Name);
        Assert.Equal(user.Email, response.Email);
        Assert.Equal(user.AvatarUrl, response.AvatarUrl);
        Assert.Equal(user.Address, response.Address);
        Assert.Equal(user.DateOfBirth.Date, response.DateOfBirth.Date);
        Assert.Equal(phoneNumber, response.Phone); // Phone number should be decrypted
        Assert.Equal(UserRoleNames.Driver, response.Role);
        Assert.Equal(2, response.TotalRent); // 2 completed bookings
        Assert.Equal(0, response.TotalRented); // Driver has no rented cars
        Assert.Equal(0, response.TotalCar); // Driver has no cars
    }

    [Fact]
    public async Task Handle_OwnerRole_ReturnsCorrectInfo()
    {
        // Arrange
        // Create owner role
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");

        // Create encryption keys
        (string key, string iv) = await _keyService.GenerateKeyAsync();
        string encryptedKey = _keyService.EncryptKey(key, _encryptionSettings.Key);

        // Create phone number
        string phoneNumber = "0987654321";
        string encryptedPhone = await _aesService.Encrypt(phoneNumber, key, iv);

        // Create encryption key record
        var encryptionKey = new EncryptionKey { EncryptedKey = encryptedKey, IV = iv };
        await _dbContext.EncryptionKeys.AddAsync(encryptionKey);
        await _dbContext.SaveChangesAsync();

        // Create user with owner role
        var user = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            ownerRole,
            "owner@test.com",
            "Test Owner",
            encryptedPhone,
            "avatar.jpg"
        );

        // Update user's encryption key ID
        user.EncryptionKeyId = encryptionKey.Id;
        await _dbContext.SaveChangesAsync();

        // Create driver
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var driver = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            driverRole,
            "driver@test.com"
        );

        // Create cars for the owner
        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmissionType = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        var car1 = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: user.Id,
            modelId: model.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: CarStatusEnum.Available
        );

        var car2 = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: user.Id,
            modelId: model.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: CarStatusEnum.Available
        );

        // Create completed bookings for the cars
        await TestDataCreateBooking.CreateTestBooking(
            _dbContext,
            driver.Id,
            car1.Id,
            BookingStatusEnum.Completed
        );
        await TestDataCreateBooking.CreateTestBooking(
            _dbContext,
            driver.Id,
            car1.Id,
            BookingStatusEnum.Completed
        );
        await TestDataCreateBooking.CreateTestBooking(
            _dbContext,
            driver.Id,
            car2.Id,
            BookingStatusEnum.Completed
        );
        await TestDataCreateBooking.CreateTestBooking(
            _dbContext,
            driver.Id,
            car1.Id,
            BookingStatusEnum.Pending
        );

        _currentUser.SetUser(user);

        var handler = new GetCurrentUser.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );
        var query = new GetCurrentUser.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Contains("Lấy thông tin người dùng thành công", result.SuccessMessage);

        var response = result.Value;
        Assert.Equal(user.Id, response.Id);
        Assert.Equal(user.Name, response.Name);
        Assert.Equal(user.Email, response.Email);
        Assert.Equal(user.AvatarUrl, response.AvatarUrl);
        Assert.Equal(user.Address, response.Address);
        Assert.Equal(user.DateOfBirth.Date, response.DateOfBirth.Date);
        Assert.Equal(phoneNumber, response.Phone); // Phone number should be decrypted
        Assert.Equal(UserRoleNames.Owner, response.Role);
        Assert.Equal(0, response.TotalRent); // Owner has no rented cars as a driver
        Assert.Equal(3, response.TotalRented); // 3 completed bookings for owner's cars
        Assert.Equal(2, response.TotalCar); // Owner has 2 cars
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Consultant")]
    [InlineData("Technician")]
    public async Task Handle_OtherRoles_ReturnsCorrectInfo(string roleName)
    {
        // Arrange
        // Create role
        var role = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, roleName);

        // Create encryption keys
        (string key, string iv) = await _keyService.GenerateKeyAsync();
        string encryptedKey = _keyService.EncryptKey(key, _encryptionSettings.Key);

        // Create phone number
        string phoneNumber = "0987654321";
        string encryptedPhone = await _aesService.Encrypt(phoneNumber, key, iv);

        // Create encryption key record
        var encryptionKey = new EncryptionKey { EncryptedKey = encryptedKey, IV = iv };
        await _dbContext.EncryptionKeys.AddAsync(encryptionKey);
        await _dbContext.SaveChangesAsync();

        // Create user with specified role
        var user = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            role,
            $"{roleName.ToLower()}@test.com",
            $"Test {roleName}",
            encryptedPhone,
            "avatar.jpg"
        );

        // Update user's encryption key ID
        user.EncryptionKeyId = encryptionKey.Id;
        await _dbContext.SaveChangesAsync();

        _currentUser.SetUser(user);

        var handler = new GetCurrentUser.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );
        var query = new GetCurrentUser.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Contains("Lấy thông tin người dùng thành công", result.SuccessMessage);

        var response = result.Value;
        Assert.Equal(user.Id, response.Id);
        Assert.Equal(user.Name, response.Name);
        Assert.Equal(user.Email, response.Email);
        Assert.Equal(user.AvatarUrl, response.AvatarUrl);
        Assert.Equal(user.Address, response.Address);
        Assert.Equal(user.DateOfBirth.Date, response.DateOfBirth.Date);
        Assert.Equal(phoneNumber, response.Phone); // Phone number should be decrypted
        Assert.Equal(roleName, response.Role);
        Assert.Equal(0, response.TotalRent); // Not a driver, so 0
        Assert.Equal(0, response.TotalRented); // Not an owner, so 0
        Assert.Equal(0, response.TotalCar); // Not an owner, so 0
    }

    [Fact]
    public async Task Handle_UserWithBalance_ReturnsCorrectBalance()
    {
        // Arrange
        var role = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");

        // Create encryption keys
        (string key, string iv) = await _keyService.GenerateKeyAsync();
        string encryptedKey = _keyService.EncryptKey(key, _encryptionSettings.Key);

        // Create phone number
        string phoneNumber = "0987654321";
        string encryptedPhone = await _aesService.Encrypt(phoneNumber, key, iv);

        // Create encryption key record
        var encryptionKey = new EncryptionKey { EncryptedKey = encryptedKey, IV = iv };
        await _dbContext.EncryptionKeys.AddAsync(encryptionKey);
        await _dbContext.SaveChangesAsync();

        // Create user with specified role
        var user = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            role,
            $"Driver@test.com",
            $"Test Driver",
            encryptedPhone,
            "avatar.jpg"
        );
        user.EncryptionKeyId = encryptionKey.Id;
        await _dbContext.SaveChangesAsync();

        // Set a balance
        user.Balance = 1500.50m;
        await _dbContext.SaveChangesAsync();

        _currentUser.SetUser(user);

        var handler = new GetCurrentUser.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );
        var query = new GetCurrentUser.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        var response = result.Value;
        Assert.Equal(1500.50m, response.Balance);
    }
}
