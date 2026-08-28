namespace GitIssues.Models
{
    public sealed class ErrorReport
    {
        public required Exception Exception { get; init; }

        public string? Title { get; init; }

        public DateTimeOffset OccurredAt { get; init; }
            = DateTimeOffset.UtcNow;

        public IDictionary<string, string?> Metadata { get; init; }
            = new Dictionary<string, string?>();
    }
}
