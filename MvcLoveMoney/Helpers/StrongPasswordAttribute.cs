using System.ComponentModel.DataAnnotations;

namespace MvcLoveMoney.Helpers
{
    // Password rule used everywhere a password is chosen (register, change, set, reset):
    // at least 8 characters, 1 uppercase letter and 1 special character.
    // It is a RegularExpressionAttribute, so jQuery validation also checks it in the browser.
    public class StrongPasswordAttribute : RegularExpressionAttribute
    {
        public const int MinLength = 8;
        public const string Hint = "At least 8 characters, including 1 uppercase letter and 1 special character.";

        public StrongPasswordAttribute()
            : base(@"^(?=.*[A-Z])(?=.*[^A-Za-z0-9]).{8,}$")
        {
            ErrorMessage = "Password must be at least 8 characters and include 1 uppercase letter and 1 special character.";
        }
    }
}
