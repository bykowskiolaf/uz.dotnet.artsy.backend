using NUnit.Framework;
using Moq;
using artsy.backend.Data;
using artsy.backend.Dtos.Auth;
using artsy.backend.Exceptions;
using artsy.backend.Models;
using artsy.backend.Services.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace artsy.backend.tests.unit.Service
{
    [TestFixture]
    public class AuthServiceTests
    {
        private ApplicationDbContext _context;
        private AuthService _authService;
        private Mock<IPasswordHasher<User>> _mockPasswordHasher;
        private Mock<IConfiguration> _mockConfiguration;
        private Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private Mock<ILogger<AuthService>> _mockLogger;

        // JWT Configuration values for testing - pozostawiamy, bo setup ich wymaga, ale nie będą używane w testach
        private const string JwtKey = "thisisasecretkeythatissupposedtobelongerthan16characters";
        private const string JwtIssuer = "TestIssuer";
        private const string JwtAudience = "TestAudience";
        private const double JwtDurationInMinutes = 15;
        private const int RefreshTokenTTLDays = 7;
        private const int MaxActiveSessionsPerUser = 2;

        private Guid _testUserId;
        private User _testUser;
        private string _testUserPassword = "Password123!";

        [SetUp]
        public void Setup()
        {
            // Konfiguracja in-memory bazy danych dla każdego testu
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);

            // Seed initial data
            _testUserId = Guid.NewGuid();
            _testUser = new User
            {
                Id = _testUserId,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hashedpassword", // Will be mocked
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Users.Add(_testUser);
            _context.SaveChanges();

            // Mock dependencies
            _mockPasswordHasher = new Mock<IPasswordHasher<User>>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockLogger = new Mock<ILogger<AuthService>>();

            // Configure Mock IPasswordHasher
            _mockPasswordHasher.Setup(p => p.HashPassword(It.IsAny<User>(), _testUserPassword))
                .Returns("hashedpassword"); // Simulate hashing
            _mockPasswordHasher.Setup(p => p.VerifyHashedPassword(It.IsAny<User>(), "hashedpassword", _testUserPassword))
                .Returns(PasswordVerificationResult.Success); // Simulate successful verification
            _mockPasswordHasher.Setup(p => p.VerifyHashedPassword(It.IsAny<User>(), "wronghash", It.IsAny<string>()))
                .Returns(PasswordVerificationResult.Failed); // Simulate failed verification
            _mockPasswordHasher.Setup(p => p.VerifyHashedPassword(It.IsAny<User>(), "hashedpassword", "wrongpassword"))
                .Returns(PasswordVerificationResult.Failed); // Simulate failed verification

            var jwtSection = new Mock<IConfigurationSection>();
            jwtSection.Setup(s => s["Key"]).Returns(JwtKey);
            jwtSection.Setup(s => s["Issuer"]).Returns(JwtIssuer);
            jwtSection.Setup(s => s["Audience"]).Returns(JwtAudience);
            jwtSection.Setup(s => s["DurationInMinutes"]).Returns(JwtDurationInMinutes.ToString());
            jwtSection.Setup(s => s["RefreshTokenTTLDays"]).Returns(RefreshTokenTTLDays.ToString());
            jwtSection.Setup(s => s["MaxActiveSessionsPerUser"]).Returns(MaxActiveSessionsPerUser.ToString());

            _mockConfiguration.Setup(c => c.GetSection("Jwt")).Returns(jwtSection.Object);

            _mockConfiguration.Setup(c => c.GetSection("Jwt:RefreshTokenTTLDays").Value).Returns(RefreshTokenTTLDays.ToString());
            _mockConfiguration.Setup(c => c.GetSection("Jwt:MaxActiveSessionsPerUser").Value).Returns(MaxActiveSessionsPerUser.ToString());
            
            var mockConnection = new Mock<ConnectionInfo>();
            mockConnection.Setup(c => c.RemoteIpAddress).Returns(System.Net.IPAddress.Parse("127.0.0.1"));
            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(c => c.Connection).Returns(mockConnection.Object);
            _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

            // Initialize AuthService with mocked dependencies
            _authService = new AuthService(
                _context,
                _mockPasswordHasher.Object,
                _mockConfiguration.Object,
                _mockHttpContextAccessor.Object,
                _mockLogger.Object
            );
        }

        [TearDown]
        public void Teardown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }


        [Test]
        public async Task RegisterAsync_NewUser_ReturnsUser()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Username = "newUser",
                Email = "new@example.com",
                Password = "NewPassword123!"
            };
            _mockPasswordHasher.Setup(p => p.HashPassword(It.IsAny<User>(), registerDto.Password))
                .Returns("hashednewpassword");

            // Act
            var result = await _authService.RegisterAsync(registerDto);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Username, Is.EqualTo(registerDto.Username));
            Assert.That(result.Email, Is.EqualTo(registerDto.Email));
            Assert.That(result.PasswordHash, Is.EqualTo("hashednewpassword"));

            // Verify it was added to the database
            var userInDb = await _context.Users.FirstOrDefaultAsync(u => u.Username == registerDto.Username);
            Assert.That(userInDb, Is.Not.Null);
            Assert.That(userInDb.Email, Is.EqualTo(registerDto.Email));
        }

        [Test]
        public void RegisterAsync_ExistingUsername_ThrowsConflictException()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Username = _testUser.Username, // Existing username
                Email = "another@example.com",
                Password = "Password123!"
            };

            // Act & Assert
            var ex = Assert.ThrowsAsync<ConflictException>(async () => await _authService.RegisterAsync(registerDto));
            Assert.That(ex.Message, Does.Contain($"Username '{registerDto.Username}' is already taken."));
        }

        [Test]
        public void RegisterAsync_ExistingEmail_ThrowsConflictException()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Username = "uniqueUser",
                Email = _testUser.Email, // Existing email
                Password = "Password123!"
            };

            // Act & Assert
            var ex = Assert.ThrowsAsync<ConflictException>(async () => await _authService.RegisterAsync(registerDto));
            Assert.That(ex.Message, Does.Contain($"Email '{registerDto.Email}' is already registered."));
        }

    }
}