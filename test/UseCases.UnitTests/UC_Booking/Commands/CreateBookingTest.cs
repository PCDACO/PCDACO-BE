using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Persistance.Data;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.UC_Booking.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;
using UUIDNext;

namespace UseCases.UnitTests.UC_Booking.Commands;

[Collection("Test Collection")]
public class CreateBookingTests(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly TestDataEmailService _emailService = new();
    private readonly IBackgroundJobClient _backgroundJobClient = new BackgroundJobClient();
    private readonly IAesEncryptionService _aesService = fixture.AesEncryptionService;
    private readonly IKeyManagementService _keyService = fixture.KeyManagementService;
    private readonly EncryptionSettings _encryptionSettings = fixture.EncryptionSettings;
    private readonly CurrentUser _currentUser = fixture.CurrentUser;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_UserNotDriver_ReturnsError()
    {
        // Arrange
        UserRole adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");

        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(testUser);

        var handler = new CreateBooking.Handler(
            _dbContext,
            _emailService,
            _backgroundJobClient,
            _currentUser
        );
        var command = new CreateBooking.CreateBookingCommand(
            CarId: Uuid.NewDatabaseFriendly(Database.PostgreSql),
            StartTime: DateTime.UtcNow,
            EndTime: DateTime.UtcNow.AddDays(1)
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains("Bạn không có quyền thực hiện chức năng này !", result.Errors);
    }

    [Fact]
    public async Task Handle_CarNotFound_ReturnsNotFound()
    {
        // Arrange
        UserRole driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");

        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(testUser);

        // Create an already processed license
        await TestDataCreateLicense.CreateTestLicense(
            _dbContext,
            testUser.Id,
            _aesService,
            _keyService,
            _encryptionSettings,
            isApproved: true
        );

        var handler = new CreateBooking.Handler(
            _dbContext,
            _emailService,
            _backgroundJobClient,
            _currentUser
        );
        var command = new CreateBooking.CreateBookingCommand(
            CarId: Uuid.NewDatabaseFriendly(Database.PostgreSql),
            StartTime: DateTime.UtcNow,
            EndTime: DateTime.UtcNow.AddDays(1)
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesBookingWithCorrectValues()
    {
        // Arrange
        UserRole driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        await TestDataBookingStatus.CreateTestBookingStatuses(_dbContext);
        TransmissionType transmissionType =
            await TestDataTransmissionType.CreateTestTransmissionType(_dbContext, "Automatic");
        FuelType fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");
        CarStatus carStatus = await TestDataCarStatus.CreateTestCarStatus(_dbContext, "Available");

        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        var testManufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var testModel = await TestDataCreateModel.CreateTestModel(_dbContext, testManufacturer.Id);
        var testCar = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: testUser.Id,
            modelId: testModel.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: carStatus
        );
        var startTime = DateTime.UtcNow;
        var endTime = DateTime.UtcNow.AddDays(3);

        // Create an already processed license
        await TestDataCreateLicense.CreateTestLicense(
            _dbContext,
            testUser.Id,
            _aesService,
            _keyService,
            _encryptionSettings,
            isApproved: true
        );

        _currentUser.SetUser(testUser);

        var handler = new CreateBooking.Handler(
            _dbContext,
            _emailService,
            _backgroundJobClient,
            _currentUser
        );
        var command = new CreateBooking.CreateBookingCommand(
            CarId: testCar.Id,
            StartTime: startTime,
            EndTime: endTime
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        var createdBooking = await _dbContext.Bookings.FirstOrDefaultAsync(b =>
            b.Id == result.Value.Id
        );

        Assert.NotNull(createdBooking);
        Assert.Equal(testUser.Id, createdBooking.UserId);
        Assert.Equal(testCar.Id, createdBooking.CarId);
    }

    [Fact]
    public void Validator_EndTimeBeforeStartTime_ReturnsValidationError()
    {
        // Arrange
        var validator = new CreateBooking.Validator();
        var command = new CreateBooking.CreateBookingCommand(
            Uuid.NewDatabaseFriendly(Database.PostgreSql),
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(-1)
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.ErrorMessage == "Thời gian kết thúc thuê phải sau thời gian bắt đầu thuê"
        );
    }

    [Fact]
    public async Task Handle_SameUserOverlappingBooking_ReturnsConflict()
    {
        // Arrange
        UserRole driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");

        TransmissionType transmissionType =
            await TestDataTransmissionType.CreateTestTransmissionType(_dbContext, "Automatic");
        FuelType fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");
        CarStatus carStatus = await TestDataCarStatus.CreateTestCarStatus(_dbContext, "Available");
        await TestDataBookingStatus.CreateTestBookingStatuses(_dbContext);

        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        var testManufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var testModel = await TestDataCreateModel.CreateTestModel(_dbContext, testManufacturer.Id);
        var testCar = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: testUser.Id,
            modelId: testModel.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: carStatus
        );
        _currentUser.SetUser(testUser);

        await TestDataCreateLicense.CreateTestLicense(
            _dbContext,
            testUser.Id,
            _aesService,
            _keyService,
            _encryptionSettings,
            isApproved: true
        );

        // Create overlapping booking
        var command1 = new CreateBooking.CreateBookingCommand(
            CarId: testCar.Id,
            StartTime: DateTime.UtcNow.AddHours(1),
            EndTime: DateTime.UtcNow.AddHours(3)
        );

        // Overlapping booking
        var command2 = new CreateBooking.CreateBookingCommand(
            CarId: testCar.Id,
            StartTime: DateTime.UtcNow.AddHours(2), // Overlaps
            EndTime: DateTime.UtcNow.AddHours(4)
        );

        var handler = new CreateBooking.Handler(
            _dbContext,
            _emailService,
            _backgroundJobClient,
            _currentUser
        );

        // Act
        var result1 = await handler.Handle(command1, CancellationToken.None);
        var result2 = await handler.Handle(command2, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Conflict, result2.Status);
    }

    [Fact]
    public async Task Handle_DifferentUserOverlappingBooking_CreatesSuccessfully()
    {
        // Arrange
        UserRole driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");

        TransmissionType transmissionType =
            await TestDataTransmissionType.CreateTestTransmissionType(_dbContext, "Automatic");
        FuelType fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");
        CarStatus carStatus = await TestDataCarStatus.CreateTestCarStatus(_dbContext, "Available");
        await TestDataBookingStatus.CreateTestBookingStatuses(_dbContext);

        var testUser1 = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            driverRole,
            "driver1@test.com"
        );
        var testUser2 = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            driverRole,
            "driver2@test.com"
        );

        var testManufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var testModel = await TestDataCreateModel.CreateTestModel(_dbContext, testManufacturer.Id);
        var testCar = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: testUser1.Id,
            modelId: testModel.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: carStatus
        );

        await TestDataCreateLicense.CreateTestLicense(
            _dbContext,
            testUser1.Id,
            _aesService,
            _keyService,
            _encryptionSettings,
            isApproved: true
        );

        await TestDataCreateLicense.CreateTestLicense(
            _dbContext,
            testUser2.Id,
            _aesService,
            _keyService,
            _encryptionSettings,
            isApproved: true
        );

        // First booking with user1
        _currentUser.SetUser(testUser1);
        var handler1 = new CreateBooking.Handler(
            _dbContext,
            _emailService,
            _backgroundJobClient,
            _currentUser
        );
        var command1 = new CreateBooking.CreateBookingCommand(
            CarId: testCar.Id,
            StartTime: DateTime.UtcNow.AddHours(1),
            EndTime: DateTime.UtcNow.AddHours(3)
        );

        // Act for User 1
        var result1 = await handler1.Handle(command1, CancellationToken.None);

        // Second booking with user2
        _currentUser.SetUser(testUser2);
        var handler2 = new CreateBooking.Handler(
            _dbContext,
            _emailService,
            _backgroundJobClient,
            _currentUser
        );
        var command2 = new CreateBooking.CreateBookingCommand(
            CarId: testCar.Id,
            StartTime: DateTime.UtcNow.AddHours(2), // Overlaps
            EndTime: DateTime.UtcNow.AddHours(4)
        );

        // Act for User 2
        var result2 = await handler2.Handle(command2, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result2.Status);

        // Verify both bookings exist
        var bookings = await _dbContext
            .Bookings.Include(b => b.Status)
            .Where(b => b.CarId == testCar.Id)
            .ToListAsync();

        Assert.Equal(2, bookings.Count);
        Assert.Contains(bookings, b => b.UserId == testUser1.Id);
        Assert.Contains(bookings, b => b.UserId == testUser2.Id);
        Assert.All(
            bookings,
            b => Assert.Equal(BookingStatusEnum.Pending.ToString(), b.Status.Name)
        );
    }

    [Fact]
    public async Task Handle_UserWithoutValidLicense_ReturnsForbidden()
    {
        // Arrange
        UserRole driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(testUser);

        // Ensure the user has no license
        var handler = new CreateBooking.Handler(
            _dbContext,
            _emailService,
            _backgroundJobClient,
            _currentUser
        );
        var command = new CreateBooking.CreateBookingCommand(
            CarId: Uuid.NewDatabaseFriendly(Database.PostgreSql),
            StartTime: DateTime.UtcNow,
            EndTime: DateTime.UtcNow.AddDays(1)
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains(
            "Bạn chưa xác thực bằng lái xe hoặc bằng lái xe chưa được phê duyệt!",
            result.Errors
        );
    }
}
