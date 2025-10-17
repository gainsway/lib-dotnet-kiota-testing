# Type-Safe Request Builder Mocking API

## Overview

The new type-safe mocking API allows you to mock Kiota HTTP client responses using the **actual request builder structure** instead of fragile URL strings. This eliminates URL pattern matching errors and provides compile-time safety.

## Benefits

### ✅ Compile-Time Safety
```csharp
// ❌ Old API - Typos and mistakes only discovered at runtime
_mockClient.MockClientResponse("/api/funds/{id}", fund);
_mockClient.MockClientResponse("/api/fundz/{id}", fund); // Typo! No error until test runs

// ✅ New API - IDE shows errors immediately
_fundClient.Api.Funds[fundId].MockGetAsync(fund);
_fundClient.Api.Fundz[fundId].MockGetAsync(fund); // Compile error: 'Fundz' doesn't exist

// ✅ Type safety - Wrong type causes compile error
var user = new User { Id = userId };
_fundClient.Api.Funds[fundId].MockGetAsync(user); 
// Compile error: Cannot convert User to Fund

// ✅ Type safety for collections too
var users = new List<User> { user };
_fundClient.Api.Funds.MockGetCollectionAsync(users);
// Compile error: Cannot convert List<User> to IEnumerable<Fund>
```

### ✅ IntelliSense Support
```csharp
// ✅ New API - IntelliSense shows available endpoints
_fundClient.Api.  // <- IDE shows: Funds, Activities, Status, etc.
```

### ✅ No URL String Matching
```csharp
// ❌ Old API - Fragile URL pattern matching
_mockClient.MockClientResponse("/api/funds/{id}", fund, req => 
    req.PathParameters["id"].ToString() == fundId.ToString());

// ✅ New API - Uses actual API structure
_fundClient.Api.Funds[fundId].MockGetAsync(fund);
// URL template and path parameters handled automatically!
```

### ✅ Clear Error Messages
```csharp
// ❌ Old API - Generic "fund not found" errors with no indication why
// Test fails: "Expected fund to not be null, but it was"

// ✅ New API - Type system catches mistakes immediately
_fundClient.Api.Funds["wrong-id"].MockGetAsync(fund);
// If test fails, you know exactly which mock is wrong
```

### ✅ Self-Documenting Code
```csharp
// ✅ New API - Code shows exact API being mocked
_fundClient.Api.Funds[fundId].Activities[activityId].MockGetAsync(activity);
// Clearly shows: GET /api/funds/{fundId}/activities/{activityId}
```

## API Reference

### Basic Usage

```csharp
// Setup
var mockClient = KiotaClientMockExtensions.GetMockableClient<MyKiotaClient>();

// Mock GET request returning a single object
// ✅ Type-safe: Can only pass Fund, not User or other types
mockClient.Api.Funds[fundId].MockGetAsync(expectedFund);

// Mock GET request returning a string
mockClient.Api.Status.MockGetAsync("OK");

// Mock GET request returning a collection
// ✅ Type-safe: Can only pass IEnumerable<Fund>, not IEnumerable<User>
var funds = new List<Fund> { fund1, fund2 };
mockClient.Api.Funds.MockGetCollectionAsync(funds);

// ⚠️ Common mistake: Don't use MockGetAsync for collections!
// ❌ Wrong - Compile error
mockClient.Api.Funds.MockGetAsync(funds); 
// Error: Cannot convert List<Fund> to Fund

// ✅ Correct - Use MockGetCollectionAsync
mockClient.Api.Funds.MockGetCollectionAsync(funds);

// Mock POST request
mockClient.Api.Funds.MockPostAsync(createdFund);

// Mock PUT request
mockClient.Api.Funds[fundId].MockPutAsync(updatedFund);

// Mock PATCH request
mockClient.Api.Funds[fundId].MockPatchAsync(patchedFund);

// Mock DELETE request
mockClient.Api.Funds[fundId].MockDeleteAsync();
```

### Exception Mocking

```csharp
// Mock an exception
var exception = new InvalidOperationException("Fund not found");
mockClient.Api.Funds[fundId]
    .MockGetAsyncException<FundRequestBuilder, Fund>(exception);

// Now when the code calls the API, it will throw the exception
// await _fundClient.Api.Funds[fundId].GetAsync(); // Throws InvalidOperationException
```

### Predicate-Based Mocking

```csharp
// Mock based on request headers
mockClient.Api.Funds[fundId].MockGetAsync(
    authorizedFund,
    req => req.Headers.ContainsKey("Authorization")
);

// Mock based on query parameters
mockClient.Api.Funds.MockGetCollectionAsync(
    funds,
    req => req.QueryParameters.ContainsKey("filter")
);

// Complex predicates
mockClient.Api.Funds[fundId].MockGetAsync(
    fund,
    req => req.Headers.ContainsKey("Authorization") 
        && req.Method == Method.GET
        && req.PathParameters["id"].ToString() == fundId.ToString()
);
```

### Multiple IDs

```csharp
// Mock different responses for different IDs
mockClient.Api.Funds["fund-1"].MockGetAsync(fund1);
mockClient.Api.Funds["fund-2"].MockGetAsync(fund2);

// Each ID gets its own mock - exact path parameter matching!
```

## How It Works

The type-safe API uses **reflection** to extract the URL template and path parameters from the request builder, then sets up the mock to match requests with:

1. **Same HTTP method** (GET, POST, etc.)
2. **Same URL template** (exact string match, case-insensitive)
3. **Same path parameter values** (e.g., `id=123`)
4. **Optional predicate** (for headers, query params, etc.)

### Example

```csharp
// When you call:
_fundClient.Api.Funds["123"].MockGetAsync(fund);

// The library:
// 1. Extracts URL template: "{+baseurl}/api/funds/{id}"
// 2. Extracts path parameters: { "id": "123" }
// 3. Sets up mock to match:
//    - Method: GET
//    - URL template: "{+baseurl}/api/funds/{id}"
//    - PathParameters["id"]: "123"
```

## Migration Guide

### Old API (Still Supported)

```csharp
// ❌ String-based URL patterns
_mockClient.MockClientResponse(
    "/api/funds/{id}", 
    fund,
    req => req.PathParameters["id"].ToString() == fundId.ToString()
);

_mockClient.MockClientResponse(
    "/api/funds/{fundId}/activities/{activityId}",
    activity,
    req => req.PathParameters["fundId"].ToString() == fundId.ToString()
        && req.PathParameters["activityId"].ToString() == activityId.ToString()
);
```

### New API (Recommended)

```csharp
// ✅ Type-safe request builder API
_fundClient.Api.Funds[fundId].MockGetAsync(fund);

_fundClient.Api.Funds[fundId]
    .Activities[activityId]
    .MockGetAsync(activity);
```

### Migration Steps

1. **Identify the request builder** for your Kiota client
2. **Replace URL strings** with request builder navigation
3. **Remove manual predicates** (path parameters handled automatically)
4. **Test!** The new API is more reliable and catches errors earlier

## Test Examples

```csharp
[Test]
public async Task GetFund_WithValidId_ReturnsExpectedFund()
{
    // Arrange
    var fundId = Guid.NewGuid();
    var expectedFund = new Fund { Id = fundId, Name = "Test Fund" };
    
    // ✅ Type-safe mock setup
    _mockClient.Api.Funds[fundId.ToString()].MockGetAsync(expectedFund);
    
    // Act
    var service = new FundService(_mockClient);
    var result = await service.GetFundAsync(fundId);
    
    // Assert
    Assert.That(result, Is.Not.Null);
    Assert.That(result.Id, Is.EqualTo(fundId));
    Assert.That(result.Name, Is.EqualTo("Test Fund"));
}

[Test]
public async Task GetFund_WithInvalidId_ThrowsException()
{
    // Arrange
    var fundId = Guid.NewGuid();
    var expectedException = new InvalidOperationException("Fund not found");
    
    // ✅ Type-safe exception mock
    _mockClient.Api.Funds[fundId.ToString()]
        .MockGetAsyncException<FundRequestBuilder, Fund>(expectedException);
    
    // Act & Assert
    var service = new FundService(_mockClient);
    Assert.ThrowsAsync<InvalidOperationException>(
        async () => await service.GetFundAsync(fundId)
    );
}

[Test]
public async Task GetActivities_WithMultipleFunds_ReturnsCorrectActivities()
{
    // Arrange
    var fund1Id = "fund-1";
    var fund2Id = "fund-2";
    
    var fund1Activities = new List<Activity> 
    { 
        new() { Id = "activity-1" } 
    };
    var fund2Activities = new List<Activity> 
    { 
        new() { Id = "activity-2" } 
    };
    
    // ✅ Type-safe mock setup - different data for different IDs
    _mockClient.Api.Funds[fund1Id].Activities.MockGetCollectionAsync(fund1Activities);
    _mockClient.Api.Funds[fund2Id].Activities.MockGetCollectionAsync(fund2Activities);
    
    // Act
    var service = new FundService(_mockClient);
    var result1 = await service.GetActivitiesAsync(fund1Id);
    var result2 = await service.GetActivitiesAsync(fund2Id);
    
    // Assert
    Assert.That(result1, Has.Count.EqualTo(1));
    Assert.That(result1.First().Id, Is.EqualTo("activity-1"));
    
    Assert.That(result2, Has.Count.EqualTo(1));
    Assert.That(result2.First().Id, Is.EqualTo("activity-2"));
}
```

## Advanced Usage

### Chaining with Predicates

```csharp
// Mock authenticated requests
_fundClient.Api.Funds[fundId].MockGetAsync(
    authenticatedFund,
    req => req.Headers.ContainsKey("Authorization")
);

// Mock unauthenticated requests (different response)
_fundClient.Api.Funds[fundId].MockGetAsync(publicFund);
```

### Complex Request Builders

```csharp
// Nested resources
_fundClient.Api
    .Funds[fundId]
    .Activities[activityId]
    .Documents[documentId]
    .MockGetAsync(document);

// Actions/operations
_fundClient.Api
    .Funds[fundId]
    .Activities[activityId]
    .Modify
    .MockPostAsync(modifiedActivity);
```

### Method Variants

```csharp
// All HTTP methods supported
builder.MockGetAsync<TBuilder, TResponse>(response);
builder.MockGetAsync<TBuilder>(stringResponse);
builder.MockGetCollectionAsync<TBuilder, TResponse>(collection);
builder.MockPostAsync<TBuilder, TResponse>(response);
builder.MockPutAsync<TBuilder, TResponse>(response);
builder.MockPatchAsync<TBuilder, TResponse>(response);
builder.MockDeleteAsync<TBuilder>();

// Exception variants
builder.MockGetAsyncException<TBuilder, TResponse>(exception);
builder.MockDeleteAsyncException<TBuilder>(exception);
```

## Backward Compatibility

**The old API is still supported!** You can gradually migrate to the new type-safe API:

```csharp
// ✅ Old API still works
_mockClient.MockClientResponse("/api/funds/{id}", fund);

// ✅ New API available for new tests
_fundClient.Api.Funds[fundId].MockGetAsync(fund);

// ✅ Both can coexist in the same test suite
```

## FAQ

### Q: What if my Kiota client doesn't follow this structure?

**A:** The new API works with any Kiota-generated client. Just use your actual request builder structure:

```csharp
// Your client structure
_myClient.V1.Users[userId].MockGetAsync(user);
_myClient.Admin.Settings.MockGetAsync(settings);
```

### Q: Can I use both old and new APIs together?

**A:** Yes! They're completely independent. Use the new API for new code and migrate gradually.

### Q: Does this change how Kiota clients work?

**A:** No! This only affects **test mocking**. Your actual Kiota client code remains unchanged.

### Q: What about query parameters?

**A:** Use predicates for query parameters:

```csharp
_fundClient.Api.Funds.MockGetCollectionAsync(
    funds,
    req => req.QueryParameters["filter"] == "active"
);
```

### Q: How do I test error scenarios?

**A:** Use the exception mocking methods:

```csharp
_fundClient.Api.Funds[fundId]
    .MockGetAsyncException<FundRequestBuilder, Fund>(
        new HttpException(404, "Not Found")
    );
```

## Implementation Details

The new API is implemented in `RequestBuilderMockExtensions.cs` and uses:

- **Reflection** to access protected `UrlTemplate` and `PathParameters` properties
- **Exact matching** on URL template and path parameter values
- **NSubstitute** to set up the underlying request adapter mocks
- **Generic constraints** to ensure type safety at compile time

## Next Steps

1. ✅ Start using the new API for new tests
2. ✅ Gradually migrate existing tests when making changes
3. ✅ Enjoy the benefits of compile-time safety and clearer error messages!

## Support

For issues or questions:
- Check the test examples in `RequestBuilderMockExtensionsTests.cs`
- Review the implementation in `RequestBuilderMockExtensions.cs`
- The old API documentation is still available in the main README

---

**Welcome to type-safe Kiota mocking! 🎉**
