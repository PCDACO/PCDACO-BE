using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Persistance.Data;
using UseCases.Abstractions;
using UseCases.UC_InspectionSchedule.Queries;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_InspectionSchedule.Queries;

[Collection("Test Collection")]
public class GetInspectionScheduleDetailTest(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly IAesEncryptionService _aesEncryptionService = fixture.AesEncryptionService;
    private readonly IKeyManagementService _keyManagementService = fixture.KeyManagementService;
    private readonly EncryptionSettings _encryptionSettings = fixture.EncryptionSettings;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_ScheduleNotFound_ReturnsNotFound()
    {
        // Arrange
        var handler = new GetInspectionScheduleDetail.Handler(
            _dbContext,
            _aesEncryptionService,
            _keyManagementService,
            _encryptionSettings
        );
        var query = new GetInspectionScheduleDetail.Query(Guid.NewGuid()); // Non-existent ID

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains(ResponseMessages.InspectionScheduleNotFound, result.Errors);
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsScheduleDetails()
    {
        // Arrange
        // Create prerequisites
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);

        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            technicianRole,
            "tech@example.com",
            "John Doe",
            "1234567890"
        );

        // Create owner with encrypted phone
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");

        // Setup encryption for owner
        (string key, string iv) = await _keyManagementService.GenerateKeyAsync();
        string encryptedKey = _keyManagementService.EncryptKey(key, _encryptionSettings.Key);

        // Create encryption key record
        var encryptionKey = new EncryptionKey { EncryptedKey = encryptedKey, IV = iv };
        await _dbContext.EncryptionKeys.AddAsync(encryptionKey);
        await _dbContext.SaveChangesAsync();

        string phoneNumber = "0987654321";
        string encryptedPhone = await _aesEncryptionService.Encrypt(phoneNumber, key, iv);

        var owner = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            ownerRole,
            "owner@example.com",
            "Jane Smith",
            encryptedPhone,
            "avatar.jpg"
        );

        // Update owner's encryption key ID
        owner.EncryptionKeyId = encryptionKey.Id;
        await _dbContext.SaveChangesAsync();

        // Create car with amenities
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
            CarStatusEnum.Pending
        );

        // Create amenities and link to car
        var amenity1 = new Amenity
        {
            Name = "Air Conditioning",
            Description = "Good",
            IconUrl = "ac.png",
        };
        var amenity2 = new Amenity
        {
            Name = "Bluetooth",
            Description = "rapidly connect",
            IconUrl = "bluetooth.png",
        };

        await _dbContext.Amenities.AddRangeAsync([amenity1, amenity2]);
        await _dbContext.SaveChangesAsync();

        var carAmenity1 = new CarAmenity { CarId = car.Id, AmenityId = amenity1.Id };
        var carAmenity2 = new CarAmenity { CarId = car.Id, AmenityId = amenity2.Id };

        await _dbContext.CarAmenities.AddRangeAsync([carAmenity1, carAmenity2]);
        await _dbContext.SaveChangesAsync();

        // Create inspection schedule
        var scheduledDate = DateTimeOffset.UtcNow.AddDays(1);
        var schedule = new InspectionSchedule
        {
            TechnicianId = technician.Id,
            CarId = car.Id,
            Status = InspectionScheduleStatusEnum.Pending,
            InspectionAddress = "123 Main St",
            InspectionDate = scheduledDate,
            Note = "Test inspection note",
            CreatedBy = consultant.Id,
        };

        await _dbContext.InspectionSchedules.AddAsync(schedule);
        await _dbContext.SaveChangesAsync();

        var handler = new GetInspectionScheduleDetail.Handler(
            _dbContext,
            _aesEncryptionService,
            _keyManagementService,
            _encryptionSettings
        );
        var query = new GetInspectionScheduleDetail.Query(schedule.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(ResponseMessages.Fetched, result.SuccessMessage);

        var response = result.Value;

        // Verify basic schedule info
        Assert.Equal(schedule.Id, response.Id);
        Assert.Equal(scheduledDate.Date, response.Date.Date);
        Assert.Equal("123 Main St", response.Address);
        Assert.Equal("Test inspection note", response.Notes);

        // Verify technician info
        Assert.Equal(technician.Id, response.Technician.Id);
        Assert.Equal("John Doe", response.Technician.Name);

        // Verify owner info with decrypted phone
        Assert.Equal(owner.Id, response.Owner.Id);
        Assert.Equal("Jane Smith", response.Owner.Name);
        Assert.Equal("avatar.jpg", response.Owner.AvatarUrl);
        Assert.Equal(phoneNumber, response.Owner.Phone); // Should be decrypted

        // Verify car info
        Assert.Equal(car.Id, response.Car.Id);
        Assert.Equal(model.Id, response.Car.ModelId);
        Assert.Equal(model.Name, response.Car.ModelName);
        Assert.Equal("Electric", response.Car.FuelType);
        Assert.Equal("Automatic", response.Car.TransmissionType);

        // Verify amenities
        Assert.Equal(2, response.Car.Amenities.Length);
        Assert.Contains(
            response.Car.Amenities,
            a => a.Name == "Air Conditioning" && a.IconUrl == "ac.png"
        );
        Assert.Contains(
            response.Car.Amenities,
            a => a.Name == "Bluetooth" && a.IconUrl == "bluetooth.png"
        );
    }

    [Fact]
    public async Task FromEntity_CorrectlyDecryptsOwnerPhone()
    {
        // Arrange
        // Create prerequisites
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);

        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(_dbContext, technicianRole);

        // Create owner with specific encrypted phone
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");

        // Setup encryption with known values
        (string key, string iv) = await _keyManagementService.GenerateKeyAsync();
        string encryptedKey = _keyManagementService.EncryptKey(key, _encryptionSettings.Key);

        var encryptionKey = new EncryptionKey { EncryptedKey = encryptedKey, IV = iv };
        await _dbContext.EncryptionKeys.AddAsync(encryptionKey);
        await _dbContext.SaveChangesAsync();

        string originalPhone = "0912345678";
        string encryptedPhone = await _aesEncryptionService.Encrypt(originalPhone, key, iv);

        var owner = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            ownerRole,
            "owner@example.com",
            "Jane Smith",
            encryptedPhone
        );

        owner.EncryptionKeyId = encryptionKey.Id;
        await _dbContext.SaveChangesAsync();

        // Create car
        var car = await CreateTestCar(owner.Id);

        // Create inspection schedule
        var schedule = new InspectionSchedule
        {
            TechnicianId = technician.Id,
            CarId = car.Id,
            Status = InspectionScheduleStatusEnum.Pending,
            InspectionAddress = "123 Main St",
            InspectionDate = DateTimeOffset.UtcNow.AddDays(1),
            CreatedBy = consultant.Id,
        };

        await _dbContext.InspectionSchedules.AddAsync(schedule);
        await _dbContext.SaveChangesAsync();

        // Reload the entity with all necessary includes
        var fullSchedule = await _dbContext
            .InspectionSchedules.AsNoTracking()
            .Include(i => i.Car)
            .ThenInclude(c => c.Owner)
            .ThenInclude(o => o.EncryptionKey)
            .Include(i => i.Car)
            .ThenInclude(c => c.Model)
            .Include(i => i.Car)
            .ThenInclude(c => c.FuelType)
            .Include(i => i.Car)
            .ThenInclude(c => c.TransmissionType)
            .Include(i => i.Car)
            .ThenInclude(c => c.CarAmenities)
            .ThenInclude(ca => ca.Amenity)
            .Include(i => i.Technician)
            .FirstAsync(s => s.Id == schedule.Id);

        // Act
        var response = await GetInspectionScheduleDetail.Response.FromEntity(
            fullSchedule,
            _encryptionSettings.Key,
            _aesEncryptionService,
            _keyManagementService
        );

        // Assert
        Assert.Equal(originalPhone, response.Owner.Phone);
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
