using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using artsy.backend.Data; // Upewnij się, że to jest poprawna ścieżka do Twojego DbContext
using artsy.backend.Models; // Upewnij się, że to jest poprawna ścieżka do Twoich modeli User i RefreshToken
using System;
using System.Linq;
using System.Threading.Tasks;

namespace artsy.backend.tests.unit.Data
{
    [TestFixture]
    public class ApplicationDbContextTests
    {
        private DbContextOptions<ApplicationDbContext> _options;

        [SetUp]
        public void Setup()
        {
            // Konfiguracja in-memory bazy danych.
            // Użycie unikalnej nazwy bazy danych dla każdego testu zapewnia izolację testów.
            _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        // --- Testowanie DbSet'ów i podstawowych operacji CRUD dla User ---

        [Test]
        public async Task UsersDbSet_CanAddAndRetrieveUserWithNewFields()
        {
            using (var context = new ApplicationDbContext(_options))
            {
                // Arrange
                var user = new User
                {
                    Username = "newtestuser",
                    Email = "newtest@example.com",
                    PasswordHash = "newhashedpassword",
                    FullName = "Test User Full",
                    Bio = "This is a test biography."
                };

                // Act
                context.Users.Add(user);
                await context.SaveChangesAsync();

                // Assert
                var retrievedUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "newtestuser");
                Assert.That(retrievedUser, Is.Not.Null);
                Assert.That(retrievedUser.Username, Is.EqualTo("newtestuser"));
                Assert.That(retrievedUser.Email, Is.EqualTo("newtest@example.com"));
                Assert.That(retrievedUser.FullName, Is.EqualTo("Test User Full"));
                Assert.That(retrievedUser.Bio, Is.EqualTo("This is a test biography."));
            }
        }

        [Test]
        public async Task UsersDbSet_CanUpdateUser()
        {
            using (var context = new ApplicationDbContext(_options))
            {
                // Arrange
                var user = new User
                {
                    Username = "updateuser",
                    Email = "update@example.com",
                    PasswordHash = "oldhash",
                    FullName = "Old Name"
                };
                context.Users.Add(user);
                await context.SaveChangesAsync();

                // Act
                user.FullName = "Updated Name";
                user.Bio = "New Bio";
                context.Users.Update(user);
                await context.SaveChangesAsync();

                // Assert
                var updatedUser = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == user.Id);
                Assert.That(updatedUser, Is.Not.Null);
                Assert.That(updatedUser.FullName, Is.EqualTo("Updated Name"));
                Assert.That(updatedUser.Bio, Is.EqualTo("New Bio"));
                Assert.That(updatedUser.UpdatedAt, Is.GreaterThanOrEqualTo(user.CreatedAt)); // UpdatedAt powinno być równe lub większe od CreatedAt
            }
        }


        [Test]
        public async Task RefreshTokensDbSet_CanAddAndRetrieveRefreshTokenWithNewFields()
        {
            using (var context = new ApplicationDbContext(_options))
            {
                // Arrange
                var user = new User
                {
                    Username = "tokenuser",
                    Email = "token@example.com",
                    PasswordHash = "hashforuser"
                };
                context.Users.Add(user);
                await context.SaveChangesAsync();

                var refreshToken = new RefreshToken
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Token = "uniqueRefreshTokenValueNew",
                    Expires = DateTime.UtcNow.AddDays(7),
                    Created = DateTime.UtcNow,
                    CreatedByIp = "192.168.1.1",
                    ReplacedByToken = "someReplacementToken"
                };

                // Act
                context.RefreshTokens.Add(refreshToken);
                await context.SaveChangesAsync();

                // Assert
                var retrievedToken = await context.RefreshTokens
                    .Include(rt => rt.User)
                    .FirstOrDefaultAsync(rt => rt.Token == "uniqueRefreshTokenValueNew");

                Assert.That(retrievedToken, Is.Not.Null);
                Assert.That(retrievedToken.Id, Is.EqualTo(refreshToken.Id));
                Assert.That(retrievedToken.Token, Is.EqualTo("uniqueRefreshTokenValueNew"));
                Assert.That(retrievedToken.Expires.Date, Is.EqualTo(refreshToken.Expires.Date)); // Porównuj daty, aby uniknąć problemów z precyzją milisekund
                Assert.That(retrievedToken.Created.Date, Is.EqualTo(refreshToken.Created.Date));
                Assert.That(retrievedToken.CreatedByIp, Is.EqualTo("192.168.1.1"));
                Assert.That(retrievedToken.ReplacedByToken, Is.EqualTo("someReplacementToken"));
                Assert.That(retrievedToken.User, Is.Not.Null);
                Assert.That(retrievedToken.User.Username, Is.EqualTo("tokenuser"));
            }
        }

        [Test]
        public async Task RefreshTokensDbSet_CanRevokeRefreshToken()
        {
            using (var context = new ApplicationDbContext(_options))
            {
                // Arrange
                var user = new User { Username = "revoker", Email = "revoke@example.com", PasswordHash = "hash" };
                context.Users.Add(user);
                await context.SaveChangesAsync();

                var refreshToken = new RefreshToken
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Token = "tokenToRevoke",
                    Expires = DateTime.UtcNow.AddDays(7),
                    CreatedByIp = "10.0.0.1"
                };
                context.RefreshTokens.Add(refreshToken);
                await context.SaveChangesAsync();

                // Act
                refreshToken.Revoked = DateTime.UtcNow;
                refreshToken.RevokedByIp = "10.0.0.1";
                context.RefreshTokens.Update(refreshToken);
                await context.SaveChangesAsync();

                // Assert
                var revokedToken = await context.RefreshTokens.AsNoTracking().FirstOrDefaultAsync(rt => rt.Id == refreshToken.Id);
                Assert.That(revokedToken, Is.Not.Null);
                Assert.That(revokedToken.Revoked, Is.Not.Null);
                Assert.That(revokedToken.RevokedByIp, Is.EqualTo("10.0.0.1"));
                Assert.That(revokedToken.IsActive, Is.False); // Sprawdź właściwość obliczeniową
            }
        }
    }
}