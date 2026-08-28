using System.Text.Json.Serialization;

namespace GitIssues.GitHub
{
    internal sealed class GitHubIssueRequest
    {
        public required string Title { get; init; }

        public required string Body { get; init; }

        public IReadOnlyCollection<string> Labels { get; init; }
            = [];
    }

    internal sealed class GitHubCommentRequest
    {
        public required string Body { get; init; }
    }

    internal sealed class GitHubIssueResponse
    {
        [JsonPropertyName("number")]
        public int Number { get; init; }
    }

    internal sealed class GitHubIssueSearchResponse
    {
        [JsonPropertyName("items")]
        public List<GitHubIssueResponse> Items { get; init; } = [];
    }
}
