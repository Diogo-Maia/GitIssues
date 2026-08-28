namespace GitIssues.Configuration
{
    public sealed class GitHubErrorReporterOptions
    {
        public required string Owner { get; set; }

        public required string Repository { get; set; }

        public required string Token { get; set; }

        public string Environment { get; set; } = "Unknown";

        public string? ServiceName { get; set; }

        public string[] Labels { get; set; } =
        [
            "service-error",
            "automatic"
        ];

        /// <summary>
        /// When enabled, an existing open issue with the same
        /// error fingerprint is reused instead of creating a new issue.
        /// </summary>
        public bool Deduplicate { get; set; } = true;

        /// <summary>
        /// Adds a comment to the existing issue when the
        /// same exception happens again.
        /// </summary>
        public bool CommentOnDuplicate { get; set; } = true;

        /// <summary>
        /// Determines whether stack traces are included
        /// in GitHub issues.
        /// </summary>
        public bool IncludeStackTrace { get; set; } = true;

        /// <summary>
        /// Normally GitHub reporting failures should not crash
        /// the application that is being monitored.
        /// </summary>
        public bool ThrowOnFailure { get; set; } = false;
    }
}
