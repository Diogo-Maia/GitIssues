namespace GitIssues.Sanitization
{
    internal interface IErrorSanitizer
    {
        string Sanitize(string? value);
    }
}
