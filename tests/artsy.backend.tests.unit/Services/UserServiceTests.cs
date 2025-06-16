namespace artsy.backend.tests.unit.Service;

using NUnit.Framework;
using artsy.backend.Data;
using artsy.backend.Dtos.Profile;
using artsy.backend.Services.User;
using artsy.backend.Models;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

[TestFixture]
public class UserServiceTests
{
    private ApplicationDbContext _context;
    private UserService _userService;
    private Guid _testUserId;
    private ClaimsPrincipal _testUserPrincipal;

    [SetUp]
    public void Setup()
    {
        // Konfiguracja in-memory bazy danych dla każdego testu
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Użyj unikalnej nazwy dla każdego testu
            .Options;

        _context = new ApplicationDbContext(options);
        _userService = new UserService(_context);

        // Dodaj przykładowego użytkownika do bazy danych przed każdym testem
        _testUserId = Guid.NewGuid();
        var testUser = new User
        {
            Id = _testUserId,
            Username = "testuser",
            Email = "test@example.com",
            FullName = "Test User",
            Bio = "This is a test bio.",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Users.Add(testUser);
        _context.SaveChanges();

        // Stwórz ClaimsPrincipal dla testowego użytkownika
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString()),
            new Claim(ClaimTypes.Name, "testuser")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        _testUserPrincipal = new ClaimsPrincipal(identity);
    }

    [TearDown]
    public void Teardown()
    {
        // Wyczyść in-memory bazę danych po każdym teście
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    // --- Testy dla GetUserProfileAsync ---

    [Test]
    public async Task GetUserProfileAsync_ValidUser_ReturnsUserProfileDto()
    {
        // Act
        var result = await _userService.GetUserProfileAsync(_testUserPrincipal);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.UserId, Is.EqualTo(_testUserId.ToString()));
        Assert.That(result.Username, Is.EqualTo("testuser"));
        Assert.That(result.Email, Is.EqualTo("test@example.com"));
        Assert.That(result.FullName, Is.EqualTo("Test User"));
        Assert.That(result.Bio, Is.EqualTo("This is a test bio."));
    }

    [Test]
    public async Task GetUserProfileAsync_UserNotFound_ReturnsNull()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, nonExistentUserId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var userPrincipal = new ClaimsPrincipal(identity);

        // Act
        var result = await _userService.GetUserProfileAsync(userPrincipal);

        // Assert
        Assert.That(result, Is.Null); // Zmienione z Is.Not.Null na Is.Null
    }

    [Test]
    public async Task GetUserProfileAsync_InvalidClaimPrincipal_ReturnsNull()
    {
        // Arrange
        var userPrincipal = new ClaimsPrincipal(); // Pusty ClaimsPrincipal

        // Act
        var result = await _userService.GetUserProfileAsync(userPrincipal);

        // Assert
        Assert.That(result, Is.Null); // Zmienione z Is.Not.Null na Is.Null
    }

    // --- Testy dla UpdateUserProfileAsync ---

    [Test]
    public async Task UpdateUserProfileAsync_ValidUserAndData_UpdatesProfileAndReturnsTrue()
    {
        // Arrange
        var updateDto = new UpdateProfileDto
        {
            FullName = "Updated Name",
            Bio = "New bio content."
        };

        // Act
        var result = await _userService.UpdateUserProfileAsync(_testUserPrincipal, updateDto);

        // Assert
        Assert.That(result, Is.True); // Zmienione z Assert.IsTrue(result)

        // Sprawdź, czy dane zostały rzeczywiście zaktualizowane w bazie danych
        var updatedUser = await _context.Users.FindAsync(_testUserId);
        Assert.That(updatedUser, Is.Not.Null);
        Assert.That(updatedUser.FullName, Is.EqualTo("Updated Name"));
        Assert.That(updatedUser.Bio, Is.EqualTo("New bio content."));
        Assert.That(updatedUser.UpdatedAt, Is.GreaterThan(_context.Users.Local.First().CreatedAt)); // Sprawdź, czy UpdatedAt się zmieniło
    }

    [Test]
    public async Task UpdateUserProfileAsync_PartialUpdateFullName_UpdatesFullNameAndReturnsTrue()
    {
        // Arrange
        var updateDto = new UpdateProfileDto
        {
            FullName = "Only FullName Changed",
            Bio = null // Nie zmieniamy bio
        };

        // Act
        var result = await _userService.UpdateUserProfileAsync(_testUserPrincipal, updateDto);

        // Assert
        Assert.That(result, Is.True); // Zmienione z Assert.IsTrue(result)

        // Sprawdź, czy tylko FullName został zaktualizowany
        var updatedUser = await _context.Users.FindAsync(_testUserId);
        Assert.That(updatedUser, Is.Not.Null);
        Assert.That(updatedUser.FullName, Is.EqualTo("Only FullName Changed"));
        Assert.That(updatedUser.Bio, Is.EqualTo("This is a test bio.")); // Bio powinno pozostać niezmienione
    }

    [Test]
    public async Task UpdateUserProfileAsync_PartialUpdateBio_UpdatesBioAndReturnsTrue()
    {
        // Arrange
        var updateDto = new UpdateProfileDto
        {
            FullName = null, // Nie zmieniamy FullName
            Bio = "Only Bio Changed"
        };

        // Act
        var result = await _userService.UpdateUserProfileAsync(_testUserPrincipal, updateDto);

        // Assert
        Assert.That(result, Is.True); // Zmienione z Assert.IsTrue(result)

        // Sprawdź, czy tylko Bio zostało zaktualizowane
        var updatedUser = await _context.Users.FindAsync(_testUserId);
        Assert.That(updatedUser, Is.Not.Null);
        Assert.That(updatedUser.FullName, Is.EqualTo("Test User")); // FullName powinno pozostać niezmienione
        Assert.That(updatedUser.Bio, Is.EqualTo("Only Bio Changed"));
    }

     [Test]
    public async Task UpdateUserProfileAsync_NoChanges_ReturnsTrue()
    {
        // Arrange
        var updateDto = new UpdateProfileDto
        {
            FullName = "Test User", // Takie same jak istniejące
            Bio = "This is a test bio." // Takie same jak istniejące
        };

        // Act
        var result = await _userService.UpdateUserProfileAsync(_testUserPrincipal, updateDto);

        // Assert
        Assert.That(result, Is.True); // Zmienione z Assert.IsTrue(result)

        // Sprawdź, czy UpdatedAt Zostało zaktualizowane (nawet jeśli FullName/Bio nie)
        var updatedUser = await _context.Users.FindAsync(_testUserId);
        Assert.That(updatedUser, Is.Not.Null);
        Assert.That(updatedUser.UpdatedAt, Is.GreaterThanOrEqualTo(_context.Users.Local.First().CreatedAt)); // Powinno być zaktualizowane
    }


    [Test]
    public async Task UpdateUserProfileAsync_UserNotFound_ReturnsFalse()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, nonExistentUserId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var userPrincipal = new ClaimsPrincipal(identity);

        var updateDto = new UpdateProfileDto
        {
            FullName = "Non Existent",
            Bio = "User"
        };

        // Act
        var result = await _userService.UpdateUserProfileAsync(userPrincipal, updateDto);

        // Assert
        Assert.That(result, Is.False); // Zmienione z Assert.IsFalse(result)
    }

    [Test]
    public async Task UpdateUserProfileAsync_InvalidClaimPrincipal_ReturnsFalse()
    {
        // Arrange
        var userPrincipal = new ClaimsPrincipal(); // Pusty ClaimsPrincipal
        var updateDto = new UpdateProfileDto
        {
            FullName = "Invalid",
            Bio = "Claims"
        };

        // Act
        var result = await _userService.UpdateUserProfileAsync(userPrincipal, updateDto);

        // Assert
        Assert.That(result, Is.False); // Zmienione z Assert.IsFalse(result)
    }
}