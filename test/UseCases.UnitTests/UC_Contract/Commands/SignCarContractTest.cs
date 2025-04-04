using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using Persistance.Data;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.UC_Contract.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;
using UseCases.Utils;

namespace UseCases.UnitTests.UC_Contract.Commands;

[Collection("Test Collection")]
public class SignCarContractTest(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly CurrentUser _currentUser = fixture.CurrentUser;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;
    private readonly IAesEncryptionService _aesService = fixture.AesEncryptionService;
    private readonly IKeyManagementService _keyService = fixture.KeyManagementService;
    private readonly EncryptionSettings _encryptionSettings = fixture.EncryptionSettings;
    private readonly GeometryFactory _geometryFactory =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_OwnerSigning_UpdatesContractSuccessfully()
    {
        // Arrange
        var (owner, technician, contract, _, car) = await SetupContractScenario();
        _currentUser.SetUser(owner); // Set owner as current user

        var handler = new SignCarContract.Handler(_dbContext, _currentUser);
        var command = new SignCarContract.Command(car.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("Hợp đồng đã được chủ xe ký thành công", result.SuccessMessage);

        // Verify contract was updated
        var updatedContract = await _dbContext.CarContracts.FindAsync(contract.Id);
        Assert.NotNull(updatedContract);
        Assert.Equal(CarContractStatusEnum.OwnerSigned, updatedContract.Status);
        Assert.NotNull(updatedContract.OwnerSignatureDate);
        Assert.Null(updatedContract.TechnicianSignatureDate);
    }

    [Fact]
    public async Task Handle_TechnicianSigning_UpdatesContractSuccessfully()
    {
        // Arrange
        var (owner, technician, contract, _, car) = await SetupContractScenario();
        _currentUser.SetUser(technician); // Set technician as current user

        var handler = new SignCarContract.Handler(_dbContext, _currentUser);
        var command = new SignCarContract.Command(car.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("Hợp đồng đã được kiểm định viên ký thành công", result.SuccessMessage);

        // Verify contract was updated
        var updatedContract = await _dbContext.CarContracts.FindAsync(contract.Id);
        Assert.NotNull(updatedContract);
        Assert.Equal(CarContractStatusEnum.TechnicianSigned, updatedContract.Status);
        Assert.Null(updatedContract.OwnerSignatureDate);
        Assert.NotNull(updatedContract.TechnicianSignatureDate);
    }

    [Fact]
    public async Task Handle_BothPartiesSigning_UpdatesScheduleStatusToSigned()
    {
        // Arrange
        var (owner, technician, contract, schedule, car) = await SetupContractScenario();

        // First, have the technician sign
        _currentUser.SetUser(technician);
        var techHandler = new SignCarContract.Handler(_dbContext, _currentUser);
        await techHandler.Handle(new SignCarContract.Command(car.Id), CancellationToken.None);

        // Then, have the owner sign
        _currentUser.SetUser(owner);
        var ownerHandler = new SignCarContract.Handler(_dbContext, _currentUser);
        var ownerCommand = new SignCarContract.Command(car.Id);

        // Act
        var result = await ownerHandler.Handle(ownerCommand, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);

        // Verify both contract and schedule were updated
        var updatedSchedule = await _dbContext.InspectionSchedules.FindAsync(schedule.Id);
        Assert.NotNull(updatedSchedule);
        Assert.Equal(InspectionScheduleStatusEnum.Signed, updatedSchedule.Status);

        var updatedContract = await _dbContext.CarContracts.FindAsync(contract.Id);
        Assert.NotNull(updatedContract);
        Assert.NotNull(updatedContract.OwnerSignatureDate);
        Assert.NotNull(updatedContract.TechnicianSignatureDate);
    }

    [Fact]
    public async Task Handle_CarNotFound_ReturnsError()
    {
        // Arrange
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await CreateUserWithEncryptedData(ownerRole.Id);
        _currentUser.SetUser(owner);

        var handler = new SignCarContract.Handler(_dbContext, _currentUser);
        var command = new SignCarContract.Command(Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Equal("Xe không tồn tại", result.Errors.First());
    }

    [Fact]
    public async Task Handle_ContractNotFound_ReturnsNotFound()
    {
        // Arrange
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await CreateUserWithEncryptedData(ownerRole.Id);
        _currentUser.SetUser(owner);

        // Create a car but no contract
        var car = await CreateTestCar(owner.Id);

        var handler = new SignCarContract.Handler(_dbContext, _currentUser);
        var command = new SignCarContract.Command(car.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Equal("Không tìm thấy hợp đồng", result.Errors.First());
    }

    [Theory]
    [InlineData(CarContractStatusEnum.Rejected)]
    [InlineData(CarContractStatusEnum.Completed)]
    public async Task Handle_ContractNotValidStatus_ReturnsConflict(
        CarContractStatusEnum carContractStatus
    )
    {
        // Arrange
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await CreateUserWithEncryptedData(ownerRole.Id);
        _currentUser.SetUser(owner);

        var car = await CreateTestCar(owner.Id);

        // Create contract with non-pending status
        var contract = new CarContract { CarId = car.Id, Status = carContractStatus };
        await _dbContext.CarContracts.AddAsync(contract);
        await _dbContext.SaveChangesAsync();

        var handler = new SignCarContract.Handler(_dbContext, _currentUser);
        var command = new SignCarContract.Command(car.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Equal("Hợp đồng không ở trạng thái có thể ký", result.Errors.First());
    }

    [Fact]
    public async Task Handle_ScheduleNotFound_ReturnsNotFound()
    {
        // Arrange
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await CreateUserWithEncryptedData(ownerRole.Id);
        _currentUser.SetUser(owner);

        var car = await CreateTestCar(owner.Id);

        // Create contract without a corresponding inspection schedule
        var contract = new CarContract { CarId = car.Id, Status = CarContractStatusEnum.Pending };
        await _dbContext.CarContracts.AddAsync(contract);
        await _dbContext.SaveChangesAsync();

        var handler = new SignCarContract.Handler(_dbContext, _currentUser);
        var command = new SignCarContract.Command(car.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains("Không tìm thấy lịch kiểm định", result.Errors.First());
    }

    [Fact]
    public async Task Handle_WrongOwner_ReturnsForbidden()
    {
        // Arrange
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var realOwner = await CreateUserWithEncryptedData(ownerRole.Id, "real@owner.com");
        var wrongOwner = await CreateUserWithEncryptedData(ownerRole.Id, "wrong@owner.com");
        _currentUser.SetUser(wrongOwner);

        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await CreateUserWithEncryptedData(technicianRole.Id);

        var (_, _, _, _, car) = await SetupContractScenario(realOwner, technician);

        var handler = new SignCarContract.Handler(_dbContext, _currentUser);
        var command = new SignCarContract.Command(car.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Equal("Bạn không có quyền ký hợp đồng này", result.Errors.First());
    }

    [Fact]
    public async Task Handle_WrongTechnician_ReturnsForbidden()
    {
        // Arrange
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await CreateUserWithEncryptedData(ownerRole.Id);

        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var realTechnician = await CreateUserWithEncryptedData(technicianRole.Id, "real@tech.com");
        var wrongTechnician = await CreateUserWithEncryptedData(
            technicianRole.Id,
            "wrong@tech.com"
        );
        _currentUser.SetUser(wrongTechnician);

        var (_, _, _, _, car) = await SetupContractScenario(owner, realTechnician);

        var handler = new SignCarContract.Handler(_dbContext, _currentUser);
        var command = new SignCarContract.Command(car.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Equal("Bạn không phải là kiểm định viên được chỉ định", result.Errors.First());
    }

    [Fact]
    public async Task Handle_NonOwnerNonTechnicianRole_ReturnsForbidden()
    {
        // Arrange
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var driver = await CreateUserWithEncryptedData(driverRole.Id);
        _currentUser.SetUser(driver);

        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await CreateUserWithEncryptedData(ownerRole.Id);

        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await CreateUserWithEncryptedData(technicianRole.Id);

        var (_, _, _, _, car) = await SetupContractScenario(owner, technician);

        var handler = new SignCarContract.Handler(_dbContext, _currentUser);
        var command = new SignCarContract.Command(car.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Equal("Bạn không có quyền ký hợp đồng này", result.Errors.First());
    }

    #region Helper Methods

    private async Task<(
        User owner,
        User technician,
        CarContract contract,
        InspectionSchedule schedule,
        Car car
    )> SetupContractScenario(User? existingOwner = null, User? existingTechnician = null)
    {
        // Create roles if not provided
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );

        // Create or use provided users
        var owner = existingOwner ?? await CreateUserWithEncryptedData(ownerRole.Id);
        var technician =
            existingTechnician
            ?? await CreateUserWithEncryptedData(technicianRole.Id, "tech@example.com");
        var consultant = await CreateUserWithEncryptedData(
            consultantRole.Id,
            "consultant@example.com"
        );

        // Create car for the owner
        var car = await CreateTestCar(owner.Id);

        // Create inspection schedule
        var schedule = new InspectionSchedule
        {
            CarId = car.Id,
            TechnicianId = technician.Id,
            Status = InspectionScheduleStatusEnum.InProgress,
            InspectionDate = DateTimeOffset.UtcNow,
            InspectionAddress = "Test Address",
            CreatedBy = consultant.Id,
        };
        await _dbContext.InspectionSchedules.AddAsync(schedule);

        // Create contract
        var contract = new CarContract
        {
            CarId = car.Id,
            TechnicianId = technician.Id,
            Status = CarContractStatusEnum.Pending,
            Terms = "Test contract terms",
        };
        await _dbContext.CarContracts.AddAsync(contract);
        await _dbContext.SaveChangesAsync();

        return (owner, technician, contract, schedule, car);
    }

    private async Task<User> CreateUserWithEncryptedData(
        Guid roleId,
        string email = "test@example.com",
        string name = "Test User",
        string phoneNumber = "0123456789",
        string licenseNumber = "123456789012"
    )
    {
        // Generate encryption key and encrypt data
        (string key, string iv) = await _keyService.GenerateKeyAsync();
        string encryptedPhoneNumber = await _aesService.Encrypt(phoneNumber, key, iv);
        string encryptedLicenseNumber = await _aesService.Encrypt(licenseNumber, key, iv);
        string encryptedKey = _keyService.EncryptKey(key, _encryptionSettings.Key);

        // Create encryption key
        var encryptionKey = new EncryptionKey { EncryptedKey = encryptedKey, IV = iv };
        await _dbContext.EncryptionKeys.AddAsync(encryptionKey);
        await _dbContext.SaveChangesAsync();

        // Create user with encrypted data
        var user = new User
        {
            Name = name,
            Email = email,
            Password = "password".HashString(),
            Address = "Test Address",
            DateOfBirth = DateTime.UtcNow.AddYears(-30),
            RoleId = roleId,
            Phone = encryptedPhoneNumber,
            EncryptionKeyId = encryptionKey.Id,
            EncryptedLicenseNumber = encryptedLicenseNumber,
            LicenseExpiryDate = DateTimeOffset.UtcNow.AddYears(1),
            LicenseImageFrontUrl = "front-url",
            LicenseImageBackUrl = "back-url",
            LicenseIsApproved = true,
            LicenseApprovedAt = DateTimeOffset.UtcNow,
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        return user;
    }

    private async Task<Car> CreateTestCar(Guid ownerId, string licensePlate = "ABC-12345")
    {
        // Create prerequisites
        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmission = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Gasoline");

        // Generate encryption key and encrypt license plate
        (string key, string iv) = await _keyService.GenerateKeyAsync();
        string encryptedLicensePlate = await _aesService.Encrypt(licensePlate, key, iv);
        string encryptedKey = _keyService.EncryptKey(key, _encryptionSettings.Key);

        // Create encryption key
        var encryptionKey = new EncryptionKey { EncryptedKey = encryptedKey, IV = iv };
        await _dbContext.EncryptionKeys.AddAsync(encryptionKey);
        await _dbContext.SaveChangesAsync();

        // Create pickup location point
        var pickupLocation = _geometryFactory.CreatePoint(new Coordinate(106.7004238, 10.7756587));
        pickupLocation.SRID = 4326;

        // Create car
        var car = new Car
        {
            OwnerId = ownerId,
            ModelId = model.Id,
            EncryptionKeyId = encryptionKey.Id,
            EncryptedLicensePlate = encryptedLicensePlate,
            FuelTypeId = fuelType.Id,
            TransmissionTypeId = transmission.Id,
            Status = CarStatusEnum.Available,
            Color = "Red",
            Seat = 4,
            Description = "Test car",
            FuelConsumption = 7.5m,
            Price = 100m,
            PickupLocation = pickupLocation,
            PickupAddress = "Test address",
            Terms = "Standard terms",
        };

        await _dbContext.Cars.AddAsync(car);
        await _dbContext.SaveChangesAsync();

        return car;
    }

    #endregion
}
