namespace GitIssues.Fingerprinting
{
    internal interface IErrorFingerprintGenerator
    {
        string Generate(Exception exception);
    }
}
