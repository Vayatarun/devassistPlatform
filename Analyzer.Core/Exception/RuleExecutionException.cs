namespace Analyzer.Core.Exceptions;

public class RuleExecutionException : Exception
{
    public string RuleId { get; }

    public RuleExecutionException(string ruleId, Exception innerException)
        : base($"Error executing rule {ruleId}", innerException)
    {
        RuleId = ruleId;
    }
}