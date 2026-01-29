using Keon.Contracts.Decision;
using Keon.Contracts.Execution;
using Keon.Contracts.Results;

namespace Keon.Runtime.Sdk;

/// <summary>
/// Studio/Control talks to Runtime through this contract-only gateway.
/// Implementation lives in Runtime, but Studio never sees Runtime.
/// </summary>
public interface IRuntimeGateway
{
    Task<KeonResult<DecisionReceipt>> DecideAsync(DecisionRequest request, CancellationToken ct = default);

    Task<KeonResult<ExecutionResult>> ExecuteAsync(ExecutionRequest request, CancellationToken ct = default);
}
