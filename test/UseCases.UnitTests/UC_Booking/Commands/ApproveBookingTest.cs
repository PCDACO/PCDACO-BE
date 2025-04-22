using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Persistance.Data;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Services.PaymentTokenService;
using UseCases.UC_Booking.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_Booking.Commands;

[Collection("Test Collection")]
public class ApproveBookingTests(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly TestDataEmailService _emailService = new();
    private readonly IBackgroundJobClient _backgroundJobClient = new BackgroundJobClient();
    private readonly IAesEncryptionService _aesService = fixture.AesEncryptionService;
    private readonly IKeyManagementService _keyService = fixture.KeyManagementService;
    private readonly EncryptionSettings _encryptionSettings = fixture.EncryptionSettings;
    private readonly ContractSettings _contractSettings = new();
    private readonly CurrentUser _currentUser = fixture.CurrentUser;
    private const string TEST_SIGNATURE_BASE64 =
        "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";
    private readonly IPaymentTokenService _paymentTokenService =
        new Infrastructure.Services.PaymentTokenService(new MemoryCache(new MemoryCacheOptions()));
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;
    private const string TEST_BASE_URL = "http://localhost:8080";

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_UserNotOwner_ReturnsError()
    {
        // Arrange
        UserRole driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(testUser);

        var handler = new ApproveBooking.Handler(
            _dbContext,
            _emailService,
            _backgroundJobClient,
            _currentUser,
            _paymentTokenService,
            _aesService,
            _keyService,
            _encryptionSettings,
            _contractSettings
        );
        var command = new ApproveBooking.Command(
            Guid.NewGuid(),
            true,
            TEST_BASE_URL,
            TEST_SIGNATURE_BASE64
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains("Bạn không có quyền thực hiện chức năng này !", result.Errors);
    }

    [Fact]
    public async Task Handle_BookingNotFound_ReturnsNotFound()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        _currentUser.SetUser(testUser);

        var handler = new ApproveBooking.Handler(
            _dbContext,
            _emailService,
            _backgroundJobClient,
            _currentUser,
            _paymentTokenService,
            _aesService,
            _keyService,
            _encryptionSettings,
            _contractSettings
        );
        var command = new ApproveBooking.Command(
            Guid.NewGuid(),
            true,
            TEST_BASE_URL,
            TEST_SIGNATURE_BASE64
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains("Không tìm thấy booking", result.Errors);
    }

    [Theory]
    [InlineData(BookingStatusEnum.Approved)]
    [InlineData(BookingStatusEnum.Rejected)]
    [InlineData(BookingStatusEnum.Ongoing)]
    [InlineData(BookingStatusEnum.Completed)]
    [InlineData(BookingStatusEnum.Cancelled)]
    public async Task Handle_InvalidBookingStatus_ReturnsConflict(BookingStatusEnum status)
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        UserRole driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var driver = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            driverRole,
            "driver@test.com"
        );
        _currentUser.SetUser(owner);

        // Setup car and booking
        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmissionType = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        var car = await TestDataCreateCar.CreateTestCar(
            _dbContext,
            owner.Id,
            model.Id,
            transmissionType,
            fuelType,
            CarStatusEnum.Available
        );

        var booking = await TestDataCreateBooking.CreateTestBooking(
            _dbContext,
            driver.Id,
            car.Id,
            status
        );

        var handler = new ApproveBooking.Handler(
            _dbContext,
            _emailService,
            _backgroundJobClient,
            _currentUser,
            _paymentTokenService,
            _aesService,
            _keyService,
            _encryptionSettings,
            _contractSettings
        );
        var command = new ApproveBooking.Command(
            booking.Id,
            true,
            TEST_BASE_URL,
            TEST_SIGNATURE_BASE64
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Contains($"Không thể phê duyệt booking ở trạng thái {status}", result.Errors);
    }

    [Theory]
    [InlineData(true, "phê duyệt", BookingStatusEnum.Approved)]
    [InlineData(false, "từ chối", BookingStatusEnum.Rejected)]
    public async Task Handle_ValidRequest_UpdatesBookingStatus(
        bool isApproved,
        string expectedMessage,
        BookingStatusEnum expectedStatus
    )
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        UserRole driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");

        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var driver = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            driverRole,
            "driver@test.com"
        );
        _currentUser.SetUser(owner);

        // Setup car and booking
        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmissionType = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        var car = await TestDataCreateCar.CreateTestCar(
            _dbContext,
            owner.Id,
            model.Id,
            transmissionType,
            fuelType,
            CarStatusEnum.Available
        );

        var booking = await TestDataCreateBooking.CreateTestBooking(
            _dbContext,
            driver.Id,
            car.Id,
            BookingStatusEnum.Pending
        );

        var handler = new ApproveBooking.Handler(
            _dbContext,
            _emailService,
            _backgroundJobClient,
            _currentUser,
            _paymentTokenService,
            _aesService,
            _keyService,
            _encryptionSettings,
            _contractSettings
        );
        var command = new ApproveBooking.Command(
            booking.Id,
            isApproved,
            TEST_BASE_URL,
            TEST_SIGNATURE_BASE64
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Contains($"Đã {expectedMessage} booking thành công", result.SuccessMessage);

        var updatedBooking = await _dbContext.Bookings.FirstAsync(b => b.Id == booking.Id);

        Assert.Equal(expectedStatus, updatedBooking.Status);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_ValidRequest_SendsCorrectEmail(bool isApproved)
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        UserRole driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");

        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var driver = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            driverRole,
            "driver@test.com"
        );
        _currentUser.SetUser(owner);

        // Setup car and booking
        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmissionType = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        var car = await TestDataCreateCar.CreateTestCar(
            _dbContext,
            owner.Id,
            model.Id,
            transmissionType,
            fuelType,
            CarStatusEnum.Available
        );

        var booking = await TestDataCreateBooking.CreateTestBooking(
            _dbContext,
            driver.Id,
            car.Id,
            BookingStatusEnum.Pending
        );

        var handler = new ApproveBooking.Handler(
            _dbContext,
            _emailService,
            _backgroundJobClient,
            _currentUser,
            _paymentTokenService,
            _aesService,
            _keyService,
            _encryptionSettings,
            _contractSettings
        );
        var command = new ApproveBooking.Command(
            booking.Id,
            isApproved,
            TEST_BASE_URL,
            TEST_SIGNATURE_BASE64
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
    }

    [Fact]
    public async Task Handle_CarOwnershipCheck_ReturnsForbidden()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        UserRole driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");

        var owner1 = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var owner2 = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var driver = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            driverRole,
            "driver@test.com"
        );
        _currentUser.SetUser(owner1);

        // Setup car and booking
        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmissionType = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        var car = await TestDataCreateCar.CreateTestCar(
            _dbContext,
            owner1.Id,
            model.Id,
            transmissionType,
            fuelType,
            CarStatusEnum.Available
        );

        var booking = await TestDataCreateBooking.CreateTestBooking(
            _dbContext,
            driver.Id,
            car.Id,
            BookingStatusEnum.Pending
        );
        // Act

        _currentUser.SetUser(owner2);

        var handler = new ApproveBooking.Handler(
            _dbContext,
            _emailService,
            _backgroundJobClient,
            _currentUser,
            _paymentTokenService,
            _aesService,
            _keyService,
            _encryptionSettings,
            _contractSettings
        );
        var command = new ApproveBooking.Command(
            booking.Id,
            true,
            TEST_BASE_URL,
            TEST_SIGNATURE_BASE64
        );

        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains("Bạn không có quyền phê duyệt booking cho xe này!", result.Errors);
    }

    [Fact]
    public async Task Handle_ValidRequest_EnqueuesEmailJob()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        UserRole driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");

        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var driver = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            driverRole,
            "driver@test.com"
        );
        _currentUser.SetUser(owner);

        // Setup car and booking
        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmissionType = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        var car = await TestDataCreateCar.CreateTestCar(
            _dbContext,
            owner.Id,
            model.Id,
            transmissionType,
            fuelType,
            CarStatusEnum.Available
        );

        var booking = await TestDataCreateBooking.CreateTestBooking(
            _dbContext,
            driver.Id,
            car.Id,
            BookingStatusEnum.Pending
        );

        var handler = new ApproveBooking.Handler(
            _dbContext,
            _emailService,
            _backgroundJobClient,
            _currentUser,
            _paymentTokenService,
            _aesService,
            _keyService,
            _encryptionSettings,
            _contractSettings
        );
        var command = new ApproveBooking.Command(
            booking.Id,
            true,
            TEST_BASE_URL,
            TEST_SIGNATURE_BASE64
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
    }

    [Fact]
    public async Task Handle_ValidApproval_UpdatesContractWithSignature()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        UserRole driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");

        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var driver = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            driverRole,
            "driver@test.com"
        );
        _currentUser.SetUser(owner);

        // Setup car and booking
        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmissionType = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        var car = await TestDataCreateCar.CreateTestCar(
            _dbContext,
            owner.Id,
            model.Id,
            transmissionType,
            fuelType,
            CarStatusEnum.Available
        );

        var booking = await TestDataCreateBooking.CreateTestBooking(
            _dbContext,
            driver.Id,
            car.Id,
            BookingStatusEnum.Pending
        );

        // Create contract for the booking
        var contract = new Contract
        {
            BookingId = booking.Id,
            Status = ContractStatusEnum.Pending,
            Terms = "Standard terms",
            DriverSignature = "driver-signature",
            DriverSignatureDate = DateTimeOffset.UtcNow.AddDays(-1),
            StartDate = DateTimeOffset.UtcNow,
            EndDate = DateTimeOffset.UtcNow.AddDays(7),
        };
        await _dbContext.Contracts.AddAsync(contract);
        await _dbContext.SaveChangesAsync();

        var handler = new ApproveBooking.Handler(
            _dbContext,
            _emailService,
            _backgroundJobClient,
            _currentUser,
            _paymentTokenService,
            _aesService,
            _keyService,
            _encryptionSettings,
            _contractSettings
        );
        var command = new ApproveBooking.Command(
            booking.Id,
            true,
            TEST_BASE_URL,
            TEST_SIGNATURE_BASE64
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Contains("Đã phê duyệt booking thành công", result.SuccessMessage);

        // Verify contract was updated with signature
        var updatedContract = await _dbContext.Contracts.FirstAsync(c => c.BookingId == booking.Id);
        Assert.NotNull(updatedContract);
        Assert.Equal(ContractStatusEnum.Confirmed, updatedContract.Status);
        Assert.Equal(TEST_SIGNATURE_BASE64, updatedContract.OwnerSignature);
        Assert.NotNull(updatedContract.OwnerSignatureDate);

        // Verify original driver signature is still there
        Assert.Equal("driver-signature", updatedContract.DriverSignature);
        Assert.NotNull(updatedContract.DriverSignatureDate);
    }

    [Fact]
    public async Task Handle_ContractNotFound_OnlyUpdatesBookingStatus()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        UserRole driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");

        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var driver = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            driverRole,
            "driver@test.com"
        );
        _currentUser.SetUser(owner);

        // Setup car and booking without a contract
        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmissionType = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        var car = await TestDataCreateCar.CreateTestCar(
            _dbContext,
            owner.Id,
            model.Id,
            transmissionType,
            fuelType,
            CarStatusEnum.Available
        );

        var booking = await TestDataCreateBooking.CreateTestBooking(
            _dbContext,
            driver.Id,
            car.Id,
            BookingStatusEnum.Pending
        );

        var handler = new ApproveBooking.Handler(
            _dbContext,
            _emailService,
            _backgroundJobClient,
            _currentUser,
            _paymentTokenService,
            _aesService,
            _keyService,
            _encryptionSettings,
            _contractSettings
        );
        var command = new ApproveBooking.Command(
            booking.Id,
            true,
            TEST_BASE_URL,
            TEST_SIGNATURE_BASE64
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Contains("Đã phê duyệt booking thành công", result.SuccessMessage);

        // Verify booking was updated
        var updatedBooking = await _dbContext.Bookings.FirstAsync(b => b.Id == booking.Id);
        Assert.Equal(BookingStatusEnum.Approved, updatedBooking.Status);

        // Verify no contract was created
        var contractExists = await _dbContext.Contracts.AnyAsync(c => c.BookingId == booking.Id);
        Assert.False(contractExists);
    }
}
