using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using artsy.backend.Data;
using artsy.backend.Models;
using artsy.backend.Services.User;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace TestProject1;

[TestFixture]
public class UserServiceTests
{
    private Mock<ApplicationDbContext> _mockContext;
    private Mock<DbSet<User>> _mockUserDbSet;
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

        _mockUserDbSet = MockHelpers.GetMockDbSet(_users);

        _mockContext = new Mock<ApplicationDbContext>();
        // DO NOT mock c => c.Users directly.  Instead, mock the DbSet and the Set<User> method:
        _mockContext.Setup(c => c.Set<User>()).Returns(_mockUserDbSet.Object);

        // Optionally setup FindAsync:
        _mockContext.Setup(c => c.FindAsync<User>(_testUserId)).ReturnsAsync(_users.FirstOrDefault(u => u.Id == _testUserId));

        // Optionally setup SaveChangesAsync (important to prevent actual DB calls):
        _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1); // Or whatever you expect

        _userService = new UserService(_mockContext.Object);
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
    }

    //Helper class for creating the mock DbSet (Taken from various sources)
    public static class MockHelpers
    {
        public static Mock<DbSet<T>> GetMockDbSet<T>(List<T> data) where T : class
        {
            var queryable = data.AsQueryable();
            var mockSet = new Mock<DbSet<T>>();
            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());

            // Add setup for adding and attaching entities, if needed
            mockSet.Setup(d => d.Add(It.IsAny<T>())).Callback<T>(s => data.Add(s));
            mockSet.Setup(d => d.Attach(It.IsAny<T>())).Callback<T>(s => { /* Simulate attaching */ });

            return mockSet;
        }
    }
}