using GitIssues.Models;

namespace GitIssues.Service
{
    public interface IGitHubErrorReporter
    {
        Task ReportAsync(
            Exception exception,
            CancellationToken cancellationToken = default);

        Task ReportAsync(
            ErrorReport report,
            CancellationToken cancellationToken = default);
    }
}
