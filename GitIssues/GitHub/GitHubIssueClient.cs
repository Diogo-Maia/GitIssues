using Microsoft.Extensions.Options;
using GitIssues.Configuration;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace GitIssues.GitHub
{
    internal sealed class GitHubIssueClient
    : IGitHubIssueClient
    {
        private readonly HttpClient _httpClient;

        private readonly GitHubErrorReporterOptions _options;

        public GitHubIssueClient(
            HttpClient httpClient,
            IOptions<GitHubErrorReporterOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<int?> FindOpenIssueAsync(
            string fingerprint,
            CancellationToken cancellationToken)
        {
            var shortFingerprint =
                fingerprint[..12];

            var query =
                $"repo:{_options.Owner}/{_options.Repository} " +
                $"is:issue is:open in:title {shortFingerprint}";

            var url =
                $"search/issues?q={Uri.EscapeDataString(query)}";

            using var request =
                CreateRequest(HttpMethod.Get, url);

            using var response =
                await _httpClient.SendAsync(
                    request,
                    cancellationToken);

            response.EnsureSuccessStatusCode();

            var result =
                await response.Content
                    .ReadFromJsonAsync<GitHubIssueSearchResponse>(
                        cancellationToken: cancellationToken);

            return result?
                .Items
                .FirstOrDefault()?
                .Number;
        }

        public async Task<int> CreateIssueAsync(
            string title,
            string body,
            IReadOnlyCollection<string> labels,
            CancellationToken cancellationToken)
        {
            var url =
                $"repos/{_options.Owner}/{_options.Repository}/issues";

            using var request =
                CreateRequest(HttpMethod.Post, url);

            request.Content =
                JsonContent.Create(
                    new GitHubIssueRequest
                    {
                        Title = title,
                        Body = body,
                        Labels = labels
                    });

            using var response =
                await _httpClient.SendAsync(
                    request,
                    cancellationToken);

            response.EnsureSuccessStatusCode();

            var issue =
                await response.Content
                    .ReadFromJsonAsync<GitHubIssueResponse>(
                        cancellationToken: cancellationToken);

            if (issue is null)
            {
                throw new InvalidOperationException(
                    "GitHub did not return an issue.");
            }

            return issue.Number;
        }

        public async Task AddCommentAsync(
            int issueNumber,
            string body,
            CancellationToken cancellationToken)
        {
            var url =
                $"repos/{_options.Owner}/{_options.Repository}" +
                $"/issues/{issueNumber}/comments";

            using var request =
                CreateRequest(HttpMethod.Post, url);

            request.Content =
                JsonContent.Create(
                    new GitHubCommentRequest
                    {
                        Body = body
                    });

            using var response =
                await _httpClient.SendAsync(
                    request,
                    cancellationToken);

            response.EnsureSuccessStatusCode();
        }

        private HttpRequestMessage CreateRequest(
            HttpMethod method,
            string url)
        {
            var request =
                new HttpRequestMessage(method, url);

            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    _options.Token);

            request.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue(
                    "application/vnd.github+json"));

            return request;
        }
    }
}
