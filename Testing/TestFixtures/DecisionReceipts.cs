using Keon.Contracts;
using Keon.Contracts.Decision;
using Keon.Contracts.Identifiers;

namespace Keon.Sdk.Testing.TestFixtures;

/// <summary>
/// Factory methods for creating test DecisionReceipt instances.
/// </summary>
public static class DecisionReceipts
{
    public static DecisionReceipt CreateApproved(
        RequestId requestId,
        string capability,
        string policyVersion = "1.0")
    {
        var receiptId = new ReceiptId(Guid.NewGuid().ToString());
        var traceId = new TraceId(Guid.NewGuid().ToString());

        return new DecisionReceipt(
            ReceiptId: receiptId,
            RequestId: requestId,
            TraceId: traceId,
            ActorId: new ActorId("actor:test"),
            Capability: capability,
            Context: CreateDefaultContext(),
            RequestedAtUtc: DateTimeOffset.UtcNow.AddMilliseconds(-50),
            DecidedAtUtc: DateTimeOffset.UtcNow,
            DurationMs: 50,
            ReceiptVersion: "1.0",
            Outcome: DecisionOutcome.Approved,
            Policy: new PolicyEvaluation(
                PolicyId: new PolicyId("test-policy"),
                Effect: PolicyEffect.Allow,
                PolicyVersion: policyVersion,
                MatchedRules: new[] { "allow-all" },
                DenialReason: null),
            Authority: new AuthorityDecision(
                Granted: true,
                GrantId: new AuthorityGrantId(Guid.NewGuid().ToString()),
                GrantReason: "Test approval",
                ExpiresAtUtc: DateTimeOffset.UtcNow.AddHours(1)),
            Evidence: Array.Empty<Evidence>(),
            Notices: Array.Empty<DecisionNotice>());
    }

    public static DecisionReceipt CreateDenied(
        RequestId requestId,
        string capability,
        string denialReason = "POLICY_DENY")
    {
        var receiptId = new ReceiptId(Guid.NewGuid().ToString());
        var traceId = new TraceId(Guid.NewGuid().ToString());

        return new DecisionReceipt(
            ReceiptId: receiptId,
            RequestId: requestId,
            TraceId: traceId,
            ActorId: new ActorId("actor:test"),
            Capability: capability,
            Context: CreateDefaultContext(),
            RequestedAtUtc: DateTimeOffset.UtcNow.AddMilliseconds(-50),
            DecidedAtUtc: DateTimeOffset.UtcNow,
            DurationMs: 50,
            ReceiptVersion: "1.0",
            Outcome: DecisionOutcome.Denied,
            Policy: new PolicyEvaluation(
                PolicyId: new PolicyId("test-policy"),
                Effect: PolicyEffect.Deny,
                PolicyVersion: "1.0",
                MatchedRules: new[] { "deny-rule" },
                DenialReason: denialReason),
            Authority: new AuthorityDecision(
                Granted: false,
                GrantId: null,
                GrantReason: null,
                ExpiresAtUtc: null),
            Evidence: new[] {
                new Evidence(
                    Kind: EvidenceKind.System,
                    Source: "test-policy-engine",
                    Summary: denialReason,
                    Data: new Dictionary<string, object?>())
            },
            Notices: new[] {
                new DecisionNotice(
                    Code: denialReason,
                    Message: "Access denied by policy",
                    Detail: null)
            });
    }

    public static DecisionReceipt CreateNeedsHumanReview(
        RequestId requestId,
        string capability)
    {
        var receiptId = new ReceiptId(Guid.NewGuid().ToString());
        var traceId = new TraceId(Guid.NewGuid().ToString());

        return new DecisionReceipt(
            ReceiptId: receiptId,
            RequestId: requestId,
            TraceId: traceId,
            ActorId: new ActorId("actor:test"),
            Capability: capability,
            Context: CreateDefaultContext(),
            RequestedAtUtc: DateTimeOffset.UtcNow.AddMilliseconds(-50),
            DecidedAtUtc: DateTimeOffset.UtcNow,
            DurationMs: 50,
            ReceiptVersion: "1.0",
            Outcome: DecisionOutcome.NeedsHumanReview,
            Policy: new PolicyEvaluation(
                PolicyId: new PolicyId("test-policy"),
                Effect: PolicyEffect.RequireHumanApproval,
                PolicyVersion: "1.0",
                MatchedRules: new[] { "require-human" },
                DenialReason: null),
            Authority: new AuthorityDecision(
                Granted: false,
                GrantId: null,
                GrantReason: null,
                ExpiresAtUtc: null),
            Evidence: Array.Empty<Evidence>(),
            Notices: new[] {
                new DecisionNotice(
                    Code: "HUMAN_REQUIRED",
                    Message: "Human review required",
                    Detail: "This operation requires manual approval")
            });
    }

    private static DecisionContext CreateDefaultContext()
    {
        var tenantId = new TenantId("test-tenant");
        return new DecisionContext(
            TenantId: tenantId,
            CorrelationId: CorrelationId.From($"t:{tenantId.Value}|c:{Guid.NewGuid()}"),
            SessionId: null,
            ClientApp: "test-client",
            Environment: "test",
            Tags: Array.Empty<string>(),
            Operation: null);
    }
}
