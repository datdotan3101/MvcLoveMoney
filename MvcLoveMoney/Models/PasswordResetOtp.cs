namespace MvcLoveMoney.Models
{
    // One "forgot password" request, kept in memory (IMemoryCache), not in the database.
    // It disappears when it expires, when the password is changed, or when the app restarts.
    public class PasswordResetOtp
    {
        // The 6-digit code is never kept in plain text
        public string CodeHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int FailedAttempts { get; set; }

        // Set when the correct code is entered; the token allows the "new password" step
        public DateTime? VerifiedAt { get; set; }
        public string? ResetTokenHash { get; set; }
    }
}
