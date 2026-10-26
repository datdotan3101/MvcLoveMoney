namespace MvcLoveMoney.Configs
{
    // Rules for the "forgot password" OTP
    public class OtpSettings
    {
        public const int CodeLength = 6;
        public const int ExpiryMinutes = 5;          // code is valid for 5 minutes
        public const int MaxFailedAttempts = 5;      // after 5 wrong codes, a new one must be requested
        public const int ResendCooldownSeconds = 60; // wait before sending another code
        public const int ResetWindowMinutes = 10;    // time to choose a new password after verifying
    }
}
