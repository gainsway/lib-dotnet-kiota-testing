# Gainsway.Kiota.Testing

A testing library that simplifies mocking [Kiota-generated](https://learn.microsoft.com/en-us/openapi/kiota/overview) API clients for unit tests using NSubstitute.
[![NuGet](https://img.shields.io/nuget/v/Gainsway.Kiota.Testing.svg)](https://www.nuget.org/packages/Gainsway.Kiota.Testing/)

> **Upgrading?** The legacy string/URL-based mocking API has been removed. See [MIGRATION.md](MIGRATION.md) if your tests still call `MockClientResponse`, `MockClientCollectionResponse`, `MockClientNoContentResponse`, their exception variants, or `GetUrlTemplate()`, or if you use `Verify*Async` with explicit type arguments.

## 📋 Table of Contents

- [Gainsway.Kiota.Testing](#gainswaykiotatesting)
  - [📋 Table of Contents](#-table-of-contents)
  - [📦 Installation](#-installation)
  - [🚀 Quick Start](#-quick-start)
  - [📖 Usage Guide - Type-Safe Extensions (Recommended)](#-usage-guide---type-safe-extensions-recommended)
    - [1. Basic Setup](#1-basic-setup)
    - [2. Mock GET Requests](#2-mock-get-requests)
      - [GET Single Object (IParsable)](#get-single-object-iparsable)
      - [GET String/Primitive](#get-stringprimitive)
      - [GET Collection](#get-collection)
      - [GET with Conditional Logic](#get-with-conditional-logic)
    - [3. Mock POST Requests](#3-mock-post-requests)
    - [4. Mock PUT Requests](#4-mock-put-requests)
    - [5. Mock PATCH Requests](#5-mock-patch-requests)
    - [6. Mock DELETE Requests](#6-mock-delete-requests)
    - [7. Mock Exception Responses](#7-mock-exception-responses)
      - [GET Exception](#get-exception)
      - [GET Collection Exception](#get-collection-exception)
      - [POST Exception](#post-exception)
      - [PUT/PATCH Exception](#putpatch-exception)
      - [DELETE Exception](#delete-exception)
    - [8. Complex Scenarios](#8-complex-scenarios)
      - [Multiple Mocks for Same Endpoint](#multiple-mocks-for-same-endpoint)
      - [Nested Resource Paths](#nested-resource-paths)
      - [Mocking Null/Empty Responses](#mocking-nullempty-responses)
    - [9. Verify Requests Were Sent](#9-verify-requests-were-sent)
      - [Specifying Call Counts with `Times`](#specifying-call-counts-with-times)
      - [Verifying Each HTTP Verb](#verifying-each-http-verb)
      - [Verification Is Scoped to Path Parameters](#verification-is-scoped-to-path-parameters)
      - [Verifying Additional Request Details](#verifying-additional-request-details)
      - [Explicit Type Arguments](#explicit-type-arguments)
      - [Verifying the Request Body](#verifying-the-request-body)
  - [🧪 Complete Test Example](#-complete-test-example)
  - [� API Reference - Type-Safe Extensions](#-api-reference---type-safe-extensions)
    - [`MockGetAsync<TBuilder, TResponse>()`](#mockgetasynctbuilder-tresponse)
    - [`Verify*` Methods](#verify-methods)
    - [`VerifyCallAssertion.WithBody<TBody>()`](#verifycallassertionwithbodytbody)
    - [`Times`](#times)
    - [`MockGetAsync<TBuilder>(string)`](#mockgetasynctbuilderstring)
    - [`MockGetCollectionAsync<TBuilder, TResponse>()`](#mockgetcollectionasynctbuilder-tresponse)
    - [`MockPostAsync<TBuilder, TResponse>()`](#mockpostasynctbuilder-tresponse)
    - [`MockPutAsync<TBuilder, TResponse>()`](#mockputasynctbuilder-tresponse)
    - [`MockPatchAsync<TBuilder, TResponse>()`](#mockpatchasynctbuilder-tresponse)
    - [`MockDeleteAsync<TBuilder>()`](#mockdeleteasynctbuilder)
    - [`MockGetAsyncException<TBuilder, TResponse>()`](#mockgetasyncexceptiontbuilder-tresponse)
    - [`MockGetCollectionAsyncException<TBuilder, TResponse>()`](#mockgetcollectionasyncexceptiontbuilder-tresponse)
    - [`MockDeleteAsyncException<TBuilder>()`](#mockdeleteasyncexceptiontbuilder)
  - [🔍 Troubleshooting](#-troubleshooting)
    - [Mock Not Matching / Returning Null](#mock-not-matching--returning-null)
    - [Advanced Debugging](#advanced-debugging)
      - [KeyNotFoundException with GetPathParameter](#keynotfoundexception-with-getpathparameter)
      - [Test Fails After Regenerating Kiota Client](#test-fails-after-regenerating-kiota-client)
    - [Finding Parameter Names for Complex Nested Paths](#finding-parameter-names-for-complex-nested-paths)
  - [🔧 Advanced: Manual Mocking Without Extensions](#-advanced-manual-mocking-without-extensions)
    - [Use Case: Accepting Any Path Parameter Value](#use-case-accepting-any-path-parameter-value)
    - [Manual Mocking Pattern](#manual-mocking-pattern)
    - [Examples](#examples)
      - [Mock GET Request Returning Object (Any ID)](#mock-get-request-returning-object-any-id)
      - [Mock GET Request Returning Collection (Any ID)](#mock-get-request-returning-collection-any-id)
      - [Mock POST Request with Body Validation](#mock-post-request-with-body-validation)
      - [Mock DELETE Request (No Return Value)](#mock-delete-request-no-return-value)
      - [Mock Request That Throws Exception](#mock-request-that-throws-exception)
    - [Finding the Correct URL Template](#finding-the-correct-url-template)
    - [When to Use Manual Mocking](#when-to-use-manual-mocking)

## 📦 Installation

```bash
dotnet add package Gainsway.Kiota.Testing
```

## 🚀 Quick Start

```csharp
using Gainsway.Kiota.Testing;

// 1. Create a mockable client
var mockClient = KiotaClientMockExtensions.GetMockableClient<MyKiotaClient>();

// 2. Setup type-safe mock using the generated client structure
//    No URL strings! Just use the client's fluent API
var itemId = "123";
var expectedItem = new MyItem { Id = itemId, Name = "Test Item" };

mockClient.Api.Items[itemId].MockGetAsync(expectedItem);
//         ^^^ Type-safe! Uses your Kiota-generated client structure

// 3. Use in your test
var service = new MyService(mockClient);
var result = await service.GetItemAsync(itemId);

// 4. Assert
Assert.That(result.Name, Is.EqualTo("Test Item"));
```

## 📖 Usage Guide - Type-Safe Extensions (Recommended)

This library provides **type-safe extension methods** that work directly with your Kiota-generated client structure. No URL strings needed!

### 1. Basic Setup

Create a mocked instance of your Kiota-generated client:

```csharp
using Gainsway.Kiota.Testing;

var mockClient = KiotaClientMockExtensions.GetMockableClient<MyKiotaClient>();
```

This creates a client with a mocked `IRequestAdapter` that you can configure for your tests.

---

### 2. Mock GET Requests

#### GET Single Object (IParsable)

```csharp
var fundId = Guid.NewGuid();
var expectedFund = new Fund 
{ 
    Id = fundId, 
    Name = "Test Fund",
    Status = FundStatus.Active
};

// Type-safe! Uses your generated client structure
mockClient.Api.Funds[fundId].MockGetAsync(expectedFund);
```

#### GET String/Primitive

```csharp
// Simple string response
mockClient.Api.Status.MockGetAsync("operational");

// Or with a variable
var status = "maintenance";
mockClient.Api.System.Status.MockGetAsync(status);
```

#### GET Collection

```csharp
var fundId = Guid.NewGuid();
var expectedActivities = new List<Activity>
{
    new Activity { Id = Guid.NewGuid(), Name = "Activity 1" },
    new Activity { Id = Guid.NewGuid(), Name = "Activity 2" }
};

// Mock collection response
mockClient.Api.Funds[fundId].Activities.MockGetCollectionAsync(expectedActivities);
```

#### GET with Conditional Logic

```csharp
var fundId = Guid.NewGuid();

// Only match requests with specific headers
mockClient.Api.Funds[fundId].MockGetAsync(
    expectedFund,
    req => req.Headers.ContainsKey("Authorization")
);

// Multiple conditions
mockClient.Api.Funds[fundId].MockGetAsync(
    expectedFund,
    req => req.Headers.ContainsKey("Authorization")
        && req.QueryParameters.ContainsKey("include")
);
```

---

### 3. Mock POST Requests

```csharp
var createdFund = new Fund 
{ 
    Id = Guid.NewGuid(), 
    Name = "New Fund",
    Status = FundStatus.Active
};

// Mock POST response
mockClient.Api.Funds.MockPostAsync(createdFund);

// With request body validation
mockClient.Api.Funds.MockPostAsync(
    createdFund,
    req => req.Content != null
);

// Mock a POST that returns no content (common for action/status-transition
// endpoints — a bare [HttpPost] returning IActionResult/ActionResult with no body)
mockClient.Api.Funds[fundId].Status.MarkSetupComplete.MockPostAsync();
```

---

### 4. Mock PUT Requests

```csharp
var fundId = Guid.NewGuid();
var updatedFund = new Fund 
{ 
    Id = fundId, 
    Name = "Updated Fund"
};

// Mock PUT response
mockClient.Api.Funds[fundId].MockPutAsync(updatedFund);

// With validation
mockClient.Api.Funds[fundId].MockPutAsync(
    updatedFund,
    req => req.Content != null
        && req.Headers.ContainsKey("If-Match")
);

// Mock a PUT that returns no content (common for a fire-and-forget replication
// write between services, where there's nothing to hand back)
mockClient.ApiInternal.Funds[fundId].MockPutAsync();
```

---

### 5. Mock PATCH Requests

```csharp
var fundId = Guid.NewGuid();
var patchedFund = new Fund 
{ 
    Id = fundId, 
    Status = FundStatus.Closed
};

// Mock PATCH response
mockClient.Api.Funds[fundId].MockPatchAsync(patchedFund);

// Mock a PATCH that returns no content (the common case for partial-update
// endpoints backed by a bare [HttpPatch] returning IActionResult/ActionResult)
mockClient.Api.FundApplications["self"][id].MockPatchAsync();
```

---

### 6. Mock DELETE Requests

```csharp
var fundId = Guid.NewGuid();

// Mock successful DELETE (no content)
mockClient.Api.Funds[fundId].MockDeleteAsync();

// Mock DELETE that returns a single object (some APIs return the deleted object)
var deletedFund = new Fund { Id = fundId, Name = "Deleted Fund", Status = FundStatus.Deleted };
mockClient.Api.Funds[fundId].MockDeleteAsync(deletedFund);

// Mock DELETE that returns a collection (bulk delete operations)
var deletedFunds = new List<Fund>
{
    new Fund { Id = Guid.NewGuid(), Name = "Fund 1", Status = FundStatus.Deleted },
    new Fund { Id = Guid.NewGuid(), Name = "Fund 2", Status = FundStatus.Deleted }
};
mockClient.Api.Funds.MockDeleteCollectionAsync(deletedFunds);

// With conditions (e.g., with request body)
mockClient.Api.Funds[fundId].MockDeleteAsync(
    deletedFund,
    req => req.Content != null && req.Headers.ContainsKey("If-Match")
);
```

---

### 7. Mock Exception Responses

All mock methods support exception overloads - just pass an `Exception` instead of a response object. When using exception overloads, you must provide explicit type parameters since the compiler cannot infer them from an exception.

#### GET Exception

```csharp
var nonExistentId = Guid.NewGuid();

// Mock 404 Not Found - using exception overload
mockClient.Api.Funds[nonExistentId].MockGetAsync<FundItemRequestBuilder, Fund>(
    new ApiException("Fund not found") { ResponseStatusCode = 404 }
);

// Mock 401 Unauthorized with predicate
mockClient.Api.Funds[fundId].MockGetAsync<FundItemRequestBuilder, Fund>(
    new ApiException("Unauthorized") { ResponseStatusCode = 401 },
    req => !req.Headers.ContainsKey("Authorization")
);
```

#### GET Collection Exception

```csharp
// Mock 500 Internal Server Error - using exception overload
mockClient.Api.Activities.MockGetCollectionAsync<ActivitiesRequestBuilder, Activity>(
    new ApiException("Internal server error") { ResponseStatusCode = 500 }
);
```

#### POST Exception

```csharp
// Mock 400 Bad Request on POST
mockClient.Api.Funds.MockPostAsync<FundsRequestBuilder, Fund>(
    new ApiException("Validation failed") { ResponseStatusCode = 400 }
);
```

#### PUT/PATCH Exception

```csharp
// Mock 409 Conflict on PUT
mockClient.Api.Funds[fundId].MockPutAsync<FundItemRequestBuilder, Fund>(
    new ApiException("Version conflict") { ResponseStatusCode = 409 }
);

// Mock 422 Unprocessable Entity on PATCH
mockClient.Api.Funds[fundId].MockPatchAsync<FundItemRequestBuilder, Fund>(
    new ApiException("Invalid field value") { ResponseStatusCode = 422 }
);
```

#### DELETE Exception

```csharp
var conflictingFundId = Guid.NewGuid();

// Mock 409 Conflict on DELETE (no content response)
mockClient.Api.Funds[conflictingFundId].MockDeleteAsync<FundItemRequestBuilder>(
    new ApiException("Conflict - Fund has active transactions") { ResponseStatusCode = 409 }
);

// Mock 409 Conflict on DELETE (with response body)
mockClient.Api.Funds[conflictingFundId].MockDeleteAsync<FundItemRequestBuilder, Fund>(
    new ApiException("Cannot delete fund with active transactions") { ResponseStatusCode = 409 }
);

// Mock exception on bulk DELETE
mockClient.Api.Funds.MockDeleteCollectionAsync<FundsRequestBuilder, Fund>(
    new ApiException("Bulk delete not allowed") { ResponseStatusCode = 403 }
);
```

---

### 8. Complex Scenarios

#### Multiple Mocks for Same Endpoint

```csharp
var fundId1 = Guid.NewGuid();
var fundId2 = Guid.NewGuid();

// Each mock is completely independent
mockClient.Api.Funds[fundId1].MockGetAsync(
    new Fund { Id = fundId1, Name = "Fund 1" }
);

mockClient.Api.Funds[fundId2].MockGetAsync(
    new Fund { Id = fundId2, Name = "Fund 2" }
);
```

#### Nested Resource Paths

```csharp
var fundId = Guid.NewGuid();
var activityId = Guid.NewGuid();

// Mock nested GET
mockClient.Api.Funds[fundId].Activities[activityId].MockGetAsync(expectedActivity);

// Mock nested POST
mockClient.Api.Funds[fundId].Activities.MockPostAsync(createdActivity);

// Mock deeply nested paths
mockClient.Api.Funds[fundId].Activities[activityId].Comments[commentId].MockGetAsync(comment);
```

#### Mocking Null/Empty Responses

```csharp
// Null response
Fund? nullFund = null;
mockClient.Api.Funds[nonExistentId].MockGetAsync(nullFund);

// Empty collection
mockClient.Api.Activities.MockGetCollectionAsync(new List<Activity>());
```

---

### 9. Verify Requests Were Sent

Mocking sets up a response; **verification** asserts your code actually called the endpoint. Use `VerifyGetAsync` with the same type-safe builder syntax — no URL strings, and no response type argument either: verification matches a call regardless of whether the endpoint returns a single object, a collection, a primitive, or no content at all.

```csharp
var fundId = Guid.NewGuid();
mockClient.Api.Funds[fundId].MockGetAsync(expectedFund);

// Act
await service.GetFundAsync(fundId);

// Assert the endpoint was called at least once
await mockClient.Api.Funds[fundId].VerifyGetAsync();
```

> ⚠️ **You must `await` the verification.** Leaving it unawaited means the assertion never runs and the test silently passes.

#### Specifying Call Counts with `Times`

By default — with no argument — verification asserts the endpoint was called **at least once**, matching NSubstitute's bare `Received()`. Use `Times` when you need an exact count:

```csharp
// At least once (the default)
await mockClient.Api.Funds[fundId].VerifyGetAsync();
await mockClient.Api.Funds[fundId].VerifyGetAsync(Times.AtLeastOnce);

// Exactly once
await mockClient.Api.Funds[fundId].VerifyGetAsync(Times.Once);

// Never called
await mockClient.Api.Funds[otherId].VerifyGetAsync(Times.Never);

// An exact number of calls
await mockClient.Api.Funds[fundId].VerifyGetAsync(Times.Exactly(3));
```

| Value | Meaning | NSubstitute equivalent |
|---|---|---|
| *(omitted)* / `Times.AtLeastOnce` | Called one or more times | `Received()` |
| `Times.Once` | Called exactly once | `Received(1)` |
| `Times.Never` | Never called | `DidNotReceive()` |
| `Times.Exactly(n)` | Called exactly `n` times | `Received(n)` |

An `int` converts implicitly, so `VerifyGetAsync(3)` is shorthand for `Times.Exactly(3)`. Prefer the named values for the common cases — they read more clearly at the call site.

#### Verifying Each HTTP Verb

There's exactly one `Verify*Async` method per verb — `VerifyGetAsync`, `VerifyPostAsync`, `VerifyPutAsync`, `VerifyPatchAsync`, `VerifyDeleteAsync` — and each one matches the call no matter what response shape the real endpoint turns out to have:

```csharp
// GET - matches a single object, a collection, or a primitive response
await mockClient.Api.Funds[fundId].VerifyGetAsync(Times.Once);
await mockClient.Api.Status.VerifyGetAsync(Times.Once);
await mockClient.Api.Funds[fundId].Activities.VerifyGetAsync(Times.Once);

// POST - matches a created-resource response or a bare action/status-transition endpoint
await mockClient.Api.Funds.VerifyPostAsync(Times.Once);
await mockClient.Api.Funds[fundId].Status.MarkSetupComplete.VerifyPostAsync(Times.Once);

// PUT / PATCH - matches a returned resource or a no-content replication/partial-update write
await mockClient.Api.Funds[fundId].VerifyPutAsync(Times.Once);
await mockClient.ApiInternal.Funds[fundId].VerifyPutAsync(Times.Once);
await mockClient.Api.FundApplications["self"][id].VerifyPatchAsync(Times.Once);

// DELETE - matches no content, a returned object, or a bulk-delete collection
await mockClient.Api.Funds[fundId].VerifyDeleteAsync(Times.Once);
await mockClient.Api.Funds.VerifyDeleteAsync(Times.Once);
```

Verification matches on HTTP method as well as URL, so a `PATCH` to an endpoint never satisfies a `VerifyPutAsync` on the same URL.

This matters because response shape is a real, common split for every verb — not just an edge case. A fire-and-forget replication `PUT`, an action/status-transition `POST` (`MarkSetupComplete`, `activate`, `suspend`), and a partial-update `PATCH` backed by a bare `ActionResult` all return no content, right alongside endpoints on the same verb that return a resource. `VerifyPutAsync` (etc.) doesn't ask which one your endpoint is — it looks at what call was actually recorded and checks the count against that, whichever `IRequestAdapter` member (`SendAsync<T>`, `SendCollectionAsync<T>`, `SendPrimitiveAsync<T>`, or `SendNoContentAsync`) the generated code happened to invoke.

#### Verification Is Scoped to Path Parameters

Each builder carries its own path parameters, so verification distinguishes between IDs automatically:

```csharp
await service.GetFundAsync(calledId);

await mockClient.Api.Funds[calledId].VerifyGetAsync(Times.Once);
await mockClient.Api.Funds[otherId].VerifyGetAsync(Times.Never);
```

#### Verifying Additional Request Details

Pass a predicate to assert on headers, query parameters, or any other part of the request:

```csharp
await mockClient.Api.Funds[fundId].VerifyGetAsync(
    Times.Once,
    req => req.Headers.ContainsKey("Authorization")
);
```

When the count doesn't match, NSubstitute throws a `ReceivedCallsException` describing the expected and actual calls.

#### Verifying the Request Body

`req.Content != null` (as used in the predicate example above) only proves *a* body was sent — it can't tell you *what* was in it. `VerifyPutAsync`/`VerifyPostAsync`/`VerifyPatchAsync` return a chainable assertion: `.WithBody<TBody>(predicate)` deserializes the actual JSON payload back into your request DTO and adds that as a second condition, alongside the call count already checked:

```csharp
await mockClient.ApiInternal.Funds[fundId]
    .VerifyPutAsync(Times.Once)
    .WithBody<FundUpdateDto>(body => body.RequiresMarket == true);
```

The count check runs first, with the same semantics as awaiting the assertion directly — so `VerifyPutAsync(Times.Never).WithBody(...)` still fails correctly if any call happened at all, and never bothers inspecting a body when none was expected. Because the returned assertion is directly awaitable, plain count checks don't need `.WithBody` at all:

```csharp
// count only — WithBody is opt-in
await mockClient.ApiInternal.Funds[fundId].VerifyPutAsync(Times.Once);
```

> ⚠️ **Requires a real serializer.** `GetMockableClient<T>()` wires a `JsonSerializationWriterFactory` into the mock adapter by default specifically so this works — the generated `PutAsync`/`PostAsync`/`PatchAsync` methods call `RequestInformation.SetContentFromParsable`, which needs a working factory to populate `RequestInformation.Content` at all. If you constructed the mock adapter yourself instead of going through `GetMockableClient<T>()`, `Content` stays `null` and `WithBody` always throws `ReceivedCallsException`.

When nothing matches — no call was made at all, a call was made but the count doesn't match, or the count matches but no matching call's body satisfies the predicate — it throws `ReceivedCallsException`, same as the other `Verify*` methods.

---

## 🧪 Complete Test Example

Here's a full test demonstrating the type-safe extensions:

```csharp
using NUnit.Framework;
using Gainsway.Kiota.Testing;

[TestFixture]
public class FundServiceTests
{
    private MyKiotaClient _mockClient;
    private FundService _service;

    [SetUp]
    public void Setup()
    {
        _mockClient = KiotaClientMockExtensions.GetMockableClient<MyKiotaClient>();
        _service = new FundService(_mockClient);
    }

    [Test]
    public async Task GetFundById_WithValidId_ShouldReturnFund()
    {
        // Arrange
        var fundId = Guid.NewGuid();
        var expectedFund = new Fund 
        { 
            Id = fundId, 
            Name = "Test Fund",
            Status = FundStatus.Active
        };

        // Type-safe mocking!
        _mockClient.Api.Funds[fundId].MockGetAsync(expectedFund);

        // Act
        var result = await _service.GetFundByIdAsync(fundId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(fundId));
        Assert.That(result.Name, Is.EqualTo("Test Fund"));
        Assert.That(result.Status, Is.EqualTo(FundStatus.Active));
    }

    [Test]
    public async Task GetFundActivities_WithValidFundId_ShouldReturnActivities()
    {
        // Arrange
        var fundId = Guid.NewGuid();
        var expectedActivities = new List<Activity>
        {
            new Activity { Id = Guid.NewGuid(), Name = "Activity 1", Amount = 1000.50 },
            new Activity { Id = Guid.NewGuid(), Name = "Activity 2", Amount = 2500.75 }
        };

        _mockClient.Api.Funds[fundId].Activities.MockGetCollectionAsync(expectedActivities);

        // Act
        var result = await _service.GetFundActivitiesAsync(fundId);

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].Name, Is.EqualTo("Activity 1"));
        Assert.That(result[1].Amount, Is.EqualTo(2500.75));
    }

    [Test]
    public async Task DeleteFund_WithValidId_ShouldCompleteSuccessfully()
    {
        // Arrange
        var fundId = Guid.NewGuid();

        _mockClient.Api.Funds[fundId].MockDeleteAsync();

        // Act & Assert - Should not throw
        Assert.DoesNotThrowAsync(async () => 
            await _service.DeleteFundAsync(fundId)
        );
    }

    [Test]
    public async Task GetFund_WhenNotFound_ShouldThrowException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        _mockClient.Api.Funds[nonExistentId]
            .MockGetAsyncException<FundItemRequestBuilder, Fund>(
                new ApiException("Not found") { ResponseStatusCode = 404 }
            );

        // Act & Assert
        Assert.ThrowsAsync<ApiException>(async () =>
            await _service.GetFundByIdAsync(nonExistentId)
        );
    }

    [Test]
    public async Task CreateFund_WithValidData_ShouldReturnCreatedFund()
    {
        // Arrange
        var createdFund = new Fund 
        { 
            Id = Guid.NewGuid(), 
            Name = "New Fund",
            Status = FundStatus.Active
        };

        _mockClient.Api.Funds.MockPostAsync(createdFund);

        // Act
        var result = await _service.CreateFundAsync(new CreateFundRequest 
        { 
            Name = "New Fund" 
        });

        // Assert
        Assert.That(result.Name, Is.EqualTo("New Fund"));
        Assert.That(result.Status, Is.EqualTo(FundStatus.Active));
    }
}
```

---

## � API Reference - Type-Safe Extensions

### `MockGetAsync<TBuilder, TResponse>()`

Mocks a GET request that returns a single object (IParsable).

**Parameters:**
- `response` (TResponse?) - The object to return when this endpoint is called
- `requestInfoPredicate` (optional) - Additional conditions to match the request

**Returns:** The request builder for fluent chaining

**Example:**
```csharp
var fundId = Guid.NewGuid();
var fund = new Fund { Id = fundId, Name = "Test Fund" };

_client.Api.Funds[fundId].MockGetAsync(fund);

// With conditions
_client.Api.Funds[fundId].MockGetAsync(
    fund,
    req => req.Headers.ContainsKey("Authorization")
);
```

---

### `Verify*` Methods

Every verb has exactly one `Verify*Async` method — no response-shape type argument, and no separate name for a no-content or collection variant. It matches the call regardless of which `IRequestAdapter` member the real code happened to invoke.

| Method | Verifies | Matches these `Mock*` shapes |
|---|---|---|
| `VerifyGetAsync<TBuilder>()` | GET | `MockGetAsync` (object), `MockGetAsync(string)` (primitive), `MockGetCollectionAsync` |
| `VerifyPostAsync<TBuilder>()` | POST | `MockPostAsync` (object or no-content), `MockPostCollectionAsync` |
| `VerifyPutAsync<TBuilder>()` | PUT | `MockPutAsync` (object or no-content) |
| `VerifyPatchAsync<TBuilder>()` | PATCH | `MockPatchAsync` (object or no-content) |
| `VerifyDeleteAsync<TBuilder>()` | DELETE | `MockDeleteAsync` (no-content, object, or collection) |

All of them share the same signature:

**Parameters:**
- `times` (Times, optional) - The expected number of calls. Defaults to `Times.AtLeastOnce`. See [Specifying Call Counts with `Times`](#specifying-call-counts-with-times).
- `requestInfoPredicate` (optional) - Additional conditions the request must match

**Returns:** A [`VerifyCallAssertion`](#verifycallassertionwithbodytbody) — directly awaitable for a count-only check, or chain `.WithBody<TBody>(predicate)` (PUT/POST/PATCH) to also assert on the deserialized request body

**Throws:** `ReceivedCallsException` when the actual call count does not match `times`

**Matching:** URL template, path parameter values, and HTTP method must all match the request builder the method is called on. The response shape (object/collection/primitive/no-content) is deliberately *not* part of the match — only `TBuilder` is a type argument, inferred from the call site, never written out.

**Example:**
```csharp
var fundId = Guid.NewGuid();
_client.Api.Funds[fundId].MockGetAsync(fund);

await _service.GetFundAsync(fundId);

// At least once
await _client.Api.Funds[fundId].VerifyGetAsync();

// Exact counts
await _client.Api.Funds[fundId].VerifyGetAsync(Times.Once);
await _client.Api.Funds[fundId].VerifyGetAsync(Times.Exactly(3));

// Never called
await _client.Api.Funds[otherId].VerifyGetAsync(Times.Never);

// With conditions
await _client.Api.Funds[fundId].VerifyGetAsync(
    Times.Once,
    req => req.Headers.ContainsKey("Authorization")
);
```

---

### `VerifyCallAssertion.WithBody<TBody>()`

Chained off `VerifyPutAsync()`, `VerifyPostAsync()`, or `VerifyPatchAsync()`. Additionally asserts that at least one matching call's deserialized body satisfies a predicate, on top of the call-count check those methods already run — see [Verifying the Request Body](#verifying-the-request-body) above. This is the way to check both count and body together, for either a with-response or a no-content endpoint.

**Parameters:**
- `bodyPredicate` (`Func<TBody, bool>`) - A predicate over the deserialized request body; must be satisfied by at least one matching call

**Returns:** A `Task` that must be awaited

**Throws:** `ReceivedCallsException` when the call count doesn't match, or when it matches but no matching call's body satisfies `bodyPredicate`

**Requires:** `TBody` must implement `IParsable` and expose a static `CreateFromDiscriminatorValue(IParseNode)` factory, as every Kiota-generated model does. The mock client must have been created via `GetMockableClient<T>()` so the adapter has a real serializer.

**Example:**
```csharp
await mockClient.ApiInternal.Funds[fundId]
    .VerifyPutAsync(Times.Once)
    .WithBody<FundUpdateDto>(body => body.RequiresMarket == true);
```

---

### `Times`

Specifies the number of calls expected by a `Verify*` assertion.

| Member | Meaning |
|---|---|
| `Times.AtLeastOnce` | Called one or more times (the default) |
| `Times.Once` | Called exactly once |
| `Times.Never` | Never called |
| `Times.Exactly(n)` | Called exactly `n` times; throws `ArgumentOutOfRangeException` if `n` is negative |

An `int` converts implicitly to `Times.Exactly(n)`.

---

### `MockGetAsync<TBuilder>(string)`

Mocks a GET request that returns a string or primitive value.

**Parameters:**
- `response` (string?) - The string to return when this endpoint is called
- `requestInfoPredicate` (optional) - Additional conditions to match the request

**Returns:** The request builder for fluent chaining

**Example:**
```csharp
_client.Api.Status.MockGetAsync("operational");
```

---

### `MockGetCollectionAsync<TBuilder, TResponse>()`

Mocks a GET request that returns a collection of objects.

**Parameters:**
- `response` (IEnumerable<TResponse>?) - The collection to return
- `requestInfoPredicate` (optional) - Additional conditions to match the request

**Returns:** The request builder for fluent chaining

**Example:**
```csharp
var activities = new List<Activity>
{
    new Activity { Id = Guid.NewGuid(), Name = "Activity 1" },
    new Activity { Id = Guid.NewGuid(), Name = "Activity 2" }
};

_client.Api.Funds[fundId].Activities.MockGetCollectionAsync(activities);
```

---

### `MockPostAsync<TBuilder, TResponse>()`

Mocks a POST request that returns a single object.

**Parameters:**
- `response` (TResponse?) - The object to return
- `requestInfoPredicate` (optional) - Additional conditions to match the request

**Returns:** The request builder for fluent chaining

**Example:**
```csharp
var createdFund = new Fund { Id = Guid.NewGuid(), Name = "New Fund" };
_client.Api.Funds.MockPostAsync(createdFund);
```

---

### `MockPostAsync<TBuilder>()`

Mocks a POST request that returns no content. Use for action/status-transition endpoints backed by a bare `[HttpPost]` returning `IActionResult`/`ActionResult` with no body.

**Parameters:**
- `requestInfoPredicate` (optional) - Additional conditions to match the request

**Returns:** The request builder for fluent chaining

**Example:**
```csharp
_client.Api.Funds[fundId].Status.MarkSetupComplete.MockPostAsync();
```

---

### `MockPutAsync<TBuilder, TResponse>()`

Mocks a PUT request that returns a single object.

**Parameters:**
- `response` (TResponse?) - The object to return
- `requestInfoPredicate` (optional) - Additional conditions to match the request

**Returns:** The request builder for fluent chaining

**Example:**
```csharp
var updatedFund = new Fund { Id = fundId, Name = "Updated Fund" };
_client.Api.Funds[fundId].MockPutAsync(updatedFund);
```

---

### `MockPutAsync<TBuilder>()`

Mocks a PUT request that returns no content. Use for a fire-and-forget replication write between services, where the endpoint has nothing to hand back.

**Parameters:**
- `requestInfoPredicate` (optional) - Additional conditions to match the request

**Returns:** The request builder for fluent chaining

**Example:**
```csharp
_client.ApiInternal.Funds[fundId].MockPutAsync();
```

---

### `MockPatchAsync<TBuilder, TResponse>()`

Mocks a PATCH request that returns a single object.

**Parameters:**
- `response` (TResponse?) - The object to return
- `requestInfoPredicate` (optional) - Additional conditions to match the request

**Returns:** The request builder for fluent chaining

**Example:**
```csharp
var patchedFund = new Fund { Id = fundId, Status = FundStatus.Closed };
_client.Api.Funds[fundId].MockPatchAsync(patchedFund);
```

---

### `MockPatchAsync<TBuilder>()`

Mocks a PATCH request that returns no content. This is the common case for partial-update endpoints backed by a bare `[HttpPatch]` returning `IActionResult`/`ActionResult` with no body.

**Parameters:**
- `requestInfoPredicate` (optional) - Additional conditions to match the request

**Returns:** The request builder for fluent chaining

**Example:**
```csharp
_client.Api.FundApplications["self"][id].MockPatchAsync();
```

---

### `MockDeleteAsync<TBuilder>()`

Mocks a DELETE request that returns no content.

**Parameters:**
- `requestInfoPredicate` (optional) - Additional conditions to match the request

**Returns:** The request builder for fluent chaining

**Example:**
```csharp
_client.Api.Funds[fundId].MockDeleteAsync();
```

---

### `MockDeleteAsync<TBuilder, TResponse>()`

Mocks a DELETE request that returns a single object.
Some APIs return data in DELETE responses (e.g., returning the deleted object or confirmation data).

**Parameters:**
- `response` (TResponse?) - The object to return
- `requestInfoPredicate` (optional) - Additional conditions to match the request

**Returns:** The request builder for fluent chaining

**Example:**
```csharp
var deletedFund = new Fund { Id = fundId, Name = "Deleted Fund", Status = FundStatus.Deleted };
_client.Api.Funds[fundId].MockDeleteAsync(deletedFund);

// With request body validation
_client.Api.Funds[fundId].MockDeleteAsync(
    deletedFund,
    req => req.Content != null
);
```

---

### `MockDeleteCollectionAsync<TBuilder, TResponse>()`

Mocks a DELETE request that returns a collection of objects.
Some APIs return multiple items in DELETE responses (e.g., bulk delete operations).

**Parameters:**
- `response` (IEnumerable<TResponse>?) - The collection to return
- `requestInfoPredicate` (optional) - Additional conditions to match the request

**Returns:** The request builder for fluent chaining

**Example:**
```csharp
var deletedFunds = new List<Fund>
{
    new Fund { Id = Guid.NewGuid(), Name = "Fund 1", Status = FundStatus.Deleted },
    new Fund { Id = Guid.NewGuid(), Name = "Fund 2", Status = FundStatus.Deleted }
};

_client.Api.Funds.MockDeleteCollectionAsync(deletedFunds);
```

---

### `MockGetAsyncException<TBuilder, TResponse>()`

**⚠️ DEPRECATED:** Use `MockGetAsync<TBuilder, TResponse>(Exception exception)` overload instead.

Mocks a GET request that throws an exception.

**Parameters:**
- `exception` (Exception) - The exception to throw
- `requestInfoPredicate` (optional) - Additional conditions to match the request

**Returns:** The request builder for fluent chaining

**Deprecated Example:**
```csharp
_client.Api.Funds[nonExistentId]
    .MockGetAsyncException<FundItemRequestBuilder, Fund>(
        new ApiException("Not found") { ResponseStatusCode = 404 }
    );
```

**New Syntax (Recommended):**
```csharp
_client.Api.Funds[nonExistentId]
    .MockGetAsync<FundItemRequestBuilder, Fund>(
        new ApiException("Not found") { ResponseStatusCode = 404 }
    );
```

---

### `MockGetCollectionAsyncException<TBuilder, TResponse>()`

**⚠️ DEPRECATED:** Use `MockGetCollectionAsync<TBuilder, TResponse>(Exception exception)` overload instead.

Mocks a GET collection request that throws an exception.

**Parameters:**
- `exception` (Exception) - The exception to throw
- `requestInfoPredicate` (optional) - Additional conditions to match the request

**Returns:** The request builder for fluent chaining

**Deprecated Example:**
```csharp
_client.Api.Activities
    .MockGetCollectionAsyncException<ActivitiesRequestBuilder, Activity>(
        new ApiException("Internal server error") { ResponseStatusCode = 500 }
    );
```

**New Syntax (Recommended):**
```csharp
_client.Api.Activities
    .MockGetCollectionAsync<ActivitiesRequestBuilder, Activity>(
        new ApiException("Internal server error") { ResponseStatusCode = 500 }
    );
```

---

### `MockDeleteAsyncException<TBuilder>()`

**⚠️ DEPRECATED:** Use `MockDeleteAsync<TBuilder>(Exception exception)` overload instead.

Mocks a DELETE request that throws an exception (no content response type).

**Parameters:**
- `exception` (Exception) - The exception to throw
- `requestInfoPredicate` (optional) - Additional conditions to match the request

**Returns:** The request builder for fluent chaining

**Deprecated Example:**
```csharp
_client.Api.Funds[conflictingFundId]
    .MockDeleteAsyncException<FundItemRequestBuilder>(
        new ApiException("Conflict") { ResponseStatusCode = 409 }
    );
```

**New Syntax (Recommended):**
```csharp
_client.Api.Funds[conflictingFundId]
    .MockDeleteAsync<FundItemRequestBuilder>(
        new ApiException("Conflict") { ResponseStatusCode = 409 }
    );
```

**For DELETE operations that return a response body:**
```csharp
_client.Api.Funds[conflictingFundId]
    .MockDeleteAsync<FundItemRequestBuilder, Fund>(
        new ApiException("Conflict") { ResponseStatusCode = 409 }
    );
```

**For bulk DELETE operations:**
```csharp
_client.Api.Funds
    .MockDeleteCollectionAsync<FundsRequestBuilder, Fund>(
        new ApiException("Bulk delete not allowed") { ResponseStatusCode = 403 }
    );
```

---

## 🔍 Troubleshooting

### Mock Not Matching / Returning Null

**Problem:** Your mock is set up but the service still returns null or throws "not configured".

**Common causes:**

1. **Wrong path parameter value:**
   ```csharp
   // ❌ Mock with different ID than what service uses
   _client.Api.Funds[fundId].MockGetAsync(fund);
   
   // But service calls:
   await _client.Api.Funds[differentFundId].Get.GetAsync();
   ```

2. **Predicate returns false:**
   ```csharp
   _client.Api.Funds[fundId].MockGetAsync(
       fund,
       req => req.Headers.ContainsKey("Authorization")  // ❌ Header missing
   );
   ```

**Solution - Add Debugging:**

```csharp
_client.Api.Funds[fundId].MockGetAsync(
    fund,
    req => {
        Console.WriteLine($"=== Mock Match Attempt ===");
        Console.WriteLine($"URL: {req.UrlTemplate}");
        Console.WriteLine($"Method: {req.HttpMethod}");
        Console.WriteLine($"Headers: {string.Join(", ", req.Headers.Keys)}");
        return true;  // Temporarily return true to see if mock is reached
    }
);
```

### Advanced Debugging

#### KeyNotFoundException with GetPathParameter

**Error:**
```
KeyNotFoundException: The given key 'id' was not present in the dictionary.
Tried: id, id, id, Id
Available keys: baseurl, fund-id
```

**Solution:** Use the parameter name shown in "Available keys":

```csharp
// ❌ Your code tried "id"
req => req.GetPathParameter("id").ToString() == fundId.ToString()

// ✅ Use actual name from error
req => req.GetPathParameter("fund-id").ToString() == fundId.ToString()

// OR use natural naming (recommended) — GetPathParameter tries variations automatically:
_client.Api.Funds[fundId].MockGetAsync(
    fund,
    req => req.GetPathParameter("fundId").ToString() == fundId.ToString()
    //     Automatically tries: fundId, fund-id, fund%2Did, FundId
);
```

#### Test Fails After Regenerating Kiota Client

**Problem:** Tests were passing, but after regenerating your Kiota client, you get compilation errors or a `KeyNotFoundException`.

**Cause:** The API contract changed (parameter renamed, path changed) and Kiota generated new code.

**Why This Is Good:** Your tests caught a breaking change! With the type-safe extensions, a renamed builder or changed generic constraint fails to compile, pointing you straight at what needs updating — you don't need to run the suite to find out.

**Solution:**

1. **Check what changed** in the generated code
2. **Verify with your API team** if this was intentional
3. **Update your tests** to reflect the new contract:
   ```csharp
   // Old (before regeneration)
   req => req.PathParameters["fundId"] == id
   
   // New (after API change)
   req => req.PathParameters["fund-id"] == id
   ```

### Finding Parameter Names for Complex Nested Paths

**Example:** `/api/funds/{fundId}/activities/{activityId}/comments/{commentId}`

**Solution:** Check the deepest request builder:

```csharp
// Look in: CommentItemRequestBuilder.cs
public CommentItemRequestBuilder(...) 
    : base(requestAdapter, 
           "{+baseurl}/api/funds/{fund%2Did}/activities/{activity%2Did}/comments/{comment%2Did}", 
           pathParameters)

// Parameter names are:
// - fund-id
// - activity-id  
// - comment-id

// Use them in your mock:
mockedClient.Api.Funds[fundId].Activities[activityId].Comments[commentId].MockGetAsync(
    comment,
    req => req.GetPathParameter("fund-id").ToString() == fundId.ToString()
        && req.GetPathParameter("activity-id").ToString() == activityId.ToString()
        && req.GetPathParameter("comment-id").ToString() == commentId.ToString()
);
```

---

## 🔧 Advanced: Manual Mocking Without Extensions

In some cases, you may need to mock directly using the adapter when:
- You need to accept **any value** for a path parameter (like `Arg.Any<string>()`)
- An extension method for your specific scenario doesn't exist yet
- You need very specific predicate logic

### Use Case: Accepting Any Path Parameter Value

**Problem:** You have a dynamically generated path parameter (e.g., account seed) that you can't predict in your test:

```csharp
// ❌ This won't work - you don't know the accountSeed value beforehand
var accountSeed = CryptoUtilities.GenerateSeed(/* unpredictable values */);
_client.Api.Accounts[accountSeed].PublicKey.MockGetAsync("mockedKey");
```

**Solution:** Mock at the adapter level to match **any** path parameter value:

```csharp
// ✅ Get the mock adapter
var adapter = _solanaAdapterServiceClient.GetMockAdapter();

// Mock to accept ANY account seed value
adapter
    .SendPrimitiveAsync<string>(
        Arg.Is<RequestInformation>(req =>
            req.HttpMethod == Method.GET
            && req.UrlTemplate == "{+baseurl}/api/accounts/{accountSeed}/public-key"
            // Note: We're NOT checking the accountSeed value - this accepts any value
        ),
        Arg.Any<Dictionary<string, ParsableFactory<IParsable>>>(),
        Arg.Any<CancellationToken>()
    )
    .Returns("mockedSolanaPublicKey");
```

### Manual Mocking Pattern

Use this pattern when you need full control:

```csharp
// 1. Get the mock adapter from your client
var adapter = _yourClient.GetMockAdapter();

// 2. Choose the appropriate Send method based on return type:
//    - SendAsync<T>                 → Single object (IParsable)
//    - SendPrimitiveAsync<T>        → Primitives (string, int, etc.)
//    - SendCollectionAsync<T>       → Collections of IParsable
//    - SendNoContentAsync           → No return value (void/Task)

// 3. Set up the mock with predicates
adapter
    .SendPrimitiveAsync<string>(  // Or SendAsync, SendCollectionAsync, etc.
        Arg.Is<RequestInformation>(req =>
            // Match on HTTP method
            req.HttpMethod == Method.GET
            
            // Match on exact URL template (get from generated code)
            && req.UrlTemplate == "{+baseurl}/api/your/path/{param}"
            
            // Optional: Check specific path parameters if needed
            && req.PathParameters.ContainsKey("param")
            
            // Optional: Add any other conditions
            && req.Headers.ContainsKey("Authorization")
        ),
        Arg.Any<ParsableFactory<YourType>>(),  // Use appropriate factory type
        Arg.Any<Dictionary<string, ParsableFactory<IParsable>>>(),
        Arg.Any<CancellationToken>()
    )
    .Returns(yourMockedValue);  // Or .Throws(exception) for error cases
```

### Examples

#### Mock GET Request Returning Object (Any ID)

```csharp
var adapter = _client.GetMockAdapter();

adapter
    .SendAsync<Fund>(
        Arg.Is<RequestInformation>(req =>
            req.HttpMethod == Method.GET
            && req.UrlTemplate == "{+baseurl}/api/funds/{fundId}"
            // Accepts any fundId value
        ),
        Arg.Any<ParsableFactory<Fund>>(),
        Arg.Any<Dictionary<string, ParsableFactory<IParsable>>>(),
        Arg.Any<CancellationToken>()
    )
    .Returns(expectedFund);
```

#### Mock GET Request Returning Collection (Any ID)

```csharp
var adapter = _client.GetMockAdapter();

adapter
    .SendCollectionAsync<Activity>(
        Arg.Is<RequestInformation>(req =>
            req.HttpMethod == Method.GET
            && req.UrlTemplate == "{+baseurl}/api/funds/{fundId}/activities"
        ),
        Arg.Any<ParsableFactory<Activity>>(),
        Arg.Any<Dictionary<string, ParsableFactory<IParsable>>>(),
        Arg.Any<CancellationToken>()
    )
    .Returns(expectedActivities);
```

#### Mock POST Request with Body Validation

```csharp
var adapter = _client.GetMockAdapter();

adapter
    .SendAsync<Fund>(
        Arg.Is<RequestInformation>(req =>
            req.HttpMethod == Method.POST
            && req.UrlTemplate == "{+baseurl}/api/funds"
            && req.Content != null  // Ensure body is present
        ),
        Arg.Any<ParsableFactory<Fund>>(),
        Arg.Any<Dictionary<string, ParsableFactory<IParsable>>>(),
        Arg.Any<CancellationToken>()
    )
    .Returns(createdFund);
```

#### Mock DELETE Request (No Return Value)

```csharp
var adapter = _client.GetMockAdapter();

adapter
    .SendNoContentAsync(
        Arg.Is<RequestInformation>(req =>
            req.HttpMethod == Method.DELETE
            && req.UrlTemplate == "{+baseurl}/api/funds/{fundId}"
        ),
        Arg.Any<Dictionary<string, ParsableFactory<IParsable>>>(),
        Arg.Any<CancellationToken>()
    )
    .Returns(Task.CompletedTask);
```

#### Mock Request That Throws Exception

```csharp
var adapter = _client.GetMockAdapter();

adapter
    .SendAsync<Fund>(
        Arg.Is<RequestInformation>(req =>
            req.HttpMethod == Method.GET
            && req.UrlTemplate == "{+baseurl}/api/funds/{fundId}"
        ),
        Arg.Any<ParsableFactory<Fund>>(),
        Arg.Any<Dictionary<string, ParsableFactory<IParsable>>>(),
        Arg.Any<CancellationToken>()
    )
    .Throws(new ApiException("Not Found") { ResponseStatusCode = 404 });
```

### Finding the Correct URL Template

To find the exact URL template for manual mocking, check the generated request builder:
```csharp
// In: FundItemRequestBuilder.cs
public FundItemRequestBuilder(...)
    : base(requestAdapter, 
           "{+baseurl}/api/funds/{fund%2Did}",  // ← This is your URL template
           pathParameters)
```

Or read it off an already-constructed `RequestInformation` with `req.NormalizeUrlTemplate()` — it strips `{+baseurl}` and turns path/query parameters into positional names (`{fund%2Did}` becomes `{pathParam1}`), which is what the "Verifying Additional Request Details" predicate examples above use.

### When to Use Manual Mocking

Use manual adapter mocking when:
- ✅ You need `Arg.Any<T>()` behavior for path parameters
- ✅ The extension method for your scenario doesn't exist
- ✅ You need very specific predicate logic (headers, body validation, etc.)
- ✅ You want maximum control over the mock setup

Use extension methods when:
- ✅ You know the exact path parameter values
- ✅ A suitable extension method exists (`MockGetAsync`, `MockPostAsync`, etc.)
- ✅ You want cleaner, more readable test code

---


