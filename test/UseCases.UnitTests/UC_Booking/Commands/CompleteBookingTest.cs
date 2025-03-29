using Ardalis.Result;
using Domain.Constants;
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
public class CompleteBookingTests(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly IBackgroundJobClient _backgroundJobClient = new BackgroundJobClient();
    private readonly TestDataEmailService _emailService = new();
    private readonly CurrentUser _currentUser = fixture.CurrentUser;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_UserNotDriver_ReturnsError()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        _currentUser.SetUser(testUser);

        var handler = new CompleteBooking.Handler(
            _dbContext,
            _backgroundJobClient,
            _emailService,
            _currentUser
        );
        var command = new CompleteBooking.Command(Guid.NewGuid());

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
        UserRole driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(testUser);

        var handler = new CompleteBooking.Handler(
            _dbContext,
            _backgroundJobClient,
            _emailService,
            _currentUser
        );
        var command = new CompleteBooking.Command(Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains("Không tìm thấy booking", result.Errors);
    }

    [Theory]
    [InlineData(BookingStatusEnum.Pending)]
    [InlineData(BookingStatusEnum.Approved)]
    [InlineData(BookingStatusEnum.Rejected)]
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
        _currentUser.SetUser(driver);

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

        var handler = new CompleteBooking.Handler(
            _dbContext,
            _backgroundJobClient,
            _emailService,
            _currentUser
        );
        var command = new CompleteBooking.Command(booking.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Contains($"Không thể phê duyệt booking ở trạng thái {status}", result.Errors);
    }

    [Fact]
    public async Task Handle_ValidRequest_UpdatesBookingStatusToCompleted()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        UserRole driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        UserRole adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");

        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var driver = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            driverRole,
            "driver@test.com"
        );
        var admin = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            adminRole,
            "admin@test.com"
        );
        _currentUser.SetUser(driver);

        // Setup transaction types
        await TestDataTransactionType.InitializeTestTransactionTypes(_dbContext);

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
            BookingStatusEnum.Ongoing
        );

        var handler = new CompleteBooking.Handler(
            _dbContext,
            _backgroundJobClient,
            _emailService,
            _currentUser
        );
        var command = new CompleteBooking.Command(booking.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);

        // Verify response structure
        Assert.NotNull(result.Value);
        Assert.Equal(0, result.Value.TotalDistance); // No tracking in test
        Assert.Equal(100m, result.Value.BasePrice);
        Assert.Equal(10m, result.Value.PlatformFee);
        Assert.Equal(110m, result.Value.FinalAmount);

        var updatedBooking = await _dbContext.Bookings.FirstAsync(b => b.Id == booking.Id);

        Assert.Equal(BookingStatusEnum.Completed, updatedBooking.Status);
        Assert.True(updatedBooking.ActualReturnTime > DateTime.UtcNow.AddMinutes(-1));
    }

    // [Fact]
    // public async Task Handle_StatusNotFound_ReturnsNotFound()
    // {
    //     // Arrange
    //     UserRole driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
    //     var driver = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
    //     _currentUser.SetUser(driver);
    //
    //     await _dbContext.SaveChangesAsync();
    //
    //     var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
    //     var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
    //     var transmissionType = await TestDataTransmissionType.CreateTestTransmissionType(
    //         _dbContext,
    //         "Automatic"
    //     );
    //     var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");
    //     var owner = await TestDataCreateUser.CreateTestUser(
    //         _dbContext,
    //         await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner")
    //     );
    //
    //     var car = await TestDataCreateCar.CreateTestCar(
    //         _dbContext,
    //         owner.Id,
    //         model.Id,
    //         transmissionType,
    //         fuelType,
    //         CarStatusEnum.Available
    //     );
    //
    //     var booking = await TestDataCreateBooking.CreateTestBooking(
    //         _dbContext,
    //         driver.Id,
    //         car.Id,
    //         BookingStatusEnum.Ongoing
    //     );
    //
    //     var handler = new CompleteBooking.Handler(_dbContext, _backgroundJobClient, _emailService, _currentUser);
    //     var command = new CompleteBooking.Command(booking.Id););
    //
    //     // Act
    //     var result = await handler.Handle(command, CancellationToken.None);
    //
    //     // Assert
    //     Assert.Equal(ResultStatus.NotFound, result.Status);
    //     Assert.Contains("Không tìm thấy trạng thái phù hợp", result.Errors);
    // }
}
