using Keon.Contracts;
using Keon.Contracts.Execution;
using Keon.Contracts.Identifiers;
using Keon.Contracts.Receipts;

namespace Keon.Sdk.Testing.TestFixtures;

/// <summary>
/// Factory methods for creating test ExecutionResult instances.
/// </summary>
public static class ExecutionResults
{
    public static ExecutionResult CreateCompleted(
        CorrelationId correlationId,
        TenantId? tenantId = null,
        DecisionReceiptId? decisionReceiptId = null)
    {
        tenantId ??= new TenantId("test-tenant");
        decisionReceiptId ??= new DecisionReceiptId(Guid.NewGuid().ToString());

        var executionId = new ExecutionId(Guid.NewGuid().ToString());
        var startedAt = DateTimeOffset.UtcNow.AddMilliseconds(-100);
        var completedAt = DateTimeOffset.UtcNow;

        return new ExecutionResult
        {
            ResultVersion = 1,
            ExecutionId = executionId,
            Link = new ExecutionReceiptLink(
                TenantId: tenantId.Value,
                CorrelationId: correlationId,
                DecisionReceiptId: decisionReceiptId.Value),
            Status = ExecutionStatus.Completed,
            Timing = new ExecutionTiming(
                StartedAt: startedAt,
                CompletedAt: completedAt,
                Duration: completedAt - startedAt),
            Diagnostics = null,
            Output = new Dictionary<string, object?> { ["result"] = "success" },
            Tags = null,
            Failure = null,
            Notices = Array.Empty<ExecutionNotice>(),
            OperationalDiagnostics = Array.Empty<ExecutionDiagnostic>()
        };
    }

    public static ExecutionResult CreateFailed(
        CorrelationId correlationId,
        string failureCode = "TEST_FAILURE",
        string failureMessage = "Test failure",
        TenantId? tenantId = null,
        DecisionReceiptId? decisionReceiptId = null)
    {
        tenantId ??= new TenantId("test-tenant");
        decisionReceiptId ??= new DecisionReceiptId(Guid.NewGuid().ToString());

        var executionId = new ExecutionId(Guid.NewGuid().ToString());
        var startedAt = DateTimeOffset.UtcNow.AddMilliseconds(-100);
        var completedAt = DateTimeOffset.UtcNow;

        return new ExecutionResult
        {
            ResultVersion = 1,
            ExecutionId = executionId,
            Link = new ExecutionReceiptLink(
                TenantId: tenantId.Value,
                CorrelationId: correlationId,
                DecisionReceiptId: decisionReceiptId.Value),
            Status = ExecutionStatus.Failed,
            Timing = new ExecutionTiming(
                StartedAt: startedAt,
                CompletedAt: completedAt,
                Duration: completedAt - startedAt),
            Diagnostics = new ExecutionDiagnostics
            {
                Message = failureMessage,
                FailureCode = ExecutionFailureCode.HandlerError,
                Details = null
            },
            Output = null,
            Tags = null,
            Failure = new ExecutionFailure
            {
                Kind = ExecutionFailureKind.HandlerFaulted,
                Code = failureCode,
                Message = failureMessage,
                Details = null,
                ExceptionType = null,
                ExceptionMessage = null
            },
            Notices = new[] {
                new ExecutionNotice
                {
                    Code = failureCode,
                    Message = failureMessage,
                    Severity = ExecutionNoticeSeverity.Error,
                    Meta = null
                }
            },
            OperationalDiagnostics = Array.Empty<ExecutionDiagnostic>()
        };
    }

    public static ExecutionResult CreateStarted(
        CorrelationId correlationId,
        TenantId? tenantId = null,
        DecisionReceiptId? decisionReceiptId = null)
    {
        tenantId ??= new TenantId("test-tenant");
        decisionReceiptId ??= new DecisionReceiptId(Guid.NewGuid().ToString());

        var executionId = new ExecutionId(Guid.NewGuid().ToString());
        var startedAt = DateTimeOffset.UtcNow;

        return new ExecutionResult
        {
            ResultVersion = 1,
            ExecutionId = executionId,
            Link = new ExecutionReceiptLink(
                TenantId: tenantId.Value,
                CorrelationId: correlationId,
                DecisionReceiptId: decisionReceiptId.Value),
            Status = ExecutionStatus.Started,
            Timing = new ExecutionTiming(
                StartedAt: startedAt,
                CompletedAt: null,
                Duration: null),
            Diagnostics = null,
            Output = null,
            Tags = null,
            Failure = null,
            Notices = Array.Empty<ExecutionNotice>(),
            OperationalDiagnostics = Array.Empty<ExecutionDiagnostic>()
        };
    }
}
