using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Persistance.Data;
using UseCases.DTOs;
using UseCases.UC_InspectionSchedule.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;
using UUIDNext;

namespace UseCases.UnitTests.UC_InspectionSchedule.Commands;

[Collection("Test Collection")]
public class CreateInspectionScheduleTest(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly CurrentUser _currentUser = fixture.CurrentUser;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;
    private readonly GeometryFactory _geometryFactory = new(new PrecisionModel(), 4326);

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_UserNotConsultant_ReturnsForbidden()
    {
        // Arrange
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var user = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(user);

        var handler = new CreateInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new CreateInspectionSchedule.Command(
            TechnicianId: Guid.NewGuid(),
            CarId: Guid.NewGuid(),
            InspectionAddress: "123 Main St",
            InspectionDate: DateTimeOffset.UtcNow.AddDays(1)
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains(ResponseMessages.ForbiddenAudit, result.Errors);
    }

    [Fact]
    public async Task Handle_CarNotFound_ReturnsError()
    {
        // Arrange
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var user = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);
        _currentUser.SetUser(user);

        var handler = new CreateInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new CreateInspectionSchedule.Command(
            TechnicianId: Guid.NewGuid(),
            CarId: Guid.NewGuid(),
            InspectionAddress: "123 Main St",
            InspectionDate: DateTimeOffset.UtcNow.AddDays(1)
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.CarNotFound, result.Errors);
    }

    [Fact]
    public async Task Handle_CarNotInPendingStatus_ForNewCarSchedule_ReturnsError()
    {
        // Arrange
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var user = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);
        _currentUser.SetUser(user);

        // Create car with non-pending status
        var owner = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner")
        );
        var car = await CreateTestCar(owner.Id, CarStatusEnum.Available);

        var handler = new CreateInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new CreateInspectionSchedule.Command(
            TechnicianId: Guid.NewGuid(),
            CarId: car.Id,
            InspectionAddress: "123 Main St",
            InspectionDate: DateTimeOffset.UtcNow.AddDays(1),
            Type: InspectionScheduleType.NewCar
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.CarIsNotInPending, result.Errors);
    }

    [Fact]
    public async Task Handle_TechnicianIdNotFound_ReturnsError()
    {
        // Arrange
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var user = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);
        _currentUser.SetUser(user);

        // Create car in pending status
        var owner = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner")
        );
        var car = await CreateTestCar(owner.Id, CarStatusEnum.Pending);

        var handler = new CreateInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new CreateInspectionSchedule.Command(
            TechnicianId: Guid.NewGuid(),
            CarId: car.Id,
            InspectionAddress: "123 Main St",
            InspectionDate: DateTimeOffset.UtcNow.AddDays(1)
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.TechnicianNotFound, result.Errors);
    }

    [Fact]
    public async Task Handle_UserNotTechnician_ReturnsError()
    {
        // Arrange
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);
        _currentUser.SetUser(consultant);

        // Create another non-technician user
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var driver = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            driverRole,
            "driver@test.com"
        );

        // Create car in pending status
        var owner = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner")
        );
        var car = await CreateTestCar(owner.Id, CarStatusEnum.Pending);

        var handler = new CreateInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new CreateInspectionSchedule.Command(
            TechnicianId: driver.Id,
            CarId: car.Id,
            InspectionAddress: "123 Main St",
            InspectionDate: DateTimeOffset.UtcNow.AddDays(1)
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.TechnicianNotFound, result.Errors);
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesInspectionSchedule()
    {
        // Arrange
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);
        _currentUser.SetUser(consultant);

        // Create technician
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            technicianRole,
            "tech@test.com"
        );

        // Create car in pending status
        var owner = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner")
        );
        var car = await CreateTestCar(owner.Id, CarStatusEnum.Pending);

        var inspectionDate = DateTimeOffset.UtcNow.AddDays(1);
        var handler = new CreateInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new CreateInspectionSchedule.Command(
            TechnicianId: technician.Id,
            CarId: car.Id,
            InspectionAddress: "123 Main St",
            InspectionDate: inspectionDate
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);

        // Verify schedule was created
        var schedule = await _dbContext.InspectionSchedules.FirstOrDefaultAsync(s =>
            s.Id == result.Value.Id
        );

        Assert.NotNull(schedule);
        Assert.Equal(technician.Id, schedule.TechnicianId);
        Assert.Equal(car.Id, schedule.CarId);
        Assert.Equal(inspectionDate, schedule.InspectionDate);
        Assert.Equal("123 Main St", schedule.InspectionAddress);
        Assert.Equal(consultant.Id, schedule.CreatedBy);
        Assert.Equal(InspectionScheduleType.NewCar, schedule.Type); // Default is NewCar
    }

    [Fact]
    public async Task Handle_CarHasActiveSchedule_ReturnsError()
    {
        // Arrange
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);
        _currentUser.SetUser(consultant);

        // Create technician
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            technicianRole,
            "tech@test.com"
        );

        // Create car in pending status
        var owner = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner")
        );
        var car = await CreateTestCar(owner.Id, CarStatusEnum.Pending);

        // Create an existing active schedule for the car
        var existingSchedule = new InspectionSchedule
        {
            TechnicianId = technician.Id,
            CarId = car.Id,
            Status = InspectionScheduleStatusEnum.Pending,
            InspectionAddress = "456 Existing St",
            InspectionDate = DateTimeOffset.UtcNow.AddDays(2),
            CreatedBy = consultant.Id,
            Type = InspectionScheduleType.NewCar,
        };
        await _dbContext.InspectionSchedules.AddAsync(existingSchedule);
        await _dbContext.SaveChangesAsync();

        var handler = new CreateInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new CreateInspectionSchedule.Command(
            TechnicianId: technician.Id,
            CarId: car.Id,
            InspectionAddress: "123 Main St",
            InspectionDate: DateTimeOffset.UtcNow.AddDays(1),
            Type: InspectionScheduleType.NewCar
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.CarHadInspectionSchedule, result.Errors);
    }

    [Fact]
    public async Task Handle_TechnicianHasScheduleWithinOneHour_ReturnsError()
    {
        // Arrange
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);
        _currentUser.SetUser(consultant);

        // Create technician
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            technicianRole,
            "tech@test.com"
        );

        // Create car in pending status
        var owner = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner")
        );
        var car1 = await CreateTestCar(owner.Id, CarStatusEnum.Pending);
        var car2 = await CreateTestCar(owner.Id, CarStatusEnum.Pending);

        // Create a base time for testing
        var baseTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(10); // 10 AM tomorrow

        // Create an existing pending schedule
        var existingSchedule = new InspectionSchedule
        {
            TechnicianId = technician.Id,
            CarId = car1.Id,
            Status = InspectionScheduleStatusEnum.Pending,
            InspectionAddress = "456 Existing St",
            InspectionDate = baseTime,
            CreatedBy = consultant.Id,
            Type = InspectionScheduleType.NewCar,
        };
        await _dbContext.InspectionSchedules.AddAsync(existingSchedule);
        await _dbContext.SaveChangesAsync();

        // Request a schedule for same technician within one hour of existing schedule
        var requestedDate = baseTime.AddMinutes(45); // 45 minutes after existing schedule
        var handler = new CreateInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new CreateInspectionSchedule.Command(
            TechnicianId: technician.Id,
            CarId: car2.Id,
            InspectionAddress: "123 Main St",
            InspectionDate: requestedDate,
            Type: InspectionScheduleType.NewCar
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(
            ResponseMessages.TechnicianHasInspectionScheduleWithinOneHour,
            result.Errors
        );
    }

    [Fact]
    public async Task Handle_CarHasRejectedSchedule_AllowsNewSchedule()
    {
        // Arrange
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);
        _currentUser.SetUser(consultant);

        // Create technician
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            technicianRole,
            "tech@test.com"
        );

        // Create car in pending status
        var owner = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner")
        );
        var car = await CreateTestCar(owner.Id, CarStatusEnum.Pending);

        // Create a rejected schedule for the car (can be same technician)
        var rejectedSchedule = new InspectionSchedule
        {
            TechnicianId = technician.Id,
            CarId = car.Id,
            Status = InspectionScheduleStatusEnum.Rejected,
            InspectionAddress = "456 Rejected St",
            InspectionDate = DateTimeOffset.UtcNow.AddDays(-1),
            CreatedBy = consultant.Id,
            Type = InspectionScheduleType.NewCar,
        };
        await _dbContext.InspectionSchedules.AddAsync(rejectedSchedule);
        await _dbContext.SaveChangesAsync();

        var inspectionDate = DateTimeOffset.UtcNow.AddDays(1);
        var handler = new CreateInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new CreateInspectionSchedule.Command(
            TechnicianId: technician.Id,
            CarId: car.Id,
            InspectionAddress: "123 Main St",
            InspectionDate: inspectionDate
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);

        // Verify schedule was created
        var schedule = await _dbContext.InspectionSchedules.FirstOrDefaultAsync(s =>
            s.Id == result.Value.Id
        );

        Assert.NotNull(schedule);
        Assert.Equal(technician.Id, schedule.TechnicianId);
        Assert.Equal(car.Id, schedule.CarId);
    }

    [Fact]
    public async Task Handle_CarHasExpiredScheduleWithDifferentTechnician_AllowsNewSchedule()
    {
        // Arrange
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);
        _currentUser.SetUser(consultant);

        // Create two technicians
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician1 = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            technicianRole,
            "tech1@test.com"
        );
        var technician2 = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            technicianRole,
            "tech2@test.com"
        );

        // Create car in pending status
        var owner = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner")
        );
        var car = await CreateTestCar(owner.Id, CarStatusEnum.Pending);

        // Create an expired schedule for the car with technician1
        var expiredSchedule = new InspectionSchedule
        {
            TechnicianId = technician1.Id,
            CarId = car.Id,
            Status = InspectionScheduleStatusEnum.Expired,
            InspectionAddress = "456 Expired St",
            InspectionDate = DateTimeOffset.UtcNow.AddDays(-2),
            CreatedBy = consultant.Id,
            Type = InspectionScheduleType.NewCar,
        };
        await _dbContext.InspectionSchedules.AddAsync(expiredSchedule);
        await _dbContext.SaveChangesAsync();

        var inspectionDate = DateTimeOffset.UtcNow.AddDays(1);
        var handler = new CreateInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new CreateInspectionSchedule.Command(
            TechnicianId: technician2.Id, // Different technician
            CarId: car.Id,
            InspectionAddress: "123 Main St",
            InspectionDate: inspectionDate
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);

        // Verify schedule was created
        var schedule = await _dbContext.InspectionSchedules.FirstOrDefaultAsync(s =>
            s.Id == result.Value.Id
        );

        Assert.NotNull(schedule);
        Assert.Equal(technician2.Id, schedule.TechnicianId);
        Assert.Equal(car.Id, schedule.CarId);
    }

    [Fact]
    public async Task Handle_ReportNotFound_ReturnsError()
    {
        // Arrange
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);
        _currentUser.SetUser(consultant);

        // Create technician
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            technicianRole,
            "tech@test.com"
        );

        // Create car (not in pending status for incident inspection)
        var owner = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner")
        );
        var car = await CreateTestCar(owner.Id, CarStatusEnum.Available);

        var nonExistentReportId = Guid.NewGuid();
        var handler = new CreateInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new CreateInspectionSchedule.Command(
            TechnicianId: technician.Id,
            CarId: car.Id,
            InspectionAddress: "123 Main St",
            InspectionDate: DateTimeOffset.UtcNow.AddDays(1),
            Type: InspectionScheduleType.Incident
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.ReportNotFound, result.Errors);
    }

    [Fact]
    public async Task Handle_ReportNotPending_ReturnsError()
    {
        // Arrange
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);
        _currentUser.SetUser(consultant);

        // Create technician
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            technicianRole,
            "tech@test.com"
        );

        // Create car in available status (for incident inspection)
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var car = await CreateTestCar(owner.Id, CarStatusEnum.Available);

        // Create a driver for the booking
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var driver = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            driverRole,
            "driver@test.com"
        );

        // Create a real booking first
        var booking = new Booking
        {
            UserId = driver.Id,
            CarId = car.Id,
            Status = BookingStatusEnum.Completed,
            StartTime = DateTimeOffset.UtcNow.AddDays(-7),
            EndTime = DateTimeOffset.UtcNow.AddDays(-6),
            ActualReturnTime = DateTimeOffset.UtcNow.AddDays(-6),
            BasePrice = 100.0m,
            PlatformFee = 10.0m,
            ExcessDay = 0,
            ExcessDayFee = 0,
            TotalAmount = 110.0m,
            Note = "Test booking for inspection report",
        };
        _dbContext.Bookings.Add(booking);
        await _dbContext.SaveChangesAsync();

        // Create a report with UnderReview status
        var report = new BookingReport
        {
            BookingId = booking.Id,
            ReportedById = owner.Id,
            Title = "Test Report",
            Description = "Test Description",
            ReportType = BookingReportType.Accident,
            Status = BookingReportStatus.UnderReview, // Not Pending
        };
        await _dbContext.BookingReports.AddAsync(report);
        await _dbContext.SaveChangesAsync();

        var handler = new CreateInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new CreateInspectionSchedule.Command(
            TechnicianId: technician.Id,
            CarId: car.Id,
            InspectionAddress: "123 Main St",
            InspectionDate: DateTimeOffset.UtcNow.AddDays(1),
            ReportId: report.Id,
            Type: InspectionScheduleType.Incident
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Báo cáo không ở trạng thái chờ", result.Errors);
    }

    [Fact]
    public async Task Handle_ValidReport_CreatesInspectionScheduleWithReport()
    {
        // Arrange
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);
        _currentUser.SetUser(consultant);

        // Create technician
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            technicianRole,
            "tech@test.com"
        );

        // Create car in available status (for incident inspection)
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var car = await CreateTestCar(owner.Id, CarStatusEnum.Available);

        // Create a driver for the booking
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var driver = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            driverRole,
            "driver@test.com"
        );

        // Create a real booking first
        var booking = new Booking
        {
            UserId = driver.Id,
            CarId = car.Id,
            Status = BookingStatusEnum.Completed,
            StartTime = DateTimeOffset.UtcNow.AddDays(-7),
            EndTime = DateTimeOffset.UtcNow.AddDays(-6),
            ActualReturnTime = DateTimeOffset.UtcNow.AddDays(-6),
            BasePrice = 100.0m,
            PlatformFee = 10.0m,
            ExcessDay = 0,
            ExcessDayFee = 0,
            TotalAmount = 110.0m,
            Note = "Test booking for inspection report",
        };
        _dbContext.Bookings.Add(booking);
        await _dbContext.SaveChangesAsync();

        // Create a report with Pending status
        var report = new BookingReport
        {
            BookingId = booking.Id,
            ReportedById = owner.Id,
            Title = "Test Report",
            Description = "Test Description",
            ReportType = BookingReportType.Accident,
            Status = BookingReportStatus.Pending,
        };
        await _dbContext.BookingReports.AddAsync(report);
        await _dbContext.SaveChangesAsync();

        var inspectionDate = DateTimeOffset.UtcNow.AddDays(1);
        var handler = new CreateInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new CreateInspectionSchedule.Command(
            TechnicianId: technician.Id,
            CarId: car.Id,
            InspectionAddress: "123 Main St",
            InspectionDate: inspectionDate,
            ReportId: report.Id,
            Type: InspectionScheduleType.Incident
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);

        // Verify schedule was created with report
        var schedule = await _dbContext.InspectionSchedules.FirstOrDefaultAsync(s =>
            s.Id == result.Value.Id
        );

        Assert.NotNull(schedule);
        Assert.Equal(technician.Id, schedule.TechnicianId);
        Assert.Equal(car.Id, schedule.CarId);
        Assert.Equal(report.Id, schedule.ReportId);
        Assert.Equal(InspectionScheduleType.Incident, schedule.Type);

        // Verify report status is set to UnderReview
        var updatedReport = await _dbContext.BookingReports.FindAsync(report.Id);
        Assert.NotNull(updatedReport);
        Assert.Equal(BookingReportStatus.UnderReview, updatedReport.Status);
    }

    [Fact]
    public async Task Handle_ValidReport_MaintainsReportUnderReviewStatus()
    {
        // Arrange
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);
        _currentUser.SetUser(consultant);

        // Create technician
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            technicianRole,
            "tech@test.com"
        );

        // Create car in available status (for incident inspection)
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var car = await CreateTestCar(owner.Id, CarStatusEnum.Available);

        // Create a driver for the booking
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var driver = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            driverRole,
            "driver@test.com"
        );

        // Create a real booking first
        var booking = new Booking
        {
            UserId = driver.Id,
            CarId = car.Id,
            Status = BookingStatusEnum.Completed,
            StartTime = DateTimeOffset.UtcNow.AddDays(-7),
            EndTime = DateTimeOffset.UtcNow.AddDays(-6),
            ActualReturnTime = DateTimeOffset.UtcNow.AddDays(-6),
            BasePrice = 100.0m,
            PlatformFee = 10.0m,
            ExcessDay = 0,
            ExcessDayFee = 0,
            TotalAmount = 110.0m,
            Note = "Test booking for inspection report",
        };
        _dbContext.Bookings.Add(booking);
        await _dbContext.SaveChangesAsync();

        // Create a report with Pending status
        var report = new BookingReport
        {
            BookingId = booking.Id,
            ReportedById = owner.Id,
            Title = "Test Report",
            Description = "Test Description",
            ReportType = BookingReportType.Accident,
            Status = BookingReportStatus.Pending,
        };
        await _dbContext.BookingReports.AddAsync(report);
        await _dbContext.SaveChangesAsync();

        var inspectionDate = DateTimeOffset.UtcNow.AddDays(1);
        var handler = new CreateInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new CreateInspectionSchedule.Command(
            TechnicianId: technician.Id,
            CarId: car.Id,
            InspectionAddress: "123 Main St",
            InspectionDate: inspectionDate,
            ReportId: report.Id,
            Type: InspectionScheduleType.Incident
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);

        // Verify schedule was created with report
        var schedule = await _dbContext.InspectionSchedules.FirstOrDefaultAsync(s =>
            s.Id == result.Value.Id
        );
        Assert.NotNull(schedule);
        Assert.Equal(report.Id, schedule.ReportId);

        // Verify report status is still UnderReview
        var updatedReport = await _dbContext.BookingReports.FirstOrDefaultAsync(r =>
            r.Id == report.Id
        );
        Assert.NotNull(updatedReport);
        Assert.Equal(BookingReportStatus.UnderReview, updatedReport.Status);

        // Verify ResolvedById is set to the current user
        Assert.Equal(consultant.Id, updatedReport.ResolvedById);
    }

    [Fact]
    public async Task Handle_IncidentInspection_CarInPendingStatus_ReturnsError()
    {
        // Arrange
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);
        _currentUser.SetUser(consultant);

        // Create technician
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            technicianRole,
            "tech@test.com"
        );

        // Create car in PENDING status
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var car = await CreateTestCar(owner.Id, CarStatusEnum.Pending);

        var handler = new CreateInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new CreateInspectionSchedule.Command(
            TechnicianId: technician.Id,
            CarId: car.Id,
            InspectionAddress: "123 Main St",
            InspectionDate: DateTimeOffset.UtcNow.AddDays(1),
            Type: InspectionScheduleType.Incident
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(
            "Không thể tao lịch sự cố cho xe đang chờ duyệt hoặc đã được thuê",
            result.Errors
        );
    }

    [Fact]
    public async Task Handle_IncidentInspection_CarInRentedStatus_ReturnsError()
    {
        // Arrange
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);
        _currentUser.SetUser(consultant);

        // Create technician
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            technicianRole,
            "tech@test.com"
        );

        // Create car in RENTED status
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var car = await CreateTestCar(owner.Id, CarStatusEnum.Rented);

        var handler = new CreateInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new CreateInspectionSchedule.Command(
            TechnicianId: technician.Id,
            CarId: car.Id,
            InspectionAddress: "123 Main St",
            InspectionDate: DateTimeOffset.UtcNow.AddDays(1),
            Type: InspectionScheduleType.Incident
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(
            "Không thể tao lịch sự cố cho xe đang chờ duyệt hoặc đã được thuê",
            result.Errors
        );
    }

    [Fact]
    public async Task Handle_IncidentInspection_CarWithActiveBooking_ReturnsError()
    {
        // Arrange
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);
        _currentUser.SetUser(consultant);

        // Create technician
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            technicianRole,
            "tech@test.com"
        );

        // Create car in available status
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var car = await CreateTestCar(owner.Id, CarStatusEnum.Available);

        // Create a driver for the booking
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var driver = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            driverRole,
            "driver@test.com"
        );

        // Create an active booking for the car
        var booking = new Booking
        {
            UserId = driver.Id,
            CarId = car.Id,
            Status = BookingStatusEnum.Approved,
            StartTime = DateTimeOffset.UtcNow.AddDays(1),
            EndTime = DateTimeOffset.UtcNow.AddDays(3),
            ActualReturnTime = DateTimeOffset.UtcNow.AddDays(3),
            BasePrice = 100.0m,
            PlatformFee = 10.0m,
            ExcessDay = 0,
            ExcessDayFee = 0.0m,
            TotalAmount = 110.0m,
            Note = "Active booking for testing",
        };
        _dbContext.Bookings.Add(booking);
        await _dbContext.SaveChangesAsync();

        var handler = new CreateInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new CreateInspectionSchedule.Command(
            TechnicianId: technician.Id,
            CarId: car.Id,
            InspectionAddress: "123 Main St",
            InspectionDate: DateTimeOffset.UtcNow.AddDays(2),
            Type: InspectionScheduleType.Incident
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Xe đang có lịch đặt không thể tạo lịch cho sự cố", result.Errors);
    }

    [Fact]
    public async Task Handle_ValidIncidentInspection_UpdatesCarToMaintainStatus()
    {
        // Arrange
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);
        _currentUser.SetUser(consultant);

        // Create technician
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            technicianRole,
            "tech@test.com"
        );

        // Create car in AVAILABLE status (not pending or rented)
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var car = await CreateTestCar(owner.Id, CarStatusEnum.Available);

        var carContract = new CarContract { CarId = car.Id, UpdatedAt = DateTimeOffset.UtcNow };
        await _dbContext.CarContracts.AddAsync(carContract);
        await _dbContext.SaveChangesAsync();

        var device = new GPSDevice
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            Name = "Test GPS",
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        await _dbContext.GPSDevices.AddAsync(device);
        await _dbContext.SaveChangesAsync();
        var carGps = new CarGPS
        {
            CarId = car.Id,
            DeviceId = device.Id,
            Location = _geometryFactory.CreatePoint(new Coordinate(0, 0)),
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        await _dbContext.CarGPSes.AddAsync(carGps);
        await _dbContext.SaveChangesAsync();

        // Create a driver for the booking
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var driver = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);

        // Create a real booking first
        var booking = new Booking
        {
            UserId = driver.Id,
            CarId = car.Id,
            Status = BookingStatusEnum.Completed,
            StartTime = DateTimeOffset.UtcNow.AddDays(-7),
            EndTime = DateTimeOffset.UtcNow.AddDays(-6),
            ActualReturnTime = DateTimeOffset.UtcNow.AddDays(-6),
            BasePrice = 100.0m,
            PlatformFee = 10.0m,
            ExcessDay = 0,
            ExcessDayFee = 0,
            TotalAmount = 110.0m,
            Note = "Test booking for inspection report",
        };
        _dbContext.Bookings.Add(booking);
        await _dbContext.SaveChangesAsync();

        // Create a report with Pending status
        var report = new BookingReport
        {
            BookingId = booking.Id,
            ReportedById = owner.Id,
            Title = "Test Report",
            Description = "Test Description",
            ReportType = BookingReportType.Accident,
            Status = BookingReportStatus.Pending,
        };
        await _dbContext.BookingReports.AddAsync(report);
        await _dbContext.SaveChangesAsync();

        var handler = new CreateInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new CreateInspectionSchedule.Command(
            TechnicianId: technician.Id,
            CarId: car.Id,
            InspectionAddress: "123 Main St",
            InspectionDate: DateTimeOffset.UtcNow.AddDays(1),
            ReportId: report.Id,
            Type: InspectionScheduleType.Incident
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);

        // Verify car status was updated to Maintain
        var updatedCar = await _dbContext.Cars.FirstOrDefaultAsync(c => c.Id == car.Id);
        Assert.NotNull(updatedCar);
        Assert.Equal(CarStatusEnum.Maintain, updatedCar.Status);

        // Verify schedule was created with correct type
        var schedule = await _dbContext.InspectionSchedules.FirstOrDefaultAsync(s =>
            s.Id == result.Value.Id
        );
        Assert.NotNull(schedule);
        Assert.Equal(report.Id, schedule.ReportId);
        Assert.Equal(InspectionScheduleType.Incident, schedule.Type);

        // Verify report status is set to UnderReview
        var updatedReport = await _dbContext.BookingReports.FirstOrDefaultAsync(r =>
            r.Id == report.Id
        );
        Assert.NotNull(updatedReport);
        Assert.Equal(BookingReportStatus.UnderReview, updatedReport.Status);
    }

    [Fact]
    public async Task Handle_ChangeGPS_CarReportNotFound_ReturnsError()
    {
        // Arrange
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);
        _currentUser.SetUser(consultant);

        // Create technician
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            technicianRole,
            "tech@test.com"
        );

        // Create car in active status (for GPS change)
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var car = await CreateTestCar(owner.Id, CarStatusEnum.Available);

        var handler = new CreateInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new CreateInspectionSchedule.Command(
            TechnicianId: technician.Id,
            CarId: car.Id,
            InspectionAddress: "123 Main St",
            InspectionDate: DateTimeOffset.UtcNow.AddDays(1),
            Type: InspectionScheduleType.ChangeGPS
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.ReportNotFound, result.Errors);
    }

    [Fact]
    public async Task Handle_ChangeGPS_CarReportNotPending_ReturnsError()
    {
        // Arrange
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);
        _currentUser.SetUser(consultant);

        // Create technician
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            technicianRole,
            "tech@test.com"
        );

        // Create car in active status (for GPS change)
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var car = await CreateTestCar(owner.Id, CarStatusEnum.Available);

        //create new driver
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var driver = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);

        // Create a car report with UnderReview status
        var carReport = new CarReport
        {
            ReportedById = driver.Id,
            CarId = car.Id,
            Title = "GPS Issue",
            Description = "GPS device malfunction",
            ReportType = CarReportType.ChangeGPS,
            Status = CarReportStatus.UnderReview, // Not Pending
        };
        await _dbContext.CarReports.AddAsync(carReport);
        await _dbContext.SaveChangesAsync();

        var handler = new CreateInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new CreateInspectionSchedule.Command(
            TechnicianId: technician.Id,
            CarId: car.Id,
            InspectionAddress: "123 Main St",
            InspectionDate: DateTimeOffset.UtcNow.AddDays(1),
            ReportId: carReport.Id,
            Type: InspectionScheduleType.ChangeGPS
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Báo cáo không ở trạng thái chờ", result.Errors);
    }

    [Fact]
    public async Task Handle_ChangeGPS_ValidCarReport_CreatesInspectionSchedule()
    {
        // Arrange
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);
        _currentUser.SetUser(consultant);

        // Create technician
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            technicianRole,
            "tech@test.com"
        );

        // Create car in active status (for GPS change)
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var car = await CreateTestCar(owner.Id, CarStatusEnum.Available);

        //create new driver
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var driver = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);

        // Create a car report with Pending status
        var carReport = new CarReport
        {
            ReportedById = driver.Id,
            CarId = car.Id,
            Title = "GPS Issue",
            Description = "GPS device malfunction",
            ReportType = CarReportType.ChangeGPS,
            Status = CarReportStatus.Pending,
        };
        await _dbContext.CarReports.AddAsync(carReport);
        await _dbContext.SaveChangesAsync();

        var inspectionDate = DateTimeOffset.UtcNow.AddDays(1);
        var handler = new CreateInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new CreateInspectionSchedule.Command(
            TechnicianId: technician.Id,
            CarId: car.Id,
            InspectionAddress: "123 Main St",
            InspectionDate: inspectionDate,
            ReportId: carReport.Id,
            Type: InspectionScheduleType.ChangeGPS
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);

        // Verify schedule was created with report
        var schedule = await _dbContext.InspectionSchedules.FirstOrDefaultAsync(s =>
            s.Id == result.Value.Id
        );

        Assert.NotNull(schedule);
        Assert.Equal(technician.Id, schedule.TechnicianId);
        Assert.Equal(car.Id, schedule.CarId);
        Assert.Equal(carReport.Id, schedule.CarReportId);
        Assert.Equal(InspectionScheduleType.ChangeGPS, schedule.Type);

        // Verify car status was updated to Maintain
        var updatedCar = await _dbContext.Cars.FirstOrDefaultAsync(c => c.Id == car.Id);
        Assert.NotNull(updatedCar);
        Assert.Equal(CarStatusEnum.Maintain, updatedCar.Status);

        // Verify report status was updated to UnderReview
        var updatedReport = await _dbContext.CarReports.FirstOrDefaultAsync(r =>
            r.Id == carReport.Id
        );
        Assert.NotNull(updatedReport);
        Assert.Equal(CarReportStatus.UnderReview, updatedReport.Status);
    }

    [Fact]
    public async Task Handle_ChangeGPS_UpdatesResolvedByIdInCarReport()
    {
        // Arrange
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);
        _currentUser.SetUser(consultant);

        // Create technician
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            technicianRole,
            "tech@test.com"
        );

        // Create car in active status (for GPS change)
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var car = await CreateTestCar(owner.Id, CarStatusEnum.Available);

        //create new driver
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var driver = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);

        // Create a car report with Pending status
        var carReport = new CarReport
        {
            ReportedById = driver.Id,
            CarId = car.Id,
            Title = "GPS Issue",
            Description = "GPS device malfunction",
            ReportType = CarReportType.ChangeGPS,
            Status = CarReportStatus.Pending,
        };
        await _dbContext.CarReports.AddAsync(carReport);
        await _dbContext.SaveChangesAsync();

        var handler = new CreateInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new CreateInspectionSchedule.Command(
            TechnicianId: technician.Id,
            CarId: car.Id,
            InspectionAddress: "123 Main St",
            InspectionDate: DateTimeOffset.UtcNow.AddDays(1),
            ReportId: carReport.Id,
            Type: InspectionScheduleType.ChangeGPS
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);

        // Verify ResolvedById was updated
        var updatedReport = await _dbContext.CarReports.FirstOrDefaultAsync(r =>
            r.Id == carReport.Id
        );
        Assert.NotNull(updatedReport);
        Assert.Equal(CarReportStatus.UnderReview, updatedReport.Status);
        Assert.Equal(consultant.Id, updatedReport.ResolvedById);
    }

    private async Task<Car> CreateTestCar(Guid ownerId, CarStatusEnum status)
    {
        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmissionType = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Gasoline");

        var car = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: ownerId,
            modelId: model.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: status
        );

        //create car contract
        var carContract = new CarContract
        {
            CarId = car.Id,
            Status = CarContractStatusEnum.Pending,
        };
        await _dbContext.CarContracts.AddAsync(carContract);
        await _dbContext.SaveChangesAsync();

        return car;
    }
}
