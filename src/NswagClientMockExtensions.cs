using System.Net;
using System.Runtime.CompilerServices;

namespace Gainsway.NSwag.Testing;

/// <summary>
/// HTTP-level mocking for generated NSwag clients with an injected HttpClient.
/// </summary>
public static class NswagClientMockExtensions
{
    private static readonly ConditionalWeakTable<object, MockHttpMessageHandler> Handlers = new();

    /// <summary>
    /// Creates an actual generated client with an in-memory HTTP transport.
    /// </summary>
    public static T GetMockableClient<T>(string baseUrl = "https://mock.liquid.local/")
        where T : class
    {
        var handler = new MockHttpMessageHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri(baseUrl) };
        try
        {
            var client =
                Activator.CreateInstance(typeof(T), httpClient) as T
                ?? throw new InvalidOperationException(
                    $"{typeof(T).FullName} must expose a public HttpClient constructor."
                );
            Handlers.Add(client, handler);
            return client;
        }
        catch
        {
            httpClient.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Returns captured HTTP requests, including raw JSON request bodies.
    /// </summary>
    public static IReadOnlyList<RecordedRequest> GetMockRequests<T>(this T client)
        where T : class => GetHandler(client).GetRequests();

    private static MockHttpMessageHandler GetHandler(object client) =>
        Handlers.TryGetValue(client, out var handler)
            ? handler
            : throw new InvalidOperationException(
                "Client was not created with NswagClientMockExtensions.GetMockableClient<T>()."
            );

    private static T Respond<T>(
        T client,
        HttpMethod method,
        string path,
        object? response,
        Func<RecordedRequest, bool>? predicate
    )
        where T : class
    {
        if (response is Exception exception)
        {
            GetHandler(client).AddException(method, path, exception, predicate);
            return client;
        }

        GetHandler(client).AddResponse(method, path, response, HttpStatusCode.OK, predicate);
        return client;
    }

    private static T RespondError<T>(
        T client,
        HttpMethod method,
        string path,
        HttpStatusCode status,
        object? error
    )
        where T : class
    {
        GetHandler(client).AddResponse(method, path, error, status, null);
        return client;
    }

    private static T RespondEmpty<T>(T client, HttpMethod method, string path)
        where T : class
    {
        GetHandler(client).AddNoContent(method, path, null);
        return client;
    }

    private static T Fail<T>(T client, HttpMethod method, string path, Exception exception)
        where T : class
    {
        GetHandler(client).AddException(method, path, exception, null);
        return client;
    }

    private static VerifyCallAssertion Verify<T>(
        T client,
        HttpMethod method,
        string path,
        Times? times,
        Func<RecordedRequest, bool>? predicate
    )
        where T : class =>
        new(GetHandler(client), method, path, times ?? Times.AtLeastOnce, predicate);

    public static T MockGetAsync<T, TResponse>(
        this T client,
        string path,
        TResponse? response,
        Func<RecordedRequest, bool>? predicate = null
    )
        where T : class => Respond(client, HttpMethod.Get, path, response, predicate);

    public static T MockGetAsync<T>(this T client, string path)
        where T : class => RespondEmpty(client, HttpMethod.Get, path);

    public static T MockGetAsync<T>(this T client, string path, Exception exception)
        where T : class => Fail(client, HttpMethod.Get, path, exception);

    public static T MockGetError<T>(
        this T client,
        string path,
        HttpStatusCode status,
        object? body = null
    )
        where T : class => RespondError(client, HttpMethod.Get, path, status, body);

    public static VerifyCallAssertion VerifyGetAsync<T>(
        this T client,
        string path,
        Times? times = null,
        Func<RecordedRequest, bool>? predicate = null
    )
        where T : class => Verify(client, HttpMethod.Get, path, times, predicate);

    public static T MockPostAsync<T, TResponse>(
        this T client,
        string path,
        TResponse? response,
        Func<RecordedRequest, bool>? predicate = null
    )
        where T : class => Respond(client, HttpMethod.Post, path, response, predicate);

    public static T MockPostAsync<T>(this T client, string path)
        where T : class => RespondEmpty(client, HttpMethod.Post, path);

    public static T MockPostAsync<T>(this T client, string path, Exception exception)
        where T : class => Fail(client, HttpMethod.Post, path, exception);

    public static T MockPostError<T>(
        this T client,
        string path,
        HttpStatusCode status,
        object? body = null
    )
        where T : class => RespondError(client, HttpMethod.Post, path, status, body);

    public static VerifyCallAssertion VerifyPostAsync<T>(
        this T client,
        string path,
        Times? times = null,
        Func<RecordedRequest, bool>? predicate = null
    )
        where T : class => Verify(client, HttpMethod.Post, path, times, predicate);

    public static T MockPutAsync<T, TResponse>(
        this T client,
        string path,
        TResponse? response,
        Func<RecordedRequest, bool>? predicate = null
    )
        where T : class => Respond(client, HttpMethod.Put, path, response, predicate);

    public static T MockPutAsync<T>(this T client, string path)
        where T : class => RespondEmpty(client, HttpMethod.Put, path);

    public static T MockPutAsync<T>(this T client, string path, Exception exception)
        where T : class => Fail(client, HttpMethod.Put, path, exception);

    public static T MockPutError<T>(
        this T client,
        string path,
        HttpStatusCode status,
        object? body = null
    )
        where T : class => RespondError(client, HttpMethod.Put, path, status, body);

    public static VerifyCallAssertion VerifyPutAsync<T>(
        this T client,
        string path,
        Times? times = null,
        Func<RecordedRequest, bool>? predicate = null
    )
        where T : class => Verify(client, HttpMethod.Put, path, times, predicate);

    public static T MockPatchAsync<T, TResponse>(
        this T client,
        string path,
        TResponse? response,
        Func<RecordedRequest, bool>? predicate = null
    )
        where T : class => Respond(client, HttpMethod.Patch, path, response, predicate);

    public static T MockPatchAsync<T>(this T client, string path)
        where T : class => RespondEmpty(client, HttpMethod.Patch, path);

    public static T MockPatchAsync<T>(this T client, string path, Exception exception)
        where T : class => Fail(client, HttpMethod.Patch, path, exception);

    public static T MockPatchError<T>(
        this T client,
        string path,
        HttpStatusCode status,
        object? body = null
    )
        where T : class => RespondError(client, HttpMethod.Patch, path, status, body);

    public static VerifyCallAssertion VerifyPatchAsync<T>(
        this T client,
        string path,
        Times? times = null,
        Func<RecordedRequest, bool>? predicate = null
    )
        where T : class => Verify(client, HttpMethod.Patch, path, times, predicate);

    public static T MockDeleteAsync<T, TResponse>(
        this T client,
        string path,
        TResponse? response,
        Func<RecordedRequest, bool>? predicate = null
    )
        where T : class => Respond(client, HttpMethod.Delete, path, response, predicate);

    public static T MockDeleteAsync<T>(this T client, string path)
        where T : class => RespondEmpty(client, HttpMethod.Delete, path);

    public static T MockDeleteAsync<T>(this T client, string path, Exception exception)
        where T : class => Fail(client, HttpMethod.Delete, path, exception);

    public static T MockDeleteError<T>(
        this T client,
        string path,
        HttpStatusCode status,
        object? body = null
    )
        where T : class => RespondError(client, HttpMethod.Delete, path, status, body);

    public static VerifyCallAssertion VerifyDeleteAsync<T>(
        this T client,
        string path,
        Times? times = null,
        Func<RecordedRequest, bool>? predicate = null
    )
        where T : class => Verify(client, HttpMethod.Delete, path, times, predicate);

    public static T MockGetCollectionAsync<T, TItem>(
        this T client,
        string path,
        IEnumerable<TItem>? response,
        Func<RecordedRequest, bool>? predicate = null
    )
        where T : class => Respond(client, HttpMethod.Get, path, response, predicate);
}
