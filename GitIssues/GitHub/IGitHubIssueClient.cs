using System;
using System.Collections.Generic;
using System.Text;

namespace GitIssues.GitHub
{
    internal interface IGitHubIssueClient
    {
        Task<int?> FindOpenIssueAsync(
            string fingerprint,
            CancellationToken cancellationToken);

        Task<int> CreateIssueAsync(
            string title,
            string body,
            IReadOnlyCollection<string> labels,
            CancellationToken cancellationToken);

        Task AddCommentAsync(
            int issueNumber,
            string body,
            CancellationToken cancellationToken);
    }
}
