# NSwag API client testing

This repository now builds **Gainsway.NSwag.Testing**, an HTTP-level test
helper for generated NSwag C# clients. The previous generator-specific request
adapters and dependencies have been removed.

## Installation

```sh
dotnet add package Gainsway.NSwag.Testing
```

## Example

```csharp
using Gainsway.NSwag.Testing;

var client = NswagClientMockExtensions.GetMockableClient<AccountServiceClient>();

client.MockGetAsync($"/api/accounts/{accountId}", new AccountDto { Id = accountId });
var account = await new AccountsService(client).GetAccount(accountId);

await client.VerifyGetAsync($"/api/accounts/{accountId}", Times.Once);
```

For writes, use the familiar awaitable `Verify*Async` and `WithBody` pattern:

```csharp
client.MockPostAsync("/api/accounts");  // 204 response
await service.CreateAccount(accountInput);

await client.VerifyPostAsync("/api/accounts", Times.Once)
    .WithBody<AccountCreateDto>(body => body.Name == accountInput.Name);
```

Other helpers: `MockGetCollectionAsync`, `MockPostAsync`, `MockPutAsync`,
`MockPatchAsync`, `MockDeleteAsync`, `MockGetError`,
`MockPostError`, `GetMockRequests`, `Times.Once`, `Times.Never`,
`Times.Exactly(n)`. Mock rules match HTTP method, path (including
`{parameter}` placeholders) and optional query string. Last matching rule
wins. JSON responses are serialized using Newtonsoft.Json and camelCase
properties, matching NSwag's default serialization behavior.

`GetMockableClient<T>()` constructs a real generated client with an injected
in-memory `HttpClient`. No authentication network calls or live service
requests are made. The helper captures HTTP requests for later verification.

See [MIGRATION.md](MIGRATION.md) for changes required in service tests.
