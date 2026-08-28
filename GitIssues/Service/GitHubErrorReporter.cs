using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using GitIssues.Configuration;
using GitIssues.Fingerprinting;
using GitIssues.GitHub;
using GitIssues.Models;
using GitIssues.Sanitization;
using System.Text;

namespace GitIssues.Service
{
    internal sealed class GitHubErrorReporter
    : IGitHubErrorReporter
    {
        private readonly IGitHubIssueClient _gitHub;

        private readonly IErrorFingerprintGenerator _fingerprintGenerator;

        private readonly IErrorSanitizer _sanitizer;

        private readonly GitHubErrorReporterOptions _options;

        private readonly ILogger<GitHubErrorReporter> _logger;

        public GitHubErrorReporter(
            IGitHubIssueClient gitHub,
            IErrorFingerprintGenerator fingerprintGenerator,
            IErrorSanitizer sanitizer,
            IOptions<GitHubErrorReporterOptions> options,
            ILogger<GitHubErrorReporter> logger)
        {
            _gitHub = gitHub;
            _fingerprintGenerator = fingerprintGenerator;
            _sanitizer = sanitizer;
            _options = options.Value;
            _logger = logger;
        }

        public Task ReportAsync(
            Exception exception,
            CancellationToken cancellationToken = default)
        {
            return ReportAsync(
                new ErrorReport
                {
                    Exception = exception
                },
                cancellationToken);
        }

        public async Task ReportAsync(
            ErrorReport report,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(report);
            ArgumentNullException.ThrowIfNull(report.Exception);

            try
            {
                var fingerprint =
                    _fingerprintGenerator.Generate(
                        report.Exception);

                var existingIssue = _options.Deduplicate
                    ? await _gitHub.FindOpenIssueAsync(
                        fingerprint,
                        cancellationToken)
                    : null;

                if (existingIssue.HasValue)
                {
                    if (_options.CommentOnDuplicate)
                    {
                        await AddDuplicateCommentAsync(
                            existingIssue.Value,
                            report,
                            cancellationToken);
                    }

                    return;
                }

                await CreateIssueAsync(
                    report,
                    fingerprint,
                    cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to report error to GitHub.");

                if (_options.ThrowOnFailure)
                    throw;
            }
        }

        private async Task CreateIssueAsync(
            ErrorReport report,
            string fingerprint,
            CancellationToken cancellationToken)
        {
            var shortFingerprint =
                fingerprint[..12];

            var exceptionType =
                report.Exception.GetType().Name;

            var title =
                report.Title
                ?? $"{exceptionType} in {_options.ServiceName ?? "Application"}";

            title =
                $"[{_options.Environment}] " +
                $"{title} [{shortFingerprint}]";

            var body =
                BuildIssueBody(
                    report,
                    fingerprint);

            await _gitHub.CreateIssueAsync(
                title,
                body,
                _options.Labels,
                cancellationToken);
        }

        private async Task AddDuplicateCommentAsync(
            int issueNumber,
            ErrorReport report,
            CancellationToken cancellationToken)
        {
            var comment = $"""
            ## Error occurred again

            **Time:** {report.OccurredAt:O}

            **Server:** {Environment.MachineName}

            **Service:** {_options.ServiceName ?? "Unknown"}

            **Environment:** {_options.Environment}
            """;

            await _gitHub.AddCommentAsync(
                issueNumber,
                comment,
                cancellationToken);
        }

        private string BuildIssueBody(
            ErrorReport report,
            string fingerprint)
        {
            var exception = report.Exception;

            var builder = new StringBuilder();

            builder.AppendLine("## Automatic Error Report");
            builder.AppendLine();

            builder.AppendLine(
                $"**Service:** {_options.ServiceName ?? "Unknown"}");

            builder.AppendLine(
                $"**Environment:** {_options.Environment}");

            builder.AppendLine(
                $"**Server:** {Environment.MachineName}");

            builder.AppendLine(
                $"**Time:** {report.OccurredAt:O}");

            builder.AppendLine(
                $"**Exception:** {exception.GetType().FullName}");

            builder.AppendLine(
                $"**Fingerprint:** `{fingerprint}`");

            builder.AppendLine();

            builder.AppendLine("## Message");
            builder.AppendLine();

            builder.AppendLine("```text");

            builder.AppendLine(
                _sanitizer.Sanitize(
                    exception.Message));

            builder.AppendLine("```");

            if (report.Metadata.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("## Metadata");
                builder.AppendLine();

                foreach (var item in report.Metadata)
                {
                    builder.AppendLine(
                        $"- **{_sanitizer.Sanitize(item.Key)}:** " +
                        $"{_sanitizer.Sanitize(item.Value)}");
                }
            }

            if (_options.IncludeStackTrace &&
                !string.IsNullOrWhiteSpace(
                    exception.StackTrace))
            {
                builder.AppendLine();
                builder.AppendLine("## Stack Trace");
                builder.AppendLine();

                builder.AppendLine("```text");

                builder.AppendLine(
                    _sanitizer.Sanitize(
                        exception.StackTrace));

                builder.AppendLine("```");
            }

            if (exception.InnerException is not null)
            {
                builder.AppendLine();
                builder.AppendLine("## Inner Exception");
                builder.AppendLine();

                builder.AppendLine("```text");

                builder.AppendLine(
                    _sanitizer.Sanitize(
                        exception.InnerException.ToString()));

                builder.AppendLine("```");
            }

            builder.AppendLine();
            builder.AppendLine("---");
            builder.AppendLine(
                "Generated automatically by GitHubErrorReporter.");

            return builder.ToString();
        }
    }
}
