using System.Security.Cryptography;
using System.Text;

namespace GitIssues.Fingerprinting
{
    internal sealed class ErrorFingerprintGenerator : IErrorFingerprintGenerator
    {
        public string Generate(Exception exception)
        {
            var declaringType =
                exception.TargetSite?.DeclaringType?.FullName
                ?? string.Empty;

            var method =
                exception.TargetSite?.Name
                ?? string.Empty;

            var value = string.Join(
                "|",
                exception.GetType().FullName,
                declaringType,
                method,
                exception.Message);

            var bytes = Encoding.UTF8.GetBytes(value);

            var hash = SHA256.HashData(bytes);

            return Convert.ToHexString(hash);
        }
    }
}
