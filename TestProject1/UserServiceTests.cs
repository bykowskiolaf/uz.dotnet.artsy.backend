using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using artsy.backend.Data;
using artsy.backend.Models;
using artsy.backend.Services.User;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System;

namespace TestProject1;

[TestFixture]
public class UserServiceTests
{
    private ApplicationDbContext _context; // Use a real DbContext
    private UserService _userService;
    private List<User> _users;
    private Guid _testUserId;

    [SetUp]
    public void Setup()
    {
        _testUserId = Guid.NewGuid();
        _users = new List<User>
        {
            new User { Id = _testUserId, Username = "testuser", Email = "test@example.com", FullName = "Test User", Bio = "Test bio" }
        };

        // Configure DbContext to use in-memory database:
        var dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;

        _context = new ApplicationDbContext(dbContextOptions); // Create a real DbContext

        // **SEED THE DATABASE:**
        _context.Users.AddRange(_users);
        _context.SaveChanges(); // Save changes to the in-memory database

        _userService = new UserService(_context); // Pass the real context to the service
    }

    [TearDown]
    public void TearDown()
    {
        // Dispose of the context after each test:
        _context.Dispose();
    }

    [Test]
    public async Task GetUserProfileAsync_ReturnsUserProfile_WhenUserExists()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString())
        };
        var claimsIdentity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        // Act
        var result = await _userService.GetUserProfileAsync(claimsPrincipal);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Username, Is.EqualTo("testuser"));  // Add more specific assertions
        Assert.That(result.Email, Is.EqualTo("test@example.com"));
    }
}