using System.ComponentModel.DataAnnotations;

namespace artsy.backend.Models;

public class RefreshToken
{
	[Key]
	public Guid Id { get; set; } // Or int
	public string Token { get; set; } = string.Empty;
	public DateTime Expires { get; set; }
	public DateTime Created { get; set; } = DateTime.UtcNow;
	public string? CreatedByIp { get; set; }
	public DateTime? Revoked { get; set; }
	public string? RevokedByIp { get; set; }
	public string? ReplacedByToken { get; set; }
	public bool IsActive => Revoked == null && !IsExpired;
	public bool IsExpired => DateTime.UtcNow >= Expires;

	public Guid UserId { get; set; }
	public virtual User User { get; set; } = null!;
}
