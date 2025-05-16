using System.ComponentModel.DataAnnotations;

namespace artsy.backend.Models;

public class User
{
	[Key]
	public Guid Id { get; set; } = Guid.NewGuid();

	[Required]
	[MaxLength(100)]
	public string Username { get; set; } = string.Empty;

	[Required]
	[EmailAddress]
	[MaxLength(255)]
	public string Email { get; set; } = string.Empty;

	[Required]
	public string PasswordHash { get; set; } = string.Empty;

	[MaxLength(200)]
	public string? FullName { get; set; }

	[MaxLength(1000)]
	public string? Bio { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}