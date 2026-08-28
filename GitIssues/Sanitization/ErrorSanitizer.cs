using System.Text.RegularExpressions;

namespace GitIssues.Sanitization
{
    internal sealed partial class ErrorSanitizer
    : IErrorSanitizer
    {
        public string Sanitize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var result = value;

            result = PasswordRegex()
                .Replace(result, "$1=[REDACTED]");

            result = TokenRegex()
                .Replace(result, "$1=[REDACTED]");

            result = BearerRegex()
                .Replace(result, "Bearer [REDACTED]");

            return result;
        }

        [GeneratedRegex(
            @"(?i)\b(password|pwd)\s*=\s*[^;\s]+")]
        private static partial Regex PasswordRegex();

        [GeneratedRegex(
            @"(?i)\b(token|api[-_]?key|access[-_]?token)\s*=\s*[^;\s]+")]
        private static partial Regex TokenRegex();

        [GeneratedRegex(
            @"(?i)Bearer\s+[A-Za-z0-9\-._~+/]+=*")]
        private static partial Regex BearerRegex();
    }
}
