# Gainsway.Kiota.Testing

A testing library that simplifies mocking [Kiota-generated](https://learn.microsoft.com/en-us/openapi/kiota/overview) API clients for unit tests using NSubstitute.
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
    - [Mocking Exception Responses](#mocking-exception-responses)
    - [Mocking POST/PUT with Request Body](#mocking-postput-with-request-body)
    - [URL Pattern Matching](#url-pattern-matching)
    - [Writing Mocks with Natural Parameter Names](#writing-mocks-with-natural-parameter-names)
    - [Finding Kiota's Actual Parameter Names (For Reference)](#finding-kiotas-actual-parameter-names-for-reference)
    - [Smart Parameter Access Methods](#smart-parameter-access-methods)
    - [Best Practice Examples](#best-practice-examples)
    - [Why This Approach Works](#why-this-approach-works)
  - [🧪 Complete Test Example](#-complete-test-example)
  - [📚 API Reference](#-api-reference)
    - [`GetMockableClient<T>()`](#getmockableclientt)
    - [`MockClientResponse<T, R>()`](#mockclientresponset-r)
    - [`MockClientCollectionResponse<T, R>()`](#mockclientcollectionresponset-r)
    - [`MockClientNoContentResponse<T>()`](#mockclientnocontentresponset)
    - [`MockClientResponseException<T, R>()`](#mockclientresponseexceptiont-r)
    - [`MockClientCollectionResponseException<T, R>()`](#mockclientcollectionresponseexceptiont-r)
    - [`MockClientNoContentResponseException<T>()`](#mockclientnocontentresponseexceptiont)
  - [🔍 Troubleshooting](#-troubleshooting)
    - [KeyNotFoundException: The given key was not present in the dictionary](#keynotfoundexception-the-given-key-was-not-present-in-the-dictionary)
    - [Mock Not Matching / Returning Null](#mock-not-matching--returning-null)
    - [Finding Parameter Names for Complex Nested Paths](#finding-parameter-names-for-complex-nested-paths)
    - [Using GetUrlTemplate() Helper](#using-geturltemplate-helper)

## 📦 Installation

```bash
dotnet add package Gainsway.Kiota.Testing
```

## 🚀 Quick Start

```csharp
using Gainsway.Kiota.Testing;

// 1. Create a mockable client
var mockClient = KiotaClientMockExtensions.GetMockableClient<MyKiotaClient>();

// 2. Setup mock response using camelCase parameter names (natural C# style)
// The library automatically tries naming variations (camelCase, kebab-case, URL-encoded)
var itemId = "123";
mockClient.MockClientResponse(
    "/api/items/{itemId}",
    new MyItem { Id = itemId, Name = "Test Item" },
    req => req.GetPathParameter("itemId").ToString() == itemId
    //     ^^^ Smart parameter access - tries multiple naming conventions
);

// 3. Use in your test
var service = new MyService(mockClient);
var result = await service.GetItemAsync(itemId);

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
    req => req.GetPathParameter("id").ToString() == fundId.ToString()
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
    req => req.GetPathParameter("id").ToString() == fundId.ToString()
);
```

**Key point:** The predicate ensures only requests with the matching `id` return this mock. The `GetPathParameter()` method automatically handles Kiota's naming variations.

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
    req => req.GetPathParameter("fundId").ToString() == fundId.ToString()
        && req.GetPathParameter("activityId").ToString() == activityId.ToString()
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
    req => req.GetPathParameter("id").ToString() == fundId.ToString()
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
    req => req.GetPathParameter("id").ToString() == fundId1.ToString()
);

// Mock endpoint 2
mockedClient.MockClientResponse(
    "/api/funds/{id}",
    new Fund { Id = fundId2, Name = "Fund 2" },
    req => req.GetPathParameter("id").ToString() == fundId2.ToString()
);

// Mock related collection endpoint
mockedClient.MockClientCollectionResponse(
    "/api/funds/{fundId}/activities",
    new List<Activity> { /* activities */ },
    req => req.GetPathParameter("fundId").ToString() == fundId1.ToString()
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
    req => req.GetPathParameter("fundId").ToString() == fundId.ToString()
        && req.GetPathParameter("activityId").ToString() == activityId.ToString()
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
    req => req.GetPathParameter("id").ToString() == nonExistentId.ToString()
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

### Mocking Exception Responses

Mock endpoints that throw exceptions (e.g., 404 Not Found, 500 Internal Server Error):

```csharp
// Mock 404 Not Found for a single object endpoint
var nonExistentId = Guid.NewGuid();
mockedClient.MockClientResponseException<TestRequestBuilder, Fund>(
    "/api/funds/{id}",
    new ApiException("Fund not found") { ResponseStatusCode = 404 },
    req => req.GetPathParameter("id").ToString() == nonExistentId.ToString()
);

// Mock 500 Internal Server Error for a collection endpoint
mockedClient.MockClientCollectionResponseException<TestRequestBuilder, Activity>(
    "/api/activities",
    new ApiException("Internal server error") { ResponseStatusCode = 500 }
);

// Mock unauthorized access (401)
mockedClient.MockClientResponseException<TestRequestBuilder, Fund>(
    "/api/funds/{id}",
    new ApiException("Unauthorized") { ResponseStatusCode = 401 },
    req => !req.Headers.ContainsKey("Authorization")
);

// Mock exception for no-content operations (e.g., DELETE)
mockedClient.MockClientNoContentResponseException(
    "/api/funds/{id}",
    new ApiException("Conflict - Fund has active transactions") { ResponseStatusCode = 409 },
    req => req.GetPathParameter("id").ToString() == conflictingFundId.ToString()
);
```

**When to use:** Testing error handling, validation failures, authorization issues, or any scenario where the API should throw an exception.

### Mocking POST/PUT with Request Body

Mock endpoints that accept request bodies and verify the content:

```csharp
// Mock POST to create a new fund
var newFund = new Fund 
{ 
    Id = Guid.NewGuid(), 
    Name = "New Fund",
    Status = FundStatus.Active 
};

mockedClient.MockClientResponse(
    "/api/funds",
    newFund,
    req => req.HttpMethod == Method.POST
        && req.Content != null
);

// Mock PUT to update a fund with body validation
var updatedFund = new Fund 
{ 
    Id = existingFundId, 
    Name = "Updated Fund",
    Status = FundStatus.Closed
};

mockedClient.MockClientResponse(
    "/api/funds/{id}",
    updatedFund,
    req => req.HttpMethod == Method.PUT
        && req.GetPathParameter("id").ToString() == existingFundId.ToString()
        && req.Content != null
);

// Mock POST with specific body property validation
// Note: You may need to deserialize req.Content to inspect body properties
mockedClient.MockClientResponse(
    "/api/funds/{fundId}/activities",
    createdActivity,
    req => req.HttpMethod == Method.POST
        && req.GetPathParameter("fundId").ToString() == fundId.ToString()
        && req.Headers.ContainsKey("Content-Type")
        && req.Headers["Content-Type"].Contains("application/json")
);

// Mock PATCH for partial updates
mockedClient.MockClientResponse(
    "/api/funds/{id}",
    patchedFund,
    req => req.HttpMethod == Method.PATCH
        && req.GetPathParameter("id").ToString() == fundId.ToString()
);
```

**When to use:** Testing create, update, or any operation that sends data to the API. Use predicates to verify HTTP method, content type, and other request properties.

**Note:** For deep inspection of request body content, you may need to read and deserialize `req.Content` within your predicate, though this can be complex in unit tests.

### URL Pattern Matching

The library uses **positional token matching** on URL templates after normalizing Kiota's URL template format. This provides the perfect balance of flexibility and validation.

**Normalization Process:**
1. Strips the `{+baseurl}` prefix from Kiota's URL template
2. Removes query parameter template syntax `{?param1,param2}`
3. Replaces path parameters with positional tokens: `{pathParam1}`, `{pathParam2}`, etc.
4. Ensures leading slash for consistent matching
5. Performs case-insensitive exact match on the tokenized pattern

**Examples:**

```csharp
// Kiota-generated URL template:
"{+baseurl}/api/funds/{fund-id}{?expand,select}"

// After normalization:
"/api/funds/{pathParam1}"

// Your mock URL template (any parameter name works):
"/api/funds/{id}"      // ✅ Normalized to "/api/funds/{pathParam1}" - Matches!
"/api/funds/{fundId}"  // ✅ Normalized to "/api/funds/{pathParam1}" - Matches!
"/api/funds/{ids}"     // ✅ Normalized to "/api/funds/{pathParam1}" - Matches!

// These WON'T match (different structure):
"/api/funds"                    // ❌ Normalized to "/api/funds" - Missing parameter
"/api/funds/{id}/activities"   // ❌ Normalized to "/api/funds/{pathParam1}/activities" - Extra path segment
"funds/{id}"                    // ❌ Normalized to "/funds/{pathParam1}" - Missing "/api" prefix
```

**What Positional Tokens Validate:**

✅ **Parameter Count** - Must have the same number of path parameters:
```csharp
"/api/funds/{id}"                              → "/api/funds/{pathParam1}"
"/api/funds/{fundId}/activities/{activityId}"  → "/api/funds/{pathParam1}/activities/{pathParam2}"

// ❌ These won't match (different parameter counts)
"/api/funds/{id}" vs "/api/funds/{fundId}/activities/{activityId}"
```

✅ **Parameter Positions** - Parameters must be in the same locations:
```csharp
"/api/funds/{id}/activities"    → "/api/funds/{pathParam1}/activities"
"/api/funds/activities/{id}"    → "/api/funds/activities/{pathParam1}"

// ❌ These won't match (parameter in different position)
```

✅ **Path Structure** - The overall URL structure must match:
```csharp
"/api/funds/{id}"               → "/api/funds/{pathParam1}"
"/api/funds/{id}/metadata"      → "/api/funds/{pathParam1}/metadata"

// ❌ Different structure won't match
```

✅ **Flexible Parameter Naming** - Parameter names don't matter for matching:
```csharp
// All of these produce the same pattern: "/api/funds/{pathParam1}"
"/api/funds/{id}"
"/api/funds/{fundId}"
"/api/funds/{fund-id}"
"/api/funds/{ids}"      // Even typos match structurally!

// The actual parameter name validation happens in your predicate:
req => req.GetPathParameter("fundId").ToString() == fundId.ToString()
//     ^^^ This tries: fundId, fund-id, fund%2Did, FundId
```

**Why Positional Token Matching?**

This approach gives you the best of both worlds:

1. **Write natural test code** using camelCase parameter names
2. **Automatic compatibility** with Kiota's naming conventions (kebab-case, URL-encoded, etc.)
3. **Structure validation** catches actual errors like missing parameters or wrong paths
4. **Parameter flexibility** lets you use any naming style in your mocks

**Example:**

```csharp
// Your mock with natural C# naming:
mockedClient.MockClientResponse(
    "/api/funds/{fundId}",  // You use natural camelCase
    fund,
    req => req.GetPathParameter("fundId").ToString() == fundId.ToString()
);

// Kiota's generated request builder uses kebab-case:
// "{+baseurl}/api/funds/{fund-id}"

// What happens:
// 1. Your pattern: "/api/funds/{fundId}" → "/api/funds/{pathParam1}" ✅
// 2. Kiota's URL:  "/api/funds/{fund-id}" → "/api/funds/{pathParam1}" ✅
// 3. Pattern matches! Predicate executes.
// 4. GetPathParameter("fundId") tries: fundId, fund-id, fund%2Did, FundId
// 5. Finds "fund-id" in PathParameters ✅
// 6. Returns the value!
```

**Independent Mock Setup:**

Positional tokens allow you to mock similar endpoints independently:

```csharp
// Different structures produce different token patterns
mockedClient.MockClientResponse(
    "/api/funds/{id}",           // → "/api/funds/{pathParam1}"
    fund
);

mockedClient.MockClientCollectionResponse(
    "/api/funds/{id}/activities", // → "/api/funds/{pathParam1}/activities"
    activities
);

mockedClient.MockClientResponse(
    "/api/funds/{id}/metadata",   // → "/api/funds/{pathParam1}/metadata"
    metadata
);

// Each mock only matches its specific structure
```

**Same Pattern, Different Values:**

You can mock the same endpoint multiple times with different predicates:

```csharp
// Both normalize to "/api/funds/{pathParam1}", but predicates differentiate:
mockedClient.MockClientResponse(
    "/api/funds/{id}",
    fund1,
    req => req.GetPathParameter("id").ToString() == fundId1.ToString()
);

mockedClient.MockClientResponse(
    "/api/funds/{id}",
    fund2,
    req => req.GetPathParameter("id").ToString() == fundId2.ToString()
);
```

**Note:** Query parameter template syntax `{?param1,param2}` is removed during normalization and doesn't affect pattern matching.

### Writing Mocks with Natural Parameter Names

**Best Practice:** Write your mocks using **camelCase** parameter names (matching your C# variable naming conventions). The library provides smart parameter access methods that automatically try multiple naming variations.

**Recommended Pattern:**

```csharp
// ✅ Use natural camelCase names in your tests
var fundId = Guid.NewGuid();

_fundManagementServiceClient.MockClientResponse(
    "/api/funds/{fundId}",
    fund,
    req => req.GetPathParameter("fundId").ToString() == fundId.ToString()
    //     ^^^ Automatically tries: fundId, fund-id, fund%2Did, FundId
);
```

**How It Works:**

The library provides extension methods that automatically try common naming variations when accessing path parameters:

- **`GetPathParameter(name)`** - Gets a parameter, throws clear exception if not found
- **`TryGetPathParameter(name, out value)`** - Safe version that returns bool

When you access `req.GetPathParameter("fundId")`, the library tries:
1. `"fundId"` (camelCase - as you provided)
2. `"fund-id"` (kebab-case)
3. `"fund%2Did"` (URL-encoded kebab-case)
4. `"FundId"` (PascalCase)

**Example with Error Handling:**

```csharp
// If Kiota uses a different parameter name, you'll get a clear error:
try
{
    var id = req.GetPathParameter("fundId");
}
catch (KeyNotFoundException ex)
{
    // Exception message shows:
    // - All naming variations tried
    // - Kiota's actual URL template
    // - Available parameter keys
    // - Guidance on how to fix
}
```

**Using TryGetPathParameter for Optional Parameters:**

```csharp
// Safe version that doesn't throw
if (req.TryGetPathParameter("fundId", out var id))
{
    return id.ToString() == fundId.ToString();
}
return false;
```

### Finding Kiota's Actual Parameter Names (For Reference)

In most cases, you won't need to know Kiota's exact parameter names thanks to the smart parameter access methods. However, if you need to debug or understand what Kiota generated:

**Solution 1: Inspect Generated Code**

Look at the Kiota-generated request builder file:

```csharp
// Example: Open FundItemRequestBuilder.cs (generated by Kiota)
public class FundItemRequestBuilder : BaseRequestBuilder
{
    public FundItemRequestBuilder(...) 
        : base(requestAdapter, "{+baseurl}/api/funds/{fund%2Did}", pathParameters)
    //                          ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
    //                          Kiota uses "fund-id" (URL-encoded hyphen: %2D)
}

// In your tests, you can use any variation:
mockedClient.MockClientResponse(
    "/api/funds/{fundId}",  // Your natural naming
    fund,
    req => req.GetPathParameter("fundId").ToString() == fundId.ToString()
    //                          ^^^^^^^^ Will find "fund-id", "fund%2Did", etc.
);
```

**Solution 2: Runtime Discovery (Debugging)**

Add logging to see what the library finds:

```csharp
mockedClient.MockClientResponse(
    "/api/funds/{fundId}",
    fund,
    req => {
        // The extension method will show you all available keys if it fails
        try
        {
            var id = req.GetPathParameter("fundId");
            Console.WriteLine($"Found parameter with value: {id}");
            return id.ToString() == fundId.ToString();
        }
        catch (KeyNotFoundException ex)
        {
            Console.WriteLine(ex.Message);  // Shows all variations tried and available keys
            throw;
        }
    }
);
```

**Solution 3: Use GetUrlTemplate() Helper**

Extract the exact URL template from Kiota's generated request builder:

```csharp
// Get the template from a request builder instance
var urlTemplate = KiotaClientMockExtensions.GetUrlTemplate(
    mockClient.Api.Funds[fundId]
);
// Returns: "/api/funds/{fund-id}" (Kiota's exact parameter name, URL-decoded)

// Use it in your mock
mockedClient.MockClientResponse(
    urlTemplate,  // Use Kiota's exact template
    fund,
    req => req.GetPathParameter("fundId").ToString() == fundId.ToString()
    //     Still use natural naming in predicates - library handles the mismatch
);
```

---

### Smart Parameter Access Methods

The library provides extension methods that automatically handle Kiota's parameter naming variations:

**GetPathParameter() - Recommended**

Throws a clear, descriptive exception if the parameter is not found:

```csharp
// ✅ Automatic variation handling with clear error messages
mockedClient.MockClientResponse(
    "/api/funds/{fundId}",
    fund,
    req => req.GetPathParameter("fundId").ToString() == fundId.ToString()
);

// If "fundId" doesn't match any variation, you get a helpful exception showing:
// - All naming variations tried (fundId, fund-id, fund%2Did, FundId)
// - Kiota's actual URL template
// - Available parameter keys
// - How to fix the issue
```

**TryGetPathParameter() - For Conditional Logic**

Returns bool without throwing exceptions:

```csharp
// ✅ Safe approach for optional parameters
mockedClient.MockClientResponse(
    "/api/funds/{fundId}",
    fund,
    req => {
        if (req.TryGetPathParameter("fundId", out var id))
        {
            return id.ToString() == fundId.ToString();
        }
        return false;
    }
);
```

**Multiple Parameters:**

```csharp
// ✅ Clean and natural - just use GetPathParameter for each parameter
var fundId = Guid.NewGuid();
var activityId = Guid.NewGuid();

mockedClient.MockClientResponse(
    "/api/funds/{fundId}/activities/{activityId}",
    activity,
    req => req.GetPathParameter("fundId").ToString() == fundId.ToString()
        && req.GetPathParameter("activityId").ToString() == activityId.ToString()
);
```

**Naming Variations Tried:**

When you call `GetPathParameter("fundId")` or `TryGetPathParameter("fundId", out var id)`, the library tries:
1. `fundId` (original - camelCase)
2. `fund-id` (kebab-case)
3. `fund%2Did` (URL-encoded kebab-case)
4. `FundId` (PascalCase)

This handles all common naming conventions that Kiota might generate.

---

### Best Practice Examples

**Write Tests with Natural CamelCase:**

```csharp
// ✅ Use idiomatic C# naming with smart parameter access
var fundId = Guid.NewGuid();

mockedClient.MockClientResponse(
    "/api/funds/{fundId}",
    fund,
    req => req.GetPathParameter("fundId").ToString() == fundId.ToString()
);

// Works automatically with any of these Kiota-generated names:
// - fundId (camelCase)
// - fund-id (kebab-case)
// - fund%2Did (URL-encoded)
// - FundId (PascalCase)
```

**Multiple Parameters:**

```csharp
var fundId = Guid.NewGuid();
var activityId = Guid.NewGuid();

// Clean and natural - library handles naming variations
mockedClient.MockClientResponse(
    "/api/funds/{fundId}/activities/{activityId}",
    activity,
    req => req.GetPathParameter("fundId").ToString() == fundId.ToString()
        && req.GetPathParameter("activityId").ToString() == activityId.ToString()
);
```

**Optional Parameters with TryGetPathParameter:**

```csharp
// When parameter might not be present
mockedClient.MockClientResponse(
    "/api/search",
    results,
    req => {
        if (req.TryGetPathParameter("query", out var q))
        {
            return q.ToString() == searchQuery;
        }
        return true; // Match all requests without query parameter
    }
);
```

---

### Why This Approach Works

**Positional Token Matching:** The library normalizes URL templates using sequential positional tokens:
- Your pattern: `/api/funds/{fundId}` → normalized to `/api/funds/{pathParam1}`
- Kiota's request: `/api/funds/{fund-id}` → normalized to `/api/funds/{pathParam1}`
- Kiota's request: `/api/funds/{fund%2Did}` → normalized to `/api/funds/{pathParam1}` (URL-decoded first)
- **Result:** Pattern matches because they have the same structure (one parameter in position 1)

This validates:
- ✅ **Parameter count** - Must have same number of parameters
- ✅ **Parameter positions** - Parameters must be in same locations  
- ✅ **Path structure** - Overall URL structure must match
- ✅ **Flexible naming** - Parameter names don't need to match

**Smart Parameter Access:** The extension methods handle naming variations automatically:
- You write: `req.GetPathParameter("fundId")`
- Library tries: `fundId`, `fund-id`, `fund%2Did`, `FundId`
- **Result:** Works with any Kiota naming convention

**Parameter Validation:** Your predicate checks the actual parameter values:
- Validates that the correct ID is being used
- Catches bugs where wrong IDs are passed
- Documents your test expectations clearly

**Benefits:**
1. **Natural C# Naming**: Write tests using idiomatic camelCase
2. **Automatic Variation Handling**: Library tries multiple naming conventions
3. **Structure Validation**: Catches missing parameters, wrong paths, position mismatches
4. **Clear Error Messages**: Know exactly what went wrong and how to fix it
5. **Contract Validation**: You still verify parameter values in predicates
6. **Maintainable**: Test code is clear and readable
7. **Typo-Tolerant**: Pattern matching is flexible on parameter names but strict on structure

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
            req => req.GetPathParameter("id").ToString() == fundId.ToString()
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
            req => req.GetPathParameter("fundId").ToString() == fundId.ToString()
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
            req => req.GetPathParameter("id").ToString() == fundId.ToString()
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
            req => req.GetPathParameter("id").ToString() == fundId.ToString()
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

---

### `MockClientResponseException<T, R>()`

Mocks an exception for a single object endpoint.

**Parameters:**
- `urlTemplate` (string) - The URL pattern to match
- `exception` (Exception) - The exception to throw
- `requestInfoPredicate` (optional) - Additional matching conditions

---

### `MockClientCollectionResponseException<T, R>()`

Mocks an exception for a collection endpoint.

**Parameters:**
- `urlTemplate` (string) - The URL pattern to match
- `exception` (Exception) - The exception to throw
- `requestInfoPredicate` (optional) - Additional matching conditions

---

### `MockClientNoContentResponseException<T>()`

Mocks an exception for a no-content endpoint.

**Parameters:**
- `urlTemplate` (string) - The URL pattern to match
- `exception` (Exception) - The exception to throw
- `requestInfoPredicate` (optional) - Additional matching conditions

---

## 🔍 Troubleshooting

### KeyNotFoundException: The given key was not present in the dictionary

**Error:**
```
System.Collections.Generic.KeyNotFoundException: The given key 'id' was not present in the dictionary.
Path parameter 'id' not found in RequestInformation.PathParameters.

Tried the following naming variations:
  - id (original)
  - id (kebab-case)
  - id (URL-encoded kebab-case)
  - Id (PascalCase)

Kiota's actual URL template: {+baseurl}/api/funds/{fund-id}

Available path parameter keys:
  - baseurl
  - fund-id

To fix this, check Kiota's generated code (e.g., *RequestBuilder.cs files) to find the exact parameter name used in the URL template.
```

**Cause:** The parameter name you're using doesn't match any of the naming variations that Kiota generated.

**Solution:** The error message shows you exactly what's available. Update your mock to use the actual parameter name shown:

```csharp
// ❌ Your code tried "id"
req => req.GetPathParameter("id").ToString() == fundId.ToString()

// ✅ Use the actual parameter name from the error message
req => req.GetPathParameter("fund-id").ToString() == fundId.ToString()

// OR update your pattern to use natural naming and it will work:
_mockClient.MockClientResponse(
    "/api/funds/{fundId}",  // Use natural camelCase
    fund,
    req => req.GetPathParameter("fundId").ToString() == fundId.ToString()
    //                          ^^^^^^^^ Will find "fund-id" automatically
);
```

### Mock Not Matching / Returning Null

**Problem:** Your mock is set up but the service still returns null or throws "not configured".

**Common Causes:**

1. **Parameter name mismatch:**
   ```csharp
   // Use GetPathParameter - it tries variations automatically and gives clear errors
   mockedClient.MockClientResponse(
       "/api/funds/{fundId}",  // Use natural naming
       fund,
       req => req.GetPathParameter("fundId").ToString() == fundId.ToString()
   );
   ```

2. **URL pattern mismatch:**
   ```csharp
   // Mock: "/api/funds/{id}"
   // Actual request: "/api/funds/{id}/activities"
   // These won't match - paths must be exact
   ```

3. **Predicate always returns false:**
   ```csharp
   mockedClient.MockClientResponse(
       "/api/funds/{fundId}",
       fund,
       req => req.GetPathParameter("fundId").ToString() == wrongId  // ❌ Wrong ID value
   );
   ```

**Solution:** The `GetPathParameter()` method provides clear error messages:

```csharp
mockedClient.MockClientResponse(
    "/api/funds/{fundId}",
    fund,
    req => {
        // GetPathParameter automatically:
        // - Tries multiple naming variations
        // - Shows clear error if not found
        // - Lists available keys
        // - Shows Kiota's URL template
        var id = req.GetPathParameter("fundId");
        Console.WriteLine($"Found parameter value: {id}");
        return id.ToString() == fundId.ToString();
    }
);
```
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


