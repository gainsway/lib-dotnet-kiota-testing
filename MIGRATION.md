# Migration Guide: Removing the Legacy URL-Based Mocking API

This release removes the legacy, string-based mocking API entirely. It was
never type-safe — URL patterns and path-parameter names were plain strings
matched with `EndsWith`, so typos, renamed parameters, and path changes in
the generated Kiota client only surfaced as runtime test failures, not
compile errors. The type-safe request-builder API (`MockGetAsync`,
`MockPostAsync`, `VerifyGetAsync`, etc.) has been the recommended way to use
this library for a while; this release makes it the *only* way.

If you're already using the type-safe API exclusively, this release is a
drop-in upgrade with one addition worth knowing about — see
[New: request body verification](#new-request-body-verification) below.
If any tests still call the legacy methods, they will no longer compile.

## What was removed

From `KiotaClientMockExtensions`:

- `MockClientResponse<T, R>(urlTemplate, returnObject, predicate?)`
- `MockClientResponse<T>(urlTemplate, returnValue, predicate?)` (string overload)
- `MockClientCollectionResponse<T, R>(urlTemplate, returnObject, predicate?)`
- `MockClientNoContentResponse<T>(urlTemplate, predicate?)`
- `MockClientResponseException<T, R>(urlTemplate, exception, predicate?)`
- `MockClientResponseException<T>(urlTemplate, exception, predicate?)` (string overload)
- `MockClientCollectionResponseException<T, R>(urlTemplate, exception, predicate?)`
- `MockClientNoContentResponseException<T>(urlTemplate, exception, predicate?)`
- `GetUrlTemplate<T>(requestBuilder)`

`GetMockableClient<T>()` and `GetMockAdapter<T>()` are unchanged and still
the entry point for both mocking and verification.

Two test files that existed solely to exercise the legacy API were deleted:
`test/IntegrationTests.cs` and `test/NewApiPathPattern.Tests.cs`. Their
type-safe equivalents already existed in
`test/RequestBuilderMockExtensions.Tests.cs`.

## Migration steps

### 1. Replace `MockClientResponse` and friends with the request-builder API

The mechanical change is: instead of a URL string plus a predicate that
manually checks path parameters, navigate the actual generated request
builder and let the library extract the URL template and path parameters
for you.

```csharp
// Before
_mockClient.MockClientResponse(
    "/api/funds/{id}",
    fund,
    req => req.PathParameters["id"].ToString() == fundId.ToString()
);

// After
_mockClient.Api.Funds[fundId].MockGetAsync(fund);
```

| Legacy call | Type-safe replacement |
|---|---|
| `MockClientResponse(url, obj, predicate?)` | `builder.MockGetAsync(obj, predicate?)` (or `MockPostAsync`/`MockPutAsync`/`MockPatchAsync`/`MockDeleteAsync` — pick the method matching the HTTP verb) |
| `MockClientResponse(url, "string", predicate?)` | `builder.MockGetAsync("string", predicate?)` |
| `MockClientCollectionResponse(url, items, predicate?)` | `builder.MockGetCollectionAsync(items, predicate?)` (or `MockPostCollectionAsync`/`MockDeleteCollectionAsync`) |
| `MockClientNoContentResponse(url, predicate?)` | `builder.MockDeleteAsync(predicate?)` / `MockPostAsync(predicate?)` / `MockPutAsync(predicate?)` / `MockPatchAsync(predicate?)` (no-content overloads) |
| `MockClientResponseException(url, ex, predicate?)` | `builder.MockGetAsyncException<TBuilder, TResponse>(ex, predicate?)` |
| `MockClientCollectionResponseException(url, ex, predicate?)` | `builder.MockGetCollectionAsyncException<TBuilder, TResponse>(ex, predicate?)` |
| `MockClientNoContentResponseException(url, ex, predicate?)` | `builder.MockDeleteAsyncException<TBuilder>(ex, predicate?)` |

If your predicate only checked path parameters (the overwhelmingly common
case), delete it — the request builder already carries the exact path
parameter values, so the type-safe `Mock*` methods match on them
automatically. Keep the predicate only if it also checks something the
builder can't know about, like headers or query parameters.

```csharp
// Before — predicate exists only to check the path parameter
_mockClient.MockClientResponse(
    "/api/funds/{fundId}/activities/{activityId}",
    activity,
    req => req.GetPathParameter("fundId").ToString() == fundId.ToString()
        && req.GetPathParameter("activityId").ToString() == activityId.ToString()
);

// After — the builder's own indexers already encode both IDs
_mockClient.Api.Funds[fundId].Activities[activityId].MockGetAsync(activity);

// Before — predicate also checks a header, so it survives the migration
_mockClient.MockClientResponse(
    "/api/funds/{id}",
    fund,
    req => req.PathParameters["id"].ToString() == fundId.ToString()
        && req.Headers.ContainsKey("Authorization")
);

// After
_mockClient.Api.Funds[fundId].MockGetAsync(
    fund,
    req => req.Headers.ContainsKey("Authorization")
);
```

### 2. Replace `GetUrlTemplate<T>()`

If you called `GetUrlTemplate()` for debugging or to feed a legacy
`MockClientResponse` call, you no longer need it for the latter — the
request builder handles URL templates internally now. For debugging, read
the template off a captured `RequestInformation` instead:

```csharp
// Before
var urlTemplate = KiotaClientMockExtensions.GetUrlTemplate(mockClient.Api.Funds[fundId]);

// After — construct a RequestInformation the same way the request builder would,
// or capture one from a mock call, then normalize it
req.NormalizeUrlTemplate();
```

`NormalizeUrlTemplate` remains public on `KiotaClientMockExtensions` for
this kind of introspection; only the legacy-specific `EndsWith` matching
helpers were removed since nothing else used them.

### 3. Update verification calls

Independently of the legacy-API removal, `Verify*Async` was collapsed from
one method per response shape down to exactly one per HTTP verb. If you were
already on the type-safe API, this is the other breaking change in this
release:

```csharp
// Before
await mockClient.Api.Funds[fundId].VerifyGetAsync<FundItemRequestBuilder, Fund>(Times.Once);
await mockClient.Api.Status.VerifyGetAsync<StatusRequestBuilder>(Times.Once);
await mockClient.Api.Funds[fundId].Activities
    .VerifyGetCollectionAsync<ActivitiesRequestBuilder, Activity>(Times.Once);
await mockClient.Api.Funds.VerifyPostCollectionAsync<FundsRequestBuilder, Fund>(Times.Once);
await mockClient.Api.Funds.VerifyDeleteCollectionAsync<FundsRequestBuilder, Fund>(Times.Once);

// After — drop the response-type argument entirely; one method matches any shape
await mockClient.Api.Funds[fundId].VerifyGetAsync(Times.Once);
await mockClient.Api.Status.VerifyGetAsync(Times.Once);
await mockClient.Api.Funds[fundId].Activities.VerifyGetAsync(Times.Once);
await mockClient.Api.Funds.VerifyPostAsync(Times.Once);
await mockClient.Api.Funds.VerifyDeleteAsync(Times.Once);
```

Remove the `<TBuilder, TResponse>` (or `<TBuilder>` for the old primitive/
no-content overloads) type argument from every `Verify*Async` call — the
builder type is now inferred from the call site and there is no response
type argument at all. The removed method names
(`VerifyGetCollectionAsync`, `VerifyPostCollectionAsync`,
`VerifyDeleteCollectionAsync`) collapse into the base verb method
(`VerifyGetAsync`, `VerifyPostAsync`, `VerifyDeleteAsync`), since response
shape is no longer part of what's being matched.

### New: request body verification

Previously there was no way to assert on the *contents* of a request body
via `Verify*` — only that a call happened, optionally narrowed by a
predicate over the raw `RequestInformation` (e.g. `req.Content != null`).
`VerifyPutAsync`/`VerifyPostAsync`/`VerifyPatchAsync` now return a
`VerifyCallAssertion`, which is directly awaitable for the same count-only
check as before, or chainable with `.WithBody<TBody>(predicate)` to also
deserialize the request body and assert on it:

```csharp
await mockClient.ApiInternal.Funds[fundId]
    .VerifyPutAsync(Times.Once)
    .WithBody<FundUpdateDto>(body => body.RequiresMarket == true);
```

This requires the mock client to have been created via
`GetMockableClient<T>()`, which now wires a real
`JsonSerializationWriterFactory` into the mock adapter so that
`RequestInformation.Content` is actually populated when generated code calls
`SetContentFromParsable`. If you construct the mock adapter yourself instead
of going through `GetMockableClient<T>()`, `.WithBody` will always throw
`ReceivedCallsException` because `Content` stays `null`.

## Why this change

The legacy API's `EndsWith`-based URL matching was originally added for
backward compatibility with early consumers, but it undermined the main
value proposition of this library: catching contract drift (renamed
parameters, changed paths) at compile time when the Kiota client is
regenerated, rather than as a runtime "mock not matching" failure with no
indication of why. Every caller of this library has been on the type-safe
API for some time; keeping the legacy code around only added maintenance
surface and a second recommended way to do the same thing in the docs.

## See also

- [README.md](README.md) for full usage documentation of the current API.
