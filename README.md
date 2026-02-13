# Keon C# SDK

> *Part of the [Keon Governance Platform](https://github.com/Keon-Systems).*
> *Documentation: [keon-docs](https://github.com/Keon-Systems/keon-docs)*
> *Website: [keon.systems](https://keon.systems)*

---

> **Powered by [OMEGA](https://github.com/omega-brands). Governed by [Keon](https://github.com/Keon-Systems).**

---

# What This Repository Is

This repository contains the public SDK and contracts for **Keon**.

It exists so that:
- interfaces are inspectable
- claims are falsifiable
- verification logic is reviewable
- integrations can be built without trust in a hosted runtime

## What This Repository Is Not

This repository does **not** include:
- hosted services
- control planes
- tenant routing logic
- operational dashboards
- production secrets

Those components live in private systems by design.

## Design Philosophy

Keon is built on the principle that **trust should be proven, not promised**.

Public SDKs are part of that proof:
- deterministic inputs
- explicit contracts
- verifiable outputs

If you can run it, inspect it, and validate it - it's working as intended.

# Keon.SDK — Safe by Default

> **The SDK that guides you to the pit of success.**

Keon.SDK is the **only** recommended way to interact with Keon Runtime. It is designed to make correct usage the default and dangerous patterns impossible.

---

## 🎯 Design Principles

1. **Correct usage is the default** — You can't accidentally do the wrong thing
2. **Receipts flow automatically** — Every decision is tracked
3. **Retries are built-in** — Transient failures are handled transparently
4. **Limits are enforced** — Resource exhaustion is prevented
5. **Validation is mandatory** — Invalid requests cannot be created

---

## 🚀 Quick Start

### Installation

```bash
dotnet add package Keon.Sdk
```

### Basic Usage

```csharp
using Keon.Sdk;
using Keon.Runtime.Sdk;
using Keon.Contracts.Decision;

// 1. Create a gateway (implementation-specific)
IRuntimeGateway gateway = new RuntimeGateway();

// 2. Wrap it in KeonClient (safe by default)
using var client = new KeonClient(gateway);

// 3. Build a request (with validation)
var request = new DecisionRequestBuilder()
    .WithCapability("data.read")
    .WithActorId(new ActorId("user-123"))
    .Build();

// 4. Make the decision (automatic retries + receipt tracking)
var result = await client.DecideAsync(request);

if (result.Success && result.Value?.Outcome == DecisionOutcome.Approved)
{
    Console.WriteLine("✅ Approved!");
}
```

---

## 📚 Core Components

### `KeonClient` — The Safe Entry Point

**Always use `KeonClient` instead of calling `IRuntimeGateway` directly.**

```csharp
using var client = new KeonClient(gateway);

// Automatic retry on transient failures
var decision = await client.DecideAsync(request);

// Receipts are automatically tracked
var history = client.ReceiptHistory;

// Decide + Execute in one call (only if approved)
var execution = await client.DecideAndExecuteAsync(
    decisionRequest,
    receipt => new ExecutionRequestBuilder()
        .WithDecisionReceiptId(new DecisionReceiptId(receipt.ReceiptId.Value))
        .Build());
```

**Why?**
- ✅ Automatic retry with exponential backoff
- ✅ Receipt tracking for audit
- ✅ Request validation
- ✅ Proper resource cleanup

---

### `RetryPolicy` — Immutable and Safe

**Use factory methods only. Custom retry policies are not allowed.**

```csharp
// ✅ GOOD: Use safe presets
var client = new KeonClient(gateway, RetryPolicy.Default());
var client = new KeonClient(gateway, RetryPolicy.Conservative());
var client = new KeonClient(gateway, RetryPolicy.NoRetry());

// ❌ BAD: Cannot create custom policies (constructor is private)
// var policy = new RetryPolicy(maxAttempts: 1000); // ❌ Won't compile
```

**Available Policies:**
- `RetryPolicy.Default()` — 3 attempts, 100ms-10s backoff (recommended)
- `RetryPolicy.Conservative()` — 2 attempts, 200ms-5s backoff
- `RetryPolicy.NoRetry()` — Fail fast, no retries

---

### `Batch` — Enforced Limits

**Batch operations have hard limits to prevent resource exhaustion.**

```csharp
// ✅ GOOD: Within limits
var results = await Batch.ExecuteAsync(
    items: requests.Take(100),
    operation: (req, ct) => client.DecideAsync(req, ct),
    maxConcurrency: 10);

// ❌ BAD: Exceeds limits
var results = await Batch.ExecuteAsync(
    items: requests.Take(2000),  // ❌ Max 1000
    operation: (req, ct) => client.DecideAsync(req, ct),
    maxConcurrency: 100);  // ❌ Max 50
```

**Hard Limits:**
- `MaxConcurrency = 50` — Cannot exceed
- `MaxBatchSize = 1000` — Cannot exceed
- `DefaultConcurrency = 10` — Recommended default

---

### Request Builders — Validation Built-In

**Builders prevent invalid requests from being created.**

```csharp
// ✅ GOOD: Valid request
var request = new DecisionRequestBuilder()
    .WithCapability("data.read")
    .WithActorId(new ActorId("user-123"))
    .Build();

// ❌ BAD: Missing capability
var request = new DecisionRequestBuilder()
    .WithActorId(new ActorId("user-123"))
    .Build();  // ❌ Throws InvalidOperationException
```

**Validation Rules:**
- Capability cannot be null or empty
- RequestId, TenantId, ActorId cannot be null
- ExecutionRequest must have valid DecisionReceiptId
- Target must have valid Kind

---

## ⚠️ Anti-Patterns

### ❌ DON'T: Call IRuntimeGateway Directly

```csharp
// ❌ BAD: No retries, no receipt tracking, no validation
var result = await gateway.DecideAsync(request);
```

```csharp
// ✅ GOOD: Use KeonClient
using var client = new KeonClient(gateway);
var result = await client.DecideAsync(request);
```

---

### ❌ DON'T: Create Custom Retry Policies

```csharp
// ❌ BAD: Constructor is private
// var policy = new RetryPolicy(maxAttempts: 1000);
```

```csharp
// ✅ GOOD: Use safe presets
var policy = RetryPolicy.Default();
```

---

### ❌ DON'T: Exceed Batch Limits

```csharp
// ❌ BAD: Will throw ArgumentException
await Batch.ExecuteAsync(items, operation, maxConcurrency: 100);
```

```csharp
// ✅ GOOD: Stay within limits
await Batch.ExecuteAsync(items, operation, maxConcurrency: 10);
```

---

## 🧪 Testing

Use the provided test fixtures for safe, realistic testing:

```csharp
using Keon.Sdk.Testing;

// In-memory gateway with deterministic behavior
var gateway = new InMemoryRuntimeGateway();
gateway.ConfigureCapability("data.read", PolicyEffect.Allow);

using var client = new KeonClient(gateway);

// Test with builders
var request = DecisionRequestBuilder.ForCapability("data.read");
var result = await client.DecideAsync(request);

Assert.True(result.Success);
Assert.Equal(DecisionOutcome.Approved, result.Value?.Outcome);
```

---

## 📖 Summary

| Component | Purpose | Key Safety Feature |
|-----------|---------|-------------------|
| `KeonClient` | Main entry point | Automatic retries + receipt tracking |
| `RetryPolicy` | Retry configuration | Immutable, preset-only |
| `Batch` | Batch operations | Hard limits enforced |
| `DecisionRequestBuilder` | Build requests | Validation on `.Build()` |
| `InMemoryRuntimeGateway` | Testing | Deterministic, no side effects |

---

**Remember:** If you're not using `KeonClient`, you're doing it wrong.

**Family is forever. This is the way.**
