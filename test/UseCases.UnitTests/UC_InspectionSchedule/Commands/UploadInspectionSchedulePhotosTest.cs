using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Moq;
using Persistance.Data;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.UC_InspectionSchedule.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_InspectionSchedule.Commands;

[Collection("Test Collection")]
public class UploadInspectionSchedulePhotosTest(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly CurrentUser _currentUser = fixture.CurrentUser;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;
    private readonly Mock<ICloudinaryServices> _cloudinaryServices = new();

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    private UploadInspectionSchedulePhotos.Command CreateValidCommand(Guid inspectionScheduleId)
    {
        var photoStream1 = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }); // Valid JPEG signature
        var photoStream2 = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }); // Valid JPEG signature

        return new UploadInspectionSchedulePhotos.Command(
            InspectionScheduleId: inspectionScheduleId,
            PhotoType: InspectionPhotoType.ExteriorCar,
            PhotoFiles: [photoStream1, photoStream2],
            Description: "Test inspection photos"
        );
    }

    [Fact]
    public async Task Handle_UserNotTechnician_ReturnsForbidden()
    {
        // Arrange
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var user = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(user);

        var handler = new UploadInspectionSchedulePhotos.Handler(
            _dbContext,
            _currentUser,
            _cloudinaryServices.Object
        );

        var command = CreateValidCommand(Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains("Bạn không có quyền thực hiện chức năng này", result.Errors);
    }

    [Fact]
    public async Task Handle_ScheduleNotFound_ReturnsNotFound()
    {
        // Arrange
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(_dbContext, technicianRole);
        _currentUser.SetUser(technician);

        var handler = new UploadInspectionSchedulePhotos.Handler(
            _dbContext,
            _currentUser,
            _cloudinaryServices.Object
        );

        var command = CreateValidCommand(Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains("Không tìm thấy thông tin kiểm định xe", result.Errors);
    }

    [Fact]
    public async Task Handle_ScheduleInPendingStatus_ReturnsError()
    {
        // Arrange
        // Create technician user
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(_dbContext, technicianRole);
        _currentUser.SetUser(technician);

        // Create prerequisites for inspection schedule
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);

        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmissionType = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        // Create car
        var car = await TestDataCreateCar.CreateTestCar(
            _dbContext,
            owner.Id,
            model.Id,
            transmissionType,
            fuelType,
            Domain.Enums.CarStatusEnum.Pending
        );

        // Create inspection schedule with Pending status
        var schedule = new InspectionSchedule
        {
            TechnicianId = technician.Id,
            CarId = car.Id,
            Status = InspectionScheduleStatusEnum.Pending,
            InspectionAddress = "123 Test St",
            InspectionDate = DateTimeOffset.UtcNow.AddDays(1),
            CreatedBy = consultant.Id,
        };

        await _dbContext.InspectionSchedules.AddAsync(schedule);
        await _dbContext.SaveChangesAsync();

        var handler = new UploadInspectionSchedulePhotos.Handler(
            _dbContext,
            _currentUser,
            _cloudinaryServices.Object
        );

        var command = CreateValidCommand(schedule.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(
            "Chỉ có thể tải lên ảnh kiểm định sau khi đã bắt đầu kiểm định và trước khi bị quá hạn kiểm định",
            result.Errors
        );
    }

    [Fact]
    public async Task Handle_ScheduleInExpiredStatus_ReturnsError()
    {
        // Arrange
        // Create technician user
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(_dbContext, technicianRole);
        _currentUser.SetUser(technician);

        // Create prerequisites for inspection schedule
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);

        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmissionType = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        // Create car
        var car = await TestDataCreateCar.CreateTestCar(
            _dbContext,
            owner.Id,
            model.Id,
            transmissionType,
            fuelType,
            Domain.Enums.CarStatusEnum.Pending
        );

        // Create inspection schedule with Expired status
        var schedule = new InspectionSchedule
        {
            TechnicianId = technician.Id,
            CarId = car.Id,
            Status = InspectionScheduleStatusEnum.Expired,
            InspectionAddress = "123 Test St",
            InspectionDate = DateTimeOffset.UtcNow.AddDays(-1),
            CreatedBy = consultant.Id,
        };

        await _dbContext.InspectionSchedules.AddAsync(schedule);
        await _dbContext.SaveChangesAsync();

        var handler = new UploadInspectionSchedulePhotos.Handler(
            _dbContext,
            _currentUser,
            _cloudinaryServices.Object
        );

        var command = CreateValidCommand(schedule.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(
            "Chỉ có thể tải lên ảnh kiểm định sau khi đã bắt đầu kiểm định và trước khi bị quá hạn kiểm định",
            result.Errors
        );
    }

    [Fact]
    public async Task Handle_ValidRequest_UploadsPhotosSuccessfully()
    {
        // Arrange
        // Create technician user
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(_dbContext, technicianRole);
        _currentUser.SetUser(technician);

        // Create prerequisites for inspection schedule
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);

        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmissionType = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        // Create car
        var car = await TestDataCreateCar.CreateTestCar(
            _dbContext,
            owner.Id,
            model.Id,
            transmissionType,
            fuelType,
            Domain.Enums.CarStatusEnum.Pending
        );

        // Create inspection schedule
        var schedule = new InspectionSchedule
        {
            TechnicianId = technician.Id,
            CarId = car.Id,
            Status = InspectionScheduleStatusEnum.InProgress,
            InspectionAddress = "123 Test St",
            InspectionDate = DateTimeOffset.UtcNow.AddDays(1),
            CreatedBy = consultant.Id,
        };

        await _dbContext.InspectionSchedules.AddAsync(schedule);
        await _dbContext.SaveChangesAsync();

        // Setup mock Cloudinary service
        _cloudinaryServices
            .Setup(x =>
                x.UploadInspectionSchedulePhotosAsync(
                    It.IsAny<string>(),
                    It.IsAny<Stream>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                (string fileName, Stream content, CancellationToken token) =>
                    $"https://cloudinary.com/{fileName}"
            );

        var handler = new UploadInspectionSchedulePhotos.Handler(
            _dbContext,
            _currentUser,
            _cloudinaryServices.Object
        );

        var command = CreateValidCommand(schedule.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("Tải lên ảnh kiểm định thành công", result.SuccessMessage);

        // Verify images were saved in the database
        var photos = await _dbContext
            .InspectionPhotos.Where(p => p.ScheduleId == schedule.Id)
            .ToListAsync();

        Assert.Equal(2, photos.Count);
        Assert.All(
            photos,
            p =>
            {
                Assert.Equal(schedule.Id, p.ScheduleId);
                Assert.Equal(InspectionPhotoType.ExteriorCar, p.Type);
                Assert.Equal("Test inspection photos", p.Description);
                Assert.StartsWith("https://cloudinary.com/", p.PhotoUrl);
            }
        );

        // Verify the returned response
        Assert.Equal(2, result.Value.Images.Length);
        Assert.All(
            result.Value.Images,
            i =>
            {
                Assert.Equal(schedule.Id, i.InspectionScheduleId);
                Assert.StartsWith("https://cloudinary.com/", i.Url);
            }
        );
    }

    [Fact]
    public void Validator_InvalidRequest_ReturnsValidationErrors()
    {
        // Arrange
        var validator = new UploadInspectionSchedulePhotos.Validator();
        var command = new UploadInspectionSchedulePhotos.Command(
            InspectionScheduleId: Guid.Empty,
            PhotoType: InspectionPhotoType.ExteriorCar,
            PhotoFiles: Array.Empty<Stream>(),
            Description: string.Empty
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "InspectionScheduleId");
        Assert.Contains(result.Errors, e => e.PropertyName == "PhotoFiles");
    }

    [Fact]
    public void Validator_InvalidFileSize_ReturnsValidationError()
    {
        // Arrange
        var validator = new UploadInspectionSchedulePhotos.Validator();

        // Create a stream that exceeds the max file size (10MB)
        var oversizedStream = new Mock<Stream>();
        oversizedStream.Setup(s => s.Length).Returns(11 * 1024 * 1024);

        var command = new UploadInspectionSchedulePhotos.Command(
            InspectionScheduleId: Guid.NewGuid(),
            PhotoType: InspectionPhotoType.ExteriorCar,
            PhotoFiles: [oversizedStream.Object],
            Description: "Test"
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("PhotoFiles"));
        Assert.Contains(
            result.Errors,
            e => e.ErrorMessage.Contains("Kích thước ảnh không được vượt quá")
        );
    }

    [Fact]
    public void Validator_MissingExpiryDateForVehicleInspectionCertificate_ReturnsValidationError()
    {
        // Arrange
        var validator = new UploadInspectionSchedulePhotos.Validator();
        var photoStream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }); // Valid JPEG signature

        var command = new UploadInspectionSchedulePhotos.Command(
            InspectionScheduleId: Guid.NewGuid(),
            PhotoType: InspectionPhotoType.VehicleInspectionCertificate,
            PhotoFiles: [photoStream],
            Description: "Vehicle Inspection Certificate",
            ExpiryDate: null // Missing expiry date
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ExpiryDate");
        Assert.Contains(
            result.Errors,
            e => e.ErrorMessage.Contains("Ngày hết hạn giấy kiểm định không được để trống")
        );
    }

    [Fact]
    public void Validator_PastExpiryDateForVehicleInspectionCertificate_ReturnsValidationError()
    {
        // Arrange
        var validator = new UploadInspectionSchedulePhotos.Validator();
        var photoStream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }); // Valid JPEG signature

        var command = new UploadInspectionSchedulePhotos.Command(
            InspectionScheduleId: Guid.NewGuid(),
            PhotoType: InspectionPhotoType.VehicleInspectionCertificate,
            PhotoFiles: [photoStream],
            Description: "Vehicle Inspection Certificate",
            ExpiryDate: DateTimeOffset.UtcNow.AddDays(-1) // Past date
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ExpiryDate");
        Assert.Contains(
            result.Errors,
            e =>
                e.ErrorMessage.Contains(
                    "Ngày hết hạn giấy kiểm định phải lớn hơn hoặc bằng thời điểm hiện tại"
                )
        );
    }

    [Fact]
    public void Validator_ValidExpiryDateForVehicleInspectionCertificate_Passes()
    {
        // Arrange
        var validator = new UploadInspectionSchedulePhotos.Validator();
        var photoStream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }); // Valid JPEG signature

        var command = new UploadInspectionSchedulePhotos.Command(
            InspectionScheduleId: Guid.NewGuid(),
            PhotoType: InspectionPhotoType.VehicleInspectionCertificate,
            PhotoFiles: [photoStream],
            Description: "Vehicle Inspection Certificate",
            ExpiryDate: DateTimeOffset.UtcNow.AddYears(1) // Valid future date
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validator_NullExpiryDateForNonCertificateType_Passes()
    {
        // Arrange
        var validator = new UploadInspectionSchedulePhotos.Validator();
        var photoStream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }); // Valid JPEG signature

        var command = new UploadInspectionSchedulePhotos.Command(
            InspectionScheduleId: Guid.NewGuid(),
            PhotoType: InspectionPhotoType.ExteriorCar,
            PhotoFiles: [photoStream],
            Description: "Car exterior",
            ExpiryDate: null
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.True(result.IsValid || !result.Errors.Any(e => e.PropertyName == "ExpiryDate"));
    }

    [Fact]
    public async Task Handle_VehicleInspectionCertificate_SetsExpiryDate()
    {
        // Arrange
        // Create technician user
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(_dbContext, technicianRole);
        _currentUser.SetUser(technician);

        // Create prerequisites for inspection schedule
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);

        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmissionType = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        // Create car
        var car = await TestDataCreateCar.CreateTestCar(
            _dbContext,
            owner.Id,
            model.Id,
            transmissionType,
            fuelType,
            Domain.Enums.CarStatusEnum.Pending
        );

        // Create inspection schedule
        var schedule = new InspectionSchedule
        {
            TechnicianId = technician.Id,
            CarId = car.Id,
            Status = InspectionScheduleStatusEnum.InProgress,
            InspectionAddress = "123 Test St",
            InspectionDate = DateTimeOffset.UtcNow.AddDays(1),
            CreatedBy = consultant.Id,
        };

        await _dbContext.InspectionSchedules.AddAsync(schedule);
        await _dbContext.SaveChangesAsync();

        // Setup mock Cloudinary service
        _cloudinaryServices
            .Setup(x =>
                x.UploadInspectionSchedulePhotosAsync(
                    It.IsAny<string>(),
                    It.IsAny<Stream>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                (string fileName, Stream content, CancellationToken token) =>
                    $"https://cloudinary.com/{fileName}"
            );

        var expiryDate = DateTimeOffset.UtcNow.AddYears(1);
        var photoStream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }); // Valid JPEG signature

        var command = new UploadInspectionSchedulePhotos.Command(
            InspectionScheduleId: schedule.Id,
            PhotoType: InspectionPhotoType.VehicleInspectionCertificate,
            PhotoFiles: [photoStream],
            Description: "Vehicle Inspection Certificate",
            ExpiryDate: expiryDate
        );

        var handler = new UploadInspectionSchedulePhotos.Handler(
            _dbContext,
            _currentUser,
            _cloudinaryServices.Object
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);

        // Verify the expiry date was saved in the database
        var photos = await _dbContext
            .InspectionPhotos.Where(p => p.ScheduleId == schedule.Id)
            .ToListAsync();

        Assert.Single(photos);
        Assert.NotNull(photos[0].InspectionCertificateExpiryDate);
        Assert.Equal(expiryDate, photos[0].InspectionCertificateExpiryDate);
    }
}
