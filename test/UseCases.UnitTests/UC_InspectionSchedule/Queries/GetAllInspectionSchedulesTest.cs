using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Persistance.Data;
using UseCases.DTOs;
using UseCases.UC_InspectionSchedule.Queries;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_InspectionSchedule.Queries;

[Collection("Test Collection")]
public class GetAllInspectionSchedulesTest(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly CurrentUser _currentUser = fixture.CurrentUser;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_UserNotConsultant_ReturnsForbidden()
    {
        // Arrange
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var user = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(user);

        var handler = new GetAllInspectionSchedules.Handler(_dbContext, _currentUser);
        var query = new GetAllInspectionSchedules.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains(ResponseMessages.ForbiddenAudit, result.Errors);
    }

    [Fact]
    public async Task Handle_NoSchedules_ReturnsEmptyList()
    {
        // Arrange
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);
        _currentUser.SetUser(consultant);

        var handler = new GetAllInspectionSchedules.Handler(_dbContext, _currentUser);
        var query = new GetAllInspectionSchedules.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalItems);
        Assert.False(result.Value.HasNext);
    }

    [Fact]
    public async Task Handle_WithKeywordSearch_ReturnsFilteredSchedules()
    {
        // Arrange
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);
        _currentUser.SetUser(consultant);

        // Create prerequisites
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            technicianRole,
            "JohnDoe@example.com",
            "John Doe",
            "0974567890"
        );

        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole, "Jane Smith");

        var car = await CreateTestCar(owner.Id);
        var pendingStatus = await TestDataCreateInspectionStatus.CreateTestInspectionStatus(
            _dbContext
        );

        var schedule = new InspectionSchedule
        {
            TechnicianId = technician.Id,
            CarId = car.Id,
            InspectionStatusId = pendingStatus.Id,
            InspectionAddress = "123 Main St 1",
            InspectionDate = DateTimeOffset.UtcNow,
            Note = "Test schedule",
        };
        await _dbContext.InspectionSchedules.AddAsync(schedule);
        await _dbContext.SaveChangesAsync();

        var handler = new GetAllInspectionSchedules.Handler(_dbContext, _currentUser);

        // Search by technician name
        var query1 = new GetAllInspectionSchedules.Query(Keyword: "John");
        var query2 = new GetAllInspectionSchedules.Query(Keyword: "xyz");

        // Act
        var result1 = await handler.Handle(query1, CancellationToken.None);
        var result2 = await handler.Handle(query2, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result1.Status);
        Assert.Single(result1.Value.Items);
        Assert.Equal("John Doe", result1.Value.Items.First().TechnicianName);

        Assert.Equal(ResultStatus.Ok, result2.Status);
        Assert.Empty(result2.Value.Items);
    }

    [Fact]
    public async Task Handle_WithDateFilter_ReturnsFilteredSchedules()
    {
        // Arrange
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);
        _currentUser.SetUser(consultant);

        // Create prerequisites
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(_dbContext, technicianRole);

        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);

        var car = await CreateTestCar(owner.Id);
        var pendingStatus = await TestDataCreateInspectionStatus.CreateTestInspectionStatus(
            _dbContext
        );

        var today = DateTimeOffset.UtcNow;
        var tomorrow = today.AddDays(1);

        var schedules = new[]
        {
            new InspectionSchedule
            {
                TechnicianId = technician.Id,
                CarId = car.Id,
                InspectionStatusId = pendingStatus.Id,
                InspectionAddress = "123 Main St 2",
                InspectionDate = today,
                Note = "Today's schedule",
            },
            new InspectionSchedule
            {
                TechnicianId = technician.Id,
                CarId = car.Id,
                InspectionStatusId = pendingStatus.Id,
                InspectionAddress = "123 Main St 3",
                InspectionDate = tomorrow,
                Note = "Tomorrow's schedule",
            },
        };
        await _dbContext.InspectionSchedules.AddRangeAsync(schedules);
        await _dbContext.SaveChangesAsync();

        var handler = new GetAllInspectionSchedules.Handler(_dbContext, _currentUser);
        var query = new GetAllInspectionSchedules.Query(InspectionDate: today);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Single(result.Value.Items);
        Assert.Equal("123 Main St 2", result.Value.Items.First().InspectionAddress);
        Assert.Equal(today.Date, result.Value.Items.First().InspectionDate.Date);
    }

    [Theory]
    [InlineData("asc")]
    [InlineData("desc")]
    public async Task Handle_WithSorting_ReturnsSortedSchedules(string sortOrder)
    {
        // Arrange
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);
        _currentUser.SetUser(consultant);

        // Create prerequisites
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(_dbContext, technicianRole);

        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);

        var car = await CreateTestCar(owner.Id);
        var pendingStatus = await TestDataCreateInspectionStatus.CreateTestInspectionStatus(
            _dbContext
        );

        // Create multiple schedules
        var schedules = new List<InspectionSchedule>();
        for (int i = 0; i < 3; i++)
        {
            var schedule = new InspectionSchedule
            {
                TechnicianId = technician.Id,
                CarId = car.Id,
                InspectionStatusId = pendingStatus.Id,
                InspectionAddress = $"123 Main St {i + 1}",
                InspectionDate = DateTimeOffset.UtcNow.AddDays(i),
                Note = $"Schedule {i + 1}",
            };
            schedules.Add(schedule);
        }
        await _dbContext.InspectionSchedules.AddRangeAsync(schedules);
        await _dbContext.SaveChangesAsync();

        var handler = new GetAllInspectionSchedules.Handler(_dbContext, _currentUser);
        var query = new GetAllInspectionSchedules.Query(SortOrder: sortOrder);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(3, result.Value.Items.Count());

        var items = result.Value.Items.ToList();
        if (sortOrder == "asc")
        {
            Assert.True(items[0].Id < items[1].Id);
            Assert.True(items[1].Id < items[2].Id);
        }
        else
        {
            Assert.True(items[0].Id > items[1].Id);
            Assert.True(items[1].Id > items[2].Id);
        }
    }

    private async Task<Car> CreateTestCar(Guid ownerId)
    {
        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmissionType = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");
        var carStatus = await TestDataCarStatus.CreateTestCarStatus(_dbContext, "Pending");

        return await TestDataCreateCar.CreateTestCar(
            _dbContext,
            ownerId,
            model.Id,
            transmissionType,
            fuelType,
            carStatus
        );
    }
}
