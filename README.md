# Gainsway.Kiota.Testing
  - [Mocking 404 Not Found](#mocking-404-not-found)
  - [Mocking Empty Collections](#mocking-empty-collections)
  - [Mocking with Query String Parameters](#mocking-with-query-string-parameters)
  - [URL Pattern Matching](#url-pattern-matching) testing library that simplifies mocking [Kiota-generated](https://learn.microsoft.com/en-us/openapi/kiota/overview) API clients for unit tests using NSubstitute.

[![NuGet](https://img.shields.io/nuget/v/Gainsway.Kiota.Testing.svg)](https://www.nuget.org/packages/Gainsway.Kiota.Testing/)

## 📋 Table of Contents

- [Gainsway.Kiota.Testing](#gainswaykiotatesting)
  - [📋 Table of Contents](#-table-of-contents)
  - [📦 Installation](#-installation)
  - [🚀 Quick Start](#-quick-start)
  - [📖 Usage Examples](#-usage-examples)
    - [1. Basic Setup - Create a Mockable Client](#1-basic-setup---create-a-mockable-client)
    - [2. Mock Single Object Response](#2-mock-single-object-response)
    - [3. Mock String/Primitive Response](#3-mock-stringprimitive-response)
    - [4. Mock Collection Response](#4-mock-collection-response)
    - [5. Mock No-Content Response](#5-mock-no-content-response)
    - [6. Using Path Parameters](#6-using-path-parameters)
    - [7. Multiple Path Parameters](#7-multiple-path-parameters)
    - [8. Complex Predicates with Multiple Conditions](#8-complex-predicates-with-multiple-conditions)
    - [9. Mocking Multiple Endpoints](#9-mocking-multiple-endpoints)
    - [10. Nested Resource Paths](#10-nested-resource-paths)
  - [🔧 Advanced Scenarios](#-advanced-scenarios)
    - [Mocking Null Responses](#mocking-null-responses)
    - [Mocking Empty Collections](#mocking-empty-collections)
    - [Mocking with Query String Parameters](#mocking-with-query-string-parameters)
    - [URL Pattern Matching](#url-pattern-matching)
  - [🧪 Complete Test Example](#-complete-test-example)
  - [📚 API Reference](#-api-reference)
    - [`GetMockableClient<T>()`](#getmockableclientt)
    - [`MockClientResponse<T, R>()`](#mockclientresponset-r)
    - [`MockClientCollectionResponse<T, R>()`](#mockclientcollectionresponset-r)
    - [`MockClientNoContentResponse<T>()`](#mockclientnocontentresponset)

## 📦 Installation

```bash
dotnet add package Gainsway.Kiota.Testing
```

## 🚀 Quick Start

```csharp
using Gainsway.Kiota.Testing;

// 1. Create a mockable client
var mockClient = KiotaClientMockExtensions.GetMockableClient<MyKiotaClient>();

// 2. Setup mock response
mockClient.MockClientResponse(
    "/api/items/{id}",
    new MyItem { Id = "123", Name = "Test Item" },
    req => req.PathParameters["id"].ToString() == "123"
);

// 3. Use in your test
var service = new MyService(mockClient);
var result = await service.GetItemAsync("123");

// 4. Assert
Assert.That(result.Name, Is.EqualTo("Test Item"));
```

## 📖 Usage Examples

### 1. Basic Setup - Create a Mockable Client

Create a mocked instance of your Kiota-generated client:

```csharp
using Gainsway.Kiota.Testing;

var mockedClient = KiotaClientMockExtensions.GetMockableClient<MyKiotaClient>();
```

This creates a client with a mocked `IRequestAdapter` that you can configure for your tests.

---

### 2. Mock Single Object Response

Mock an endpoint that returns a single object (IParsable):

```csharp
var expectedFund = new Fund 
{ 
    Id = Guid.NewGuid(), 
    Name = "Test Fund",
    Status = FundStatus.Active
};

mockedClient.MockClientResponse(
    "/api/funds/{id}",
    expectedFund
);
```

**When to use:** GET endpoints that return a single entity.

---

### 3. Mock String/Primitive Response

Mock an endpoint that returns a primitive type like string:

```csharp
mockedClient.MockClientResponse(
    "/api/status",
    "operational"
);
```

Or with a variable:

```csharp
string expectedStatus = "maintenance";
mockedClient.MockClientResponse(
    "/api/system/status",
    expectedStatus
);
```

**When to use:** Endpoints that return plain text, status codes, or primitive values.

---

### 4. Mock Collection Response

Mock an endpoint that returns a list of objects:

```csharp
var expectedActivities = new List<Activity>
{
    new Activity { Id = Guid.NewGuid(), Name = "Activity 1" },
    new Activity { Id = Guid.NewGuid(), Name = "Activity 2" },
    new Activity { Id = Guid.NewGuid(), Name = "Activity 3" }
};

mockedClient.MockClientCollectionResponse(
    "/api/activities",
    expectedActivities
);
```

**When to use:** GET endpoints that return arrays or lists.

---

### 5. Mock No-Content Response

Mock an endpoint that returns no content (204 No Content, typically DELETE or PUT):

```csharp
var fundId = Guid.NewGuid();

mockedClient.MockClientNoContentResponse(
    "/api/funds/{id}",
    req => req.PathParameters["id"].ToString() == fundId.ToString()
);
```

**When to use:** DELETE operations, void PUT/POST operations, or any endpoint that doesn't return data.

---

### 6. Using Path Parameters

Match requests based on specific path parameter values:

```csharp
var fundId = Guid.NewGuid();
var expectedFund = new Fund { Id = fundId, Name = "Specific Fund" };

mockedClient.MockClientResponse(
    "/api/funds/{id}",
    expectedFund,
    req => req.PathParameters["id"].ToString() == fundId.ToString()
);
```

**Key point:** The predicate ensures only requests with the matching `id` return this mock.

---

### 7. Multiple Path Parameters

Match requests with multiple path parameters:

```csharp
var fundId = Guid.NewGuid();
var activityId = Guid.NewGuid();
var expectedActivity = new Activity { Id = activityId, Name = "Modified" };

mockedClient.MockClientResponse(
    "/api/funds/{fundId}/activities/{activityId}",
    expectedActivity,
    req => req.PathParameters["fundId"].ToString() == fundId.ToString()
        && req.PathParameters["activityId"].ToString() == activityId.ToString()
);
```

**When to use:** Nested resource endpoints like `/users/{userId}/posts/{postId}`.

---

### 8. Complex Predicates with Multiple Conditions

Combine multiple conditions for more specific matching:

```csharp
mockedClient.MockClientResponse(
    "/api/funds/{id}",
    expectedFund,
    req => req.PathParameters["id"].ToString() == fundId.ToString()
        && req.HttpMethod == Method.GET
        && req.Headers.ContainsKey("Authorization")
);
```

**When to use:** When you need to differentiate based on HTTP method, headers, or other request properties.

---

### 9. Mocking Multiple Endpoints

Set up multiple mocks for different endpoints:

```csharp
var fundId1 = Guid.NewGuid();
var fundId2 = Guid.NewGuid();

// Mock endpoint 1
mockedClient.MockClientResponse(
    "/api/funds/{id}",
    new Fund { Id = fundId1, Name = "Fund 1" },
    req => req.PathParameters["id"].ToString() == fundId1.ToString()
);

// Mock endpoint 2
mockedClient.MockClientResponse(
    "/api/funds/{id}",
    new Fund { Id = fundId2, Name = "Fund 2" },
    req => req.PathParameters["id"].ToString() == fundId2.ToString()
);

// Mock related collection endpoint
mockedClient.MockClientCollectionResponse(
    "/api/funds/{fundId}/activities",
    new List<Activity> { /* activities */ },
    req => req.PathParameters["fundId"].ToString() == fundId1.ToString()
);
```

**Key point:** Predicates ensure each mock matches only its intended requests.

---

### 10. Nested Resource Paths

Mock deeply nested API paths:

```csharp
var fundId = Guid.NewGuid();
var activityId = Guid.NewGuid();

mockedClient.MockClientResponse(
    "/api/funds/{fundId}/activities/{activityId}/modify",
    modifiedActivity,
    req => req.PathParameters["fundId"].ToString() == fundId.ToString()
        && req.PathParameters["activityId"].ToString() == activityId.ToString()
);
```

**Common patterns:**
- `/api/funds/{fundId}/activities` - List activities
- `/api/funds/{fundId}/activities/{activityId}` - Get specific activity
- `/api/funds/{fundId}/activities/{activityId}/modify` - Modify activity

---

## 🔧 Advanced Scenarios

### Mocking Null Responses

```csharp
Fund? nullFund = null;
mockedClient.MockClientResponse(
    "/api/funds/{id}",
    nullFund,
    req => req.PathParameters["id"].ToString() == nonExistentId.ToString()
);
```

### Mocking Empty Collections

```csharp
mockedClient.MockClientCollectionResponse(
    "/api/activities",
    new List<Activity>() // Empty list
);
```

### Mocking with Query String Parameters

The library strips Kiota's query parameter **template syntax** (e.g., `{?page,limit}`) from URL matching, but does **NOT** match actual query string values in the URL template parameter.

To mock endpoints with specific query parameters, use the `requestInfoPredicate`:

```csharp
// Mock GET /api/items?status=active&page=1
mockedClient.MockClientCollectionResponse(
    "/api/items",
    activeItems,
    req => req.QueryParameters.ContainsKey("status") 
        && req.QueryParameters["status"].ToString() == "active"
        && req.QueryParameters.ContainsKey("page")
        && req.QueryParameters["page"].ToString() == "1"
);

// Mock GET /api/search?q=test
mockedClient.MockClientCollectionResponse(
    "/api/search",
    searchResults,
    req => req.QueryParameters.ContainsKey("q")
        && req.QueryParameters["q"].ToString() == "test"
);
```

**Important:** Query parameters are accessed through `req.QueryParameters`, not embedded in the URL template string.

### URL Pattern Matching

The library uses `EndsWith()` matching on URL templates after stripping Kiota's query parameter template placeholders:

```csharp
// Kiota URL template (what's in req.UrlTemplate):
"/api/funds/{id}{?expand,select}"

// After regex strip of {?...}:
"/api/funds/{id}"

// Your mock URL template should be:
"/api/funds/{id}"  // ✅ Matches

// These will also match:
"funds/{id}"       // ✅ Matches (uses EndsWith)
"{id}"             // ✅ Matches (uses EndsWith)

// This won't match:
"/api/funds"       // ❌ Doesn't end with /{id}
```

**Note:** The regex pattern `@"\{\?.*?\}"` only removes Kiota's optional parameter syntax, not actual query string values.

---

## 🧪 Complete Test Example

Here's a full test demonstrating the library in action:

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

        _mockClient.MockClientResponse(
            "/api/funds/{id}",
            expectedFund,
            req => req.PathParameters["id"].ToString() == fundId.ToString()
        );

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

        _mockClient.MockClientCollectionResponse(
            "/api/funds/{fundId}/activities",
            expectedActivities,
            req => req.PathParameters["fundId"].ToString() == fundId.ToString()
        );

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

        _mockClient.MockClientNoContentResponse(
            "/api/funds/{id}",
            req => req.PathParameters["id"].ToString() == fundId.ToString()
        );

        // Act & Assert - Should not throw
        Assert.DoesNotThrowAsync(async () => 
            await _service.DeleteFundAsync(fundId)
        );
    }

    [Test]
    public async Task StartSeeding_WithMetadata_ShouldProcessCorrectly()
    {
        // Arrange
        var fundId = Guid.NewGuid();
        var metadata = new SeedingMetadata
        {
            Recipients = new[] 
            { 
                new Recipient { AccountId = Guid.NewGuid(), Amount = 1500.25 },
                new Recipient { AccountId = Guid.NewGuid(), Amount = 2999.99 }
            }
        };

        var expectedFund = new Fund 
        { 
            Id = fundId, 
            Status = FundStatus.Seeding 
        };

        _mockClient.MockClientResponse(
            "/api/funds/{id}",
            expectedFund,
            req => req.PathParameters["id"].ToString() == fundId.ToString()
        );

        // Act
        var result = await _service.StartSeedingAsync(fundId, metadata);

        // Assert
        Assert.That(result.Fund.Status, Is.EqualTo(FundStatus.Seeding));
        Assert.That(result.Message, Does.Contain("Seeding process started"));
    }
}
```

---

## 📚 API Reference

### `GetMockableClient<T>()`

Creates a mockable instance of a Kiota-generated client.

**Type Parameter:**
- `T` - The Kiota-generated client type (must inherit from `BaseRequestBuilder`)

**Returns:** An instance of `T` with a mocked `IRequestAdapter`

---

### `MockClientResponse<T, R>()`

Mocks a single object response for an endpoint.

**Parameters:**
- `urlTemplate` (string) - The URL pattern to match (e.g., `/api/items/{id}`)
- `returnObject` (R?) - The object to return (must implement `IParsable`)
- `requestInfoPredicate` (optional) - Additional matching conditions

**Overload:** `MockClientResponse<T>(string, string?)` - For string responses

---

### `MockClientCollectionResponse<T, R>()`

Mocks a collection response for an endpoint.

**Parameters:**
- `urlTemplate` (string) - The URL pattern to match
- `returnObject` (IEnumerable<R>?) - The collection to return
- `requestInfoPredicate` (optional) - Additional matching conditions

---

### `MockClientNoContentResponse<T>()`

Mocks a no-content (204) response for an endpoint.

**Parameters:**
- `urlTemplate` (string) - The URL pattern to match
- `requestInfoPredicate` (optional) - Additional matching conditions


