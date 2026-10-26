using Microsoft.AspNetCore.DataProtection;
using System.Globalization;
using System.Security.Cryptography;

namespace MvcLoveMoney.Services
{
    // Turns database Ids into encrypted, URL-safe strings and back,
    // so URLs show /Transactions/Edit/CfDJ8... instead of /Transactions/Edit/8.
    public interface IIdProtector
    {
        string Protect(int id);

        // null when the string is missing, tampered with, or not ours
        int? Unprotect(string? protectedId);
    }

    public class IdProtector : IIdProtector
    {
        private readonly IDataProtector _protector;

        public IdProtector(IDataProtectionProvider provider)
        {
            _protector = provider.CreateProtector("MvcLoveMoney.EntityIds");
        }

        public string Protect(int id)
        {
            // Output is base64url, safe to put in a URL path
            return _protector.Protect(id.ToString(CultureInfo.InvariantCulture));
        }

        public int? Unprotect(string? protectedId)
        {
            if (string.IsNullOrWhiteSpace(protectedId))
                return null;

            try
            {
                var value = _protector.Unprotect(protectedId);
                return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var id) ? id : null;
            }
            catch (Exception ex) when (ex is CryptographicException or FormatException)
            {
                return null;
            }
        }
    }
}
