# Migrating service tests to NSwag

The previous request-builder-based test API cannot work with NSwag, because
NSwag generates service methods rather than fluent request-builder objects.
The replacement deliberately preserves the testing *workflow* and most
method names, while matching actual HTTP requests.

## Before / after

```csharp
// Before
using Gainsway.Kiota.Testing;

var client = KiotaClientMockExtensions.GetMockableClient<AccountServiceClient>();
client.Api.Accounts[accountId].MockGetAsync(account);
await client.Api.Accounts[accountId].VerifyGetAsync(Times.Once);
```

```csharp
// After
using Gainsway.NSwag.Testing;

var client = NswagClientMockExtensions.GetMockableClient<AccountServiceClient>();
client.MockGetAsync($"/api/accounts/{accountId}", account);
await client.VerifyGetAsync($"/api/accounts/{accountId}", Times.Once);
```

Likewise, replace `client.Api...MockPostAsync(response)` with
`client.MockPostAsync("/api/route", response)`; the same applies to GET
collections, PUT, PATCH, DELETE and exception mocks.

Use `{id}` placeholders for reusable path mocks; the same method and
number of segments must match. Optional query strings are matched when
included in the mock path. `GetMockRequests()` exposes exact URIs and
serialized JSON payloads for custom assertions.

The familiar `Times.Once`, `Times.Never`, `Times.Exactly(n)` and
`VerifyPostAsync(path).WithBody<T>(predicate)` patterns are preserved.

## Error and no-content responses

```csharp
client.MockPostAsync("/api/items"); // HTTP 204
client.MockGetError("/api/items/404", HttpStatusCode.NotFound);
client.MockGetAsync("/api/items/5", new HttpRequestException("offline"));
```

An HTTP error response is normally preferable to throwing a generator-specific
exception: the generated NSwag client will raise its own typed `ApiException`
when it sees the error status.

## Package change

Use `Gainsway.NSwag.Testing` instead of
`Gainsway.Kiota.Testing`. Delete old test code referencing request builders,
`IRequestAdapter`, `RequestInformation` or Kiota serialization helpers.
The test library does not reference or transitively install Kiota.
