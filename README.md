# Gainsway.Kiota.Testing

A testing library that simplifies mocking [Kiota-generated](https://learn.microsoft.com/en-us/openapi/kiota/overview) API clients for unit tests using NSubstitute.
[![NuGet](https://img.shields.io/nuget/v/Gainsway.Kiota.Testing.svg)](https://www.nuget.org/packages/Gainsway.Kiota.Testing/)

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
  - [🧪 Complete Test Example](#-complete-test-example)
  - [� API Reference - Type-Safe Extensions](#-api-reference---type-safe-extensions)
    - [`MockGetAsync<TBuilder, TResponse>()`](#mockgetasynctbuilder-tresponse)
    - [`MockGetAsync<TBuilder>(string)`](#mockgetasynctbuilderstring)
    - [`MockGetCollectionAsync<TBuilder, TResponse>()`](#mockgetcollectionasynctbuilder-tresponse)
    - [`MockPostAsync<TBuilder, TResponse>()`](#mockpostasynctbuilder-tresponse)
    - [`MockPutAsync<TBuilder, TResponse>()`](#mockputasynctbuilder-tresponse)
    - [`MockPatchAsync<TBuilder, TResponse>()`](#mockpatchasynctbuilder-tresponse)
    - [`MockDeleteAsync<TBuilder>()`](#mockdeleteasynctbuilder)
    - [`MockGetAsyncException<TBuilder, TResponse>()`](#mockgetasyncexceptiontbuilder-tresponse)
    - [`MockGetCollectionAsyncException<TBuilder, TResponse>()`](#mockgetcollectionasyncexceptiontbuilder-tresponse)
    - [`MockDeleteAsyncException<TBuilder>()`](#mockdeleteasyncexceptiontbuilder)
  - [� Legacy API Reference - URL-Based Mocking](#-legacy-api-reference---url-based-mocking)
    - [Overview](#overview)
    - [`GetMockableClient<T>()`](#getmockableclientt)
    - [`MockClientResponse<T, R>()`](#mockclientresponset-r)
    - [`MockClientCollectionResponse<T, R>()`](#mockclientcollectionresponset-r)
    - [`MockClientNoContentResponse<T>()`](#mockclientnocontentresponset)
    - [`MockClientResponseException<T, R>()`](#mockclientresponseexceptiont-r)
    - [`MockClientCollectionResponseException<T, R>()`](#mockclientcollectionresponseexceptiont-r)
    - [`MockClientNoContentResponseException<T>()`](#mockclientnocontentresponseexceptiont)
    - [Legacy API Usage Notes](#legacy-api-usage-notes)
      - [URL Pattern Matching](#url-pattern-matching)
      - [Smart Parameter Access](#smart-parameter-access)
      - [Query Parameters](#query-parameters)
      - [Multiple Path Parameters](#multiple-path-parameters)
  - [🔍 Troubleshooting](#-troubleshooting)
    - [Mock Not Matching / Returning Null](#mock-not-matching--returning-null)
    - [Advanced Debugging](#advanced-debugging)
      - [Check URL Template (Legacy API)](#check-url-template-legacy-api)
      - [KeyNotFoundException with GetPathParameter](#keynotfoundexception-with-getpathparameter)
      - [Test Fails After Regenerating Kiota Client](#test-fails-after-regenerating-kiota-client)
    - [Finding Parameter Names for Complex Nested Paths](#finding-parameter-names-for-complex-nested-paths)
    - [Using GetUrlTemplate() Helper](#using-geturltemplate-helper)
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

// Multiple conditions with query parameters (type-safe helpers)
mockClient.Api.Funds[fundId].MockGetAsync(
    expectedFund,
    req => req.Headers.ContainsKey("Authorization")
        && req.GetQueryParameter("include").ToString() == "activities"
);

// Check if optional query parameter exists
mockClient.Api.Funds.MockGetCollectionAsync(
    funds,
    req => {
        // Required: must have $select parameter
        if (!req.TryGetQueryParameter("select", out var selectValue))
            return false;
        
        // Optional: $filter parameter
        var hasFilter = req.TryGetQueryParameter("filter", out var filterValue);
        
        return selectValue.ToString() == "id,name"
            && (!hasFilter || filterValue.ToString().Contains("active"));
    }
);
```

**Note:** Use `GetQueryParameter()` and `TryGetQueryParameter()` for type-safe query parameter access. These methods automatically try multiple naming conventions (e.g., `select`, `$select`, `%24select`) and provide helpful error messages if parameters are not found.

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
```

---

### 6. Mock DELETE Requests

```csharp
var fundId = Guid.NewGuid();

// Mock successful DELETE (no content)
mockClient.Api.Funds[fundId].MockDeleteAsync();

// With conditions
mockClient.Api.Funds[fundId].MockDeleteAsync(
    req => req.Headers.ContainsKey("If-Match")
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

// Mock 409 Conflict on DELETE
mockClient.Api.Funds[conflictingFundId].MockDeleteAsync<FundItemRequestBuilder>(
    new ApiException("Conflict - Fund has active transactions") { ResponseStatusCode = 409 }
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

Mocks a DELETE request that throws an exception.

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

---

## � Legacy API Reference - URL-Based Mocking

> **Note:** The type-safe extension methods (MockGetAsync, MockPostAsync, etc.) are now the recommended approach.  
> This section documents the older URL-based API for backward compatibility and migration purposes.

### Overview

The legacy API requires you to:
- Write URL templates manually as strings
- Handle path parameter name variations yourself  
- Use predicates to match specific requests

While still functional, the type-safe extensions provide better compile-time safety and are less error-prone.

---

### `GetMockableClient<T>()`

Creates a mockable instance of a Kiota-generated client.

**Type Parameter:**
- `T` - The Kiota-generated client type (must inherit from `BaseRequestBuilder`)

**Returns:** An instance of `T` with a mocked `IRequestAdapter`

**Example:**
```csharp
var mockClient = KiotaClientMockExtensions.GetMockableClient<MyKiotaClient>();
```

---

### `MockClientResponse<T, R>()`

Mocks a single object response for an endpoint using URL pattern matching.

**Parameters:**
- `urlTemplate` (string) - The URL pattern to match (e.g., `/api/items/{id}`)
- `returnObject` (R?) - The object to return (must implement `IParsable`)
- `requestInfoPredicate` (optional) - Additional matching conditions

**Overload:** `MockClientResponse<T>(string, string?)` - For string responses

**Example:**
```csharp
var fundId = Guid.NewGuid();
var fund = new Fund { Id = fundId, Name = "Test Fund" };

mockClient.MockClientResponse(
    "/api/funds/{id}",
    fund,
    req => req.GetPathParameter("id").ToString() == fundId.ToString()
);

// String response
mockClient.MockClientResponse("/api/status", "operational");
```

---

### `MockClientCollectionResponse<T, R>()`

Mocks a collection response using URL pattern matching.

**Parameters:**
- `urlTemplate` (string) - The URL pattern to match
- `returnObject` (IEnumerable<R>?) - The collection to return
- `requestInfoPredicate` (optional) - Additional matching conditions

**Example:**
```csharp
var activities = new List<Activity>
{
    new Activity { Id = Guid.NewGuid(), Name = "Activity 1" }
};

mockClient.MockClientCollectionResponse(
    "/api/funds/{fundId}/activities",
    activities,
    req => req.GetPathParameter("fundId").ToString() == fundId.ToString()
);
```

---

### `MockClientNoContentResponse<T>()`

Mocks a no-content (204) response using URL pattern matching.

**Parameters:**
- `urlTemplate` (string) - The URL pattern to match
- `requestInfoPredicate` (optional) - Additional matching conditions

**Example:**
```csharp
var fundId = Guid.NewGuid();

mockClient.MockClientNoContentResponse(
    "/api/funds/{id}",
    req => req.GetPathParameter("id").ToString() == fundId.ToString()
);
```

---

### `MockClientResponseException<T, R>()`

Mocks an exception for a single object endpoint using URL pattern matching.

**Parameters:**
- `urlTemplate` (string) - The URL pattern to match
- `exception` (Exception) - The exception to throw
- `requestInfoPredicate` (optional) - Additional matching conditions

**Example:**
```csharp
var nonExistentId = Guid.NewGuid();

mockClient.MockClientResponseException<TestRequestBuilder, Fund>(
    "/api/funds/{id}",
    new ApiException("Fund not found") { ResponseStatusCode = 404 },
    req => req.GetPathParameter("id").ToString() == nonExistentId.ToString()
);
```

---

### `MockClientCollectionResponseException<T, R>()`

Mocks an exception for a collection endpoint using URL pattern matching.

**Parameters:**
- `urlTemplate` (string) - The URL pattern to match
- `exception` (Exception) - The exception to throw
- `requestInfoPredicate` (optional) - Additional matching conditions

**Example:**
```csharp
mockClient.MockClientCollectionResponseException<TestRequestBuilder, Activity>(
    "/api/activities",
    new ApiException("Internal server error") { ResponseStatusCode = 500 }
);
```

---

### `MockClientNoContentResponseException<T>()`

Mocks an exception for a no-content endpoint using URL pattern matching.

**Parameters:**
- `urlTemplate` (string) - The URL pattern to match
- `exception` (Exception) - The exception to throw
- `requestInfoPredicate` (optional) - Additional matching conditions

**Example:**
```csharp
mockClient.MockClientNoContentResponseException(
    "/api/funds/{id}",
    new ApiException("Conflict") { ResponseStatusCode = 409 },
    req => req.GetPathParameter("id").ToString() == conflictingFundId.ToString()
);
```

### Legacy API Usage Notes

#### URL Pattern Matching

The library uses **positional token matching** on URL templates after normalizing Kiota's URL template format:

1. Strips the `{+baseurl}` prefix from Kiota's URL template
2. Removes query parameter template syntax `{?param1,param2}`
3. Replaces path parameters with positional tokens: `{pathParam1}`, `{pathParam2}`, etc.
4. Ensures leading slash for consistent matching
5. Performs case-insensitive exact match on the tokenized pattern

**Example:**
```csharp
// Kiota-generated: "{+baseurl}/api/funds/{fund-id}{?expand}"
// After normalization: "/api/funds/{pathParam1}"

// Your mock (any parameter name works structurally):
mockClient.MockClientResponse(
    "/api/funds/{id}",      // Normalized to "/api/funds/{pathParam1}" - Matches!
    fund,
    req => req.GetPathParameter("id").ToString() == fundId.ToString()
);
```

#### Smart Parameter Access

The library provides helper methods that try multiple naming variations:

- `GetPathParameter(name)` - Gets a parameter, throws clear exception if not found
- `TryGetPathParameter(name, out value)` - Safe version that returns bool

When you call `req.GetPathParameter("fundId")`, it automatically tries:
1. `fundId` (original - camelCase)
2. `fund-id` (kebab-case)
3. `fund%2Did` (URL-encoded kebab-case)
4. `FundId` (PascalCase)

#### Query Parameters

Query parameters can be accessed using type-safe helper methods similar to path parameters:

**Recommended Approach - Type-Safe Helpers:**

```csharp
// Use GetQueryParameter() - tries multiple naming conventions
mockClient.MockClientCollectionResponse(
    "/api/items",
    items,
    req => req.GetQueryParameter("select").ToString() == "id,name"
        && req.GetQueryParameter("filter").ToString() == "status eq 'active'"
);

// Works with OData-style parameters ($select, $filter, etc.)
mockClient.Api.Funds.MockGetCollectionAsync(
    funds,
    req => req.GetQueryParameter("select").ToString() == "id,name"
        // Automatically tries: select, $select, %24select, etc.
);

// Use TryGetQueryParameter() for optional parameters
mockClient.Api.Items.MockGetCollectionAsync(
    items,
    req => {
        // Required parameter
        if (!req.TryGetQueryParameter("select", out var selectValue))
            return false;
        
        // Optional parameter
        var hasFilter = req.TryGetQueryParameter("filter", out var filterValue);
        
        return selectValue.ToString() == "id,name" 
            && (!hasFilter || filterValue.ToString() == "status eq 'active'");
    }
);
```

The helper methods try these naming variations automatically:
1. `select` (original)
2. `$select` (OData style)
3. `%24select` (URL-encoded OData)
4. `select-param` (kebab-case)
5. `Select` (PascalCase)

**Legacy Approach - Direct Access:**

```csharp
// Direct access (not recommended - no naming convention handling)
mockClient.MockClientCollectionResponse(
    "/api/items",
    items,
    req => req.QueryParameters.ContainsKey("status") 
        && req.QueryParameters["status"].ToString() == "active"
);
```

#### Multiple Path Parameters

```csharp
var fundId = Guid.NewGuid();
var activityId = Guid.NewGuid();

mockClient.MockClientResponse(
    "/api/funds/{fundId}/activities/{activityId}",
    activity,
    req => req.GetPathParameter("fundId").ToString() == fundId.ToString()
        && req.GetPathParameter("activityId").ToString() == activityId.ToString()
);
```

---

## 🔍 Troubleshooting

### Mock Not Matching / Returning Null

**Problem:** Your mock is set up but the service still returns null or throws "not configured".

**Common Causes with Type-Safe Extensions:**

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

**Common Causes with Legacy URL-Based API:**

1. **Parameter name mismatch:**
   ```csharp
   // Use GetPathParameter - it tries variations automatically
   mockClient.MockClientResponse(
       "/api/funds/{fundId}",
       fund,
       req => req.GetPathParameter("fundId").ToString() == fundId.ToString()
   );
   ```

2. **URL pattern mismatch:**
   ```csharp
   // ❌ Mock: "/api/funds/{id}"
   // ❌ Actual: "/api/funds/{id}/activities"
   // These won't match - paths must have same structure
   ```

3. **Predicate always returns false:**
   ```csharp
   mockClient.MockClientResponse(
       "/api/funds/{fundId}",
       fund,
       req => req.GetPathParameter("fundId").ToString() == wrongId  // ❌ Wrong ID
   );
   ```

### Advanced Debugging

#### Check URL Template (Legacy API)

```csharp
var urlTemplate = KiotaClientMockExtensions.GetUrlTemplate(
    mockClient.Api.Funds[fundId]
);
Console.WriteLine($"Kiota's template: {urlTemplate}");

// Use in your mock
mockClient.MockClientResponse(
    urlTemplate,
    fund,
    req => {
        // Debug: Log all parameters
        foreach (var kvp in req.PathParameters)
        {
            Console.WriteLine($"  {kvp.Key} = {kvp.Value}");
        }
        return req.GetPathParameter("fund-id").ToString() == fundId.ToString()
    }
);
```

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

// OR use natural naming (recommended):
mockClient.MockClientResponse(
    "/api/funds/{fundId}",  // Natural camelCase
    fund,
    req => req.GetPathParameter("fundId").ToString() == fundId.ToString()
    //     Automatically tries: fundId, fund-id, fund%2Did, FundId
);
```

#### Test Fails After Regenerating Kiota Client

**Problem:** Tests were passing, but after regenerating your Kiota client, you get exceptions or mismatches.

**Cause:** The API contract changed (parameter renamed, path changed) and Kiota generated new code.

**Why This Is Good:** Your tests caught a breaking change!

**Solution:**
1. Check what changed in the generated code
2. Verify with your API team if this was intentional
3. Update your tests to reflect the new contract

**With Type-Safe Extensions:** Compilation errors will guide you to what needs updating.

**With Legacy API:** Runtime errors will show parameter name mismatches.

---
    req => {
        // Debug: Log all parameter keys and values
        Console.WriteLine("=== Request Debug Info ===");
        Console.WriteLine($"URL: {req.UrlTemplate}");
        Console.WriteLine($"Parameters:");
        foreach (var kvp in req.PathParameters)
        {
            Console.WriteLine($"  {kvp.Key} = {kvp.Value}");
        }
        
        // Now check with the correct key
        return req.GetPathParameter("fund-id").ToString() == fundId.ToString();
    }
);
```

### Test Fails After Regenerating Kiota Client

**Problem:** Tests were passing, but after regenerating your Kiota client, you get `KeyNotFoundException`.

**Cause:** The API contract changed (parameter renamed, path changed) and Kiota generated new code.

**Why This Is Good:** Your tests caught a breaking change! This is exactly what explicit parameter checking is designed to do.

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
mockedClient.MockClientResponse(
    "/api/funds/{fund-id}/activities/{activity-id}/comments/{comment-id}",
    comment,
    req => req.GetPathParameter("fund-id").ToString() == fundId.ToString()
        && req.GetPathParameter("activity-id").ToString() == activityId.ToString()
        && req.GetPathParameter("comment-id").ToString() == commentId.ToString()
);
```

### Using GetUrlTemplate() Helper

**Purpose:** Extract URL templates programmatically from Kiota's generated request builders.

**Usage:**
```csharp
// Get template from a request builder instance
var urlTemplate = KiotaClientMockExtensions.GetUrlTemplate(
    mockClient.Api.Funds[fundId]
);
// Returns: "/api/funds/{*}"

// Use in mock (but still need to check parameter keys explicitly)
mockedClient.MockClientResponse(
    urlTemplate,
    fund,
    req => req.GetPathParameter("fund-id").ToString() == fundId.ToString()
);
```

**Note:** `GetUrlTemplate()` returns wildcards for parameters (`{*}`), which is useful for the URL pattern. However, you still need to know the exact parameter key names (from generated code) for your predicates.

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

To find the exact URL template for manual mocking:

1. **Check the generated request builder**:
   ```csharp
   // In: FundItemRequestBuilder.cs
   public FundItemRequestBuilder(...)
       : base(requestAdapter, 
              "{+baseurl}/api/funds/{fund%2Did}",  // ← This is your URL template
              pathParameters)
   ```

2. **Or use GetUrlTemplate()** (for reference):
   ```csharp
   var template = KiotaClientMockExtensions.GetUrlTemplate(
       _client.Api.Funds[fundId]
   );
   // But note: URL-decoded, so "{fund%2Did}" becomes "{fund-id}"
   ```

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


