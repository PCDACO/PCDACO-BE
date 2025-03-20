using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
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
        var query = new GetAllInspectionSchedules.Query(Guid.NewGuid(), MonthEnum.January, 2022);

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
        var query = new GetAllInspectionSchedules.Query(Guid.NewGuid(), MonthEnum.January, 2022);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Empty(result.Value);
        Assert.Equal(ResponseMessages.Fetched, result.SuccessMessage);
    }

    [Fact]
    public async Task Handle_WithTechnicianFilter_ReturnsFilteredSchedules()
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
        var technician1 = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            technicianRole,
            "tech1@example.com",
            "John Doe",
            "0974567890"
        );
        var technician2 = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            technicianRole,
            "tech2@example.com",
            "Jane Smith",
            "0974567891"
        );

        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);

        var car = await CreateTestCar(owner.Id);

        // Create schedules for different technicians
        var schedules = new[]
        {
            new InspectionSchedule
            {
                TechnicianId = technician1.Id,
                CarId = car.Id,
                Status = InspectionScheduleStatusEnum.Pending,
                InspectionAddress = "123 Tech1 St",
                InspectionDate = DateTimeOffset.UtcNow,
                Note = "Technician 1 schedule",
                CreatedBy = consultant.Id,
            },
            new InspectionSchedule
            {
                TechnicianId = technician2.Id,
                CarId = car.Id,
                Status = InspectionScheduleStatusEnum.Pending,
                InspectionAddress = "456 Tech2 St",
                InspectionDate = DateTimeOffset.UtcNow,
                Note = "Technician 2 schedule",
                CreatedBy = consultant.Id,
            },
        };
        await _dbContext.InspectionSchedules.AddRangeAsync(schedules);
        await _dbContext.SaveChangesAsync();

        var handler = new GetAllInspectionSchedules.Handler(_dbContext, _currentUser);
        var query = new GetAllInspectionSchedules.Query(technician1.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Single(result.Value);
        Assert.Equal("John Doe", result.Value.First().TechnicianName);
        Assert.Equal("123 Tech1 St", result.Value.First().InspectionAddress);
        Assert.Equal(ResponseMessages.Fetched, result.SuccessMessage);
    }

    [Fact]
    public async Task Handle_WithMonthYearFilter_ReturnsFilteredSchedules()
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
        // Create schedules for different months/years
        var januaryDate = new DateTimeOffset(2023, 1, 15, 10, 0, 0, TimeSpan.Zero);
        var februaryDate = new DateTimeOffset(2023, 2, 15, 10, 0, 0, TimeSpan.Zero);
        var nextYearDate = new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero);

        var schedules = new[]
        {
            new InspectionSchedule
            {
                TechnicianId = technician.Id,
                CarId = car.Id,
                Status = InspectionScheduleStatusEnum.Pending,
                InspectionAddress = "123 January St",
                InspectionDate = januaryDate,
                Note = "January 2023 schedule",
                CreatedBy = consultant.Id,
            },
            new InspectionSchedule
            {
                TechnicianId = technician.Id,
                CarId = car.Id,
                Status = InspectionScheduleStatusEnum.Approved,
                InspectionAddress = "456 February St",
                InspectionDate = februaryDate,
                Note = "February 2023 schedule",
                CreatedBy = consultant.Id,
            },
            new InspectionSchedule
            {
                TechnicianId = technician.Id,
                CarId = car.Id,
                Status = InspectionScheduleStatusEnum.Rejected,
                InspectionAddress = "789 January Next Year St",
                InspectionDate = nextYearDate,
                Note = "January 2024 schedule",
                CreatedBy = consultant.Id,
            },
        };
        await _dbContext.InspectionSchedules.AddRangeAsync(schedules);
        await _dbContext.SaveChangesAsync();

        var handler = new GetAllInspectionSchedules.Handler(_dbContext, _currentUser);

        // Query for January 2023
        var januaryQuery = new GetAllInspectionSchedules.Query(null, MonthEnum.January, 2023);

        // Query for 2023 (all months)
        var yearQuery = new GetAllInspectionSchedules.Query(null, null, 2023);

        // Act
        var januaryResult = await handler.Handle(januaryQuery, CancellationToken.None);
        var yearResult = await handler.Handle(yearQuery, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, januaryResult.Status);
        Assert.Single(januaryResult.Value);
        Assert.Equal("123 January St", januaryResult.Value.First().InspectionAddress);

        Assert.Equal(ResultStatus.Ok, yearResult.Status);
        Assert.Equal(2, yearResult.Value.Count());
        Assert.Contains(yearResult.Value, s => s.InspectionAddress == "123 January St");
        Assert.Contains(yearResult.Value, s => s.InspectionAddress == "456 February St");
        Assert.DoesNotContain(
            yearResult.Value,
            s => s.InspectionAddress == "789 January Next Year St"
        );
    }

    [Fact]
    public async Task Handle_WithDefaultYear_UsesCurrentYear()
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
        // Create schedules for current year and last year
        var currentYear = DateTimeOffset.UtcNow.Year;
        var currentYearDate = new DateTimeOffset(currentYear, 3, 15, 10, 0, 0, TimeSpan.Zero);
        var lastYearDate = new DateTimeOffset(currentYear - 1, 3, 15, 10, 0, 0, TimeSpan.Zero);

        var schedules = new[]
        {
            new InspectionSchedule
            {
                TechnicianId = technician.Id,
                CarId = car.Id,
                Status = InspectionScheduleStatusEnum.InProgress,
                InspectionAddress = "123 Current Year St",
                InspectionDate = currentYearDate,
                Note = "Current year schedule",
                CreatedBy = consultant.Id,
            },
            new InspectionSchedule
            {
                TechnicianId = technician.Id,
                CarId = car.Id,
                Status = InspectionScheduleStatusEnum.Expired,
                InspectionAddress = "456 Last Year St",
                InspectionDate = lastYearDate,
                Note = "Last year schedule",
                CreatedBy = consultant.Id,
            },
        };
        await _dbContext.InspectionSchedules.AddRangeAsync(schedules);
        await _dbContext.SaveChangesAsync();

        var handler = new GetAllInspectionSchedules.Handler(_dbContext, _currentUser);

        // Query with month but no year (should default to current year)
        var query = new GetAllInspectionSchedules.Query(null, MonthEnum.March, null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Single(result.Value);
        Assert.Equal("123 Current Year St", result.Value.First().InspectionAddress);
        Assert.Equal(currentYear, result.Value.First().InspectionDate.Year);
    }

    [Fact]
    public async Task Handle_WithNoFilters_ReturnsAllSchedules()
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

        // Create multiple schedules
        var schedules = new List<InspectionSchedule>();
        for (int i = 0; i < 3; i++)
        {
            var schedule = new InspectionSchedule
            {
                TechnicianId = technician.Id,
                CarId = car.Id,
                Status = InspectionScheduleStatusEnum.Pending,
                InspectionAddress = $"123 Main St {i + 1}",
                InspectionDate = DateTimeOffset.UtcNow.AddDays(i),
                Note = $"Schedule {i + 1}",
                CreatedBy = consultant.Id,
            };
            schedules.Add(schedule);
        }
        await _dbContext.InspectionSchedules.AddRangeAsync(schedules);
        await _dbContext.SaveChangesAsync();

        var handler = new GetAllInspectionSchedules.Handler(_dbContext, _currentUser);
        var query = new GetAllInspectionSchedules.Query(null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(3, result.Value.Count());
        Assert.Equal(ResponseMessages.Fetched, result.SuccessMessage);

        // Verify they are ordered by ID ascending by default
        var items = result.Value.ToList();
        Assert.True(items[0].Id < items[1].Id);
        Assert.True(items[1].Id < items[2].Id);
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

        return await TestDataCreateCar.CreateTestCar(
            _dbContext,
            ownerId,
            model.Id,
            transmissionType,
            fuelType,
            CarStatusEnum.Pending
        );
    }
}
