using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Persistance.Data;
using UseCases.DTOs;
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
    private readonly CurrentUser _currentUser = fixture.CurrentUser;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;

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
            _currentUser
        );
        var command = new ApproveBooking.Command(Guid.NewGuid(), true);

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
            _currentUser
        );
        var command = new ApproveBooking.Command(Guid.NewGuid(), true);

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
            _currentUser
        );
        var command = new ApproveBooking.Command(booking.Id, true);

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
            _currentUser
        );
        var command = new ApproveBooking.Command(booking.Id, isApproved);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Contains($"Đã {expectedMessage} booking thành công", result.SuccessMessage);

        var updatedBooking = await _dbContext
            .Bookings
            .FirstAsync(b => b.Id == booking.Id);

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
            _currentUser
        );
        var command = new ApproveBooking.Command(booking.Id, isApproved);

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
            _currentUser
        );
        var command = new ApproveBooking.Command(booking.Id, true);

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
            _currentUser
        );
        var command = new ApproveBooking.Command(booking.Id, true);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
    }
}