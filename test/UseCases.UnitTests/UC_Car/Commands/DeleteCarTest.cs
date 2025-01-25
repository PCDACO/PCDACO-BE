using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Persistance.Data;
using Testcontainers.PostgreSql;
using UseCases.DTOs;
using UseCases.UC_Car.Commands;
using UUIDNext;

namespace UseCases.UnitTests.UC_Car.Commands
{
    public class DeleteCarTests : IAsyncLifetime
    {
        private AppDBContext _dbContext;
        private readonly CurrentUser _currentUser;
        private readonly PostgreSqlContainer _postgresContainer;

        public DeleteCarTests()
        {
            _postgresContainer = new PostgreSqlBuilder()
                .WithImage("postgis/postgis:latest")
                .WithCleanUp(true)
                .Build();

            _currentUser = new CurrentUser();
        }

        public async Task InitializeAsync()
        {
            await _postgresContainer.StartAsync();

            var options = new DbContextOptionsBuilder<AppDBContext>()
                .UseNpgsql(_postgresContainer.GetConnectionString(), o => o.UseNetTopologySuite())
                .EnableSensitiveDataLogging()
                .Options;

            _dbContext = new AppDBContext(options);
            await _dbContext.Database.MigrateAsync();
        }

        public async Task DisposeAsync()
        {
            await _postgresContainer.DisposeAsync();
            await _dbContext.DisposeAsync();
        }

        private async Task<User> CreateTestUser(UserRole role)
        {
            var encryptionKey = new EncryptionKey
            {
                Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
                EncryptedKey = "test-user-key",
                IV = "test-user-iv"
            };

            _dbContext.EncryptionKeys.Add(encryptionKey);
            // await _dbContext.SaveChangesAsync(); // Save encryption key first

            var user = new User
            {
                Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
                EncryptionKeyId = encryptionKey.Id,
                Name = "Test User",
                Email = "test@example.com",
                Password = "password",
                Role = role,
                Address = "Test Address",
                DateOfBirth = DateTime.UtcNow.AddYears(-30),
                Phone = "1234567890"
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            return user;
        }

        private async Task<Car> CreateTestCar(Guid ownerId)
        {
            var encryptionKey = new EncryptionKey
            {
                Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
                EncryptedKey = "test-key",
                IV = "test-iv"
            };

            var manufacturer = new Manufacturer
            {
                Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
                Name = "Test Manufacturer"
            };

            var car = new Car
            {
                Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
                OwnerId = ownerId,
                ManufacturerId = manufacturer.Id,
                EncryptionKeyId = encryptionKey.Id,
                EncryptedLicensePlate = "ABC123",
                Color = "Red",
                Seat = 4,
                FuelConsumption = 5.5m,
                PricePerDay = 100m,
                PricePerHour = 10m,
                Location = new Point(0, 0),
                EncryptionKey = encryptionKey
            };

            _dbContext.Manufacturers.Add(manufacturer);
            _dbContext.EncryptionKeys.Add(encryptionKey);
            _dbContext.Cars.Add(car);
            await _dbContext.SaveChangesAsync();

            return car;
        }

        [Fact]
        public async Task Handle_UserIsAdmin_ReturnsForbidden()
        {
            // Arrange
            var user = await CreateTestUser(UserRole.Admin);
            _currentUser.SetUser(user);
            var testCar = await CreateTestCar(user.Id);

            var handler = new DeleteCar.Handler(_dbContext, _currentUser);
            var command = new DeleteCar.Command(testCar.Id);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(ResultStatus.Forbidden, result.Status);
            Assert.Contains("Bạn không có quyền thực hiện chức năng này", result.Errors);
        }

        [Fact]
        public async Task Handle_CarNotFound_ReturnsNotFound()
        {
            // Arrange
            var user = await CreateTestUser(UserRole.Driver);
            _currentUser.SetUser(user);

            var handler = new DeleteCar.Handler(_dbContext, _currentUser);
            var command = new DeleteCar.Command(Uuid.NewDatabaseFriendly(Database.PostgreSql));

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Contains("Không tìm thấy xe cần xóa", result.Errors);
        }

        [Fact]
        public async Task Handle_UserNotOwner_ReturnsForbidden()
        {
            // Arrange
            var owner = await CreateTestUser(UserRole.Owner);
            var requester = await CreateTestUser(UserRole.Driver);
            _currentUser.SetUser(requester);

            var testCar = await CreateTestCar(owner.Id);

            var handler = new DeleteCar.Handler(_dbContext, _currentUser);
            var command = new DeleteCar.Command(testCar.Id);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(ResultStatus.Forbidden, result.Status);
            Assert.Contains("Bạn không có quyền xóa xe này", result.Errors);
        }

        [Fact]
        public async Task Handle_ValidRequest_DeletesCarSuccessfully()
        {
            // Arrange
            var user = await CreateTestUser(UserRole.Owner);
            _currentUser.SetUser(user);
            var testCar = await CreateTestCar(user.Id);

            var handler = new DeleteCar.Handler(_dbContext, _currentUser);
            var command = new DeleteCar.Command(testCar.Id);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(ResultStatus.Ok, result.Status);

            // Verify car soft-delete
            var deletedCar = await _dbContext
                .Cars.IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Id == testCar.Id);

            Assert.NotNull(deletedCar);
            Assert.True(deletedCar!.IsDeleted);
            Assert.NotNull(deletedCar.DeletedAt);

            // Verify related entities soft-delete
            Assert.Empty(
                await _dbContext
                    .ImageCars.IgnoreQueryFilters()
                    .Where(ic => ic.CarId == testCar.Id && !ic.IsDeleted)
                    .ToListAsync()
            );
        }

        [Fact]
        public async Task Handle_CarAlreadyDeleted_ReturnsError()
        {
            // Arrange
            var user = await CreateTestUser(UserRole.Owner);
            _currentUser.SetUser(user);

            // Create and immediately delete a car
            var testCar = await CreateTestCar(user.Id);
            var handler = new DeleteCar.Handler(_dbContext, _currentUser);
            await handler.Handle(new DeleteCar.Command(testCar.Id), CancellationToken.None);

            // Act - Try to delete again
            var result = await handler.Handle(
                new DeleteCar.Command(testCar.Id),
                CancellationToken.None
            );

            // Assert
            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Contains("Không tìm thấy xe cần xóa", result.Errors);
        }
    }
}
