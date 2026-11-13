using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Gainsway.NSwag.Testing;

internal sealed class MockHttpMessageHandler : HttpMessageHandler
{
    private sealed record Rule(
        HttpMethod Method,
        string Path,
        Func<RecordedRequest, bool>? Predicate,
        Func<HttpResponseMessage> Respond
    );

    private readonly object _sync = new();
    private readonly List<Rule> _rules = [];
    private readonly List<RecordedRequest> _requests = [];

    internal static readonly JsonSerializerSettings SerializerSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
    };

    public void AddResponse(
        HttpMethod method,
        string path,
        object? response,
        HttpStatusCode statusCode,
        Func<RecordedRequest, bool>? predicate
    )
    {
        var json = JsonConvert.SerializeObject(response, SerializerSettings);
        Add(
            method,
            path,
            predicate,
            () =>
                new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                }
        );
    }

    public void AddNoContent(
        HttpMethod method,
        string path,
        Func<RecordedRequest, bool>? predicate
    ) => Add(method, path, predicate, () => new HttpResponseMessage(HttpStatusCode.NoContent));

    public void AddException(
        HttpMethod method,
        string path,
        Exception exception,
        Func<RecordedRequest, bool>? predicate
    ) => Add(method, path, predicate, () => throw exception);

    public IReadOnlyList<RecordedRequest> GetRequests()
    {
        lock (_sync)
        {
            return _requests.ToArray();
        }
    }

    public IReadOnlyList<RecordedRequest> Matching(
        HttpMethod method,
        string path,
        Func<RecordedRequest, bool>? predicate
    )
    {
        lock (_sync)
        {
            return _requests
                .Where(r => Matches(r, method, path) && (predicate is null || predicate(r)))
                .ToArray();
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        var requestUri =
            request.RequestUri ?? throw new InvalidOperationException("Request has no URI.");
        var headers = request.Headers.ToDictionary(x => x.Key, x => x.Value.ToArray());
        if (request.Content is not null)
        {
            foreach (var header in request.Content.Headers)
            {
                headers[header.Key] = header.Value.ToArray();
            }
        }

        var recorded = new RecordedRequest(
            request.Method,
            requestUri,
            request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken),
            headers
        );

        Func<HttpResponseMessage>? responder = null;
        lock (_sync)
        {
            _requests.Add(recorded);
            for (var index = _rules.Count - 1; index >= 0; index--)
            {
                var rule = _rules[index];
                if (
                    Matches(recorded, rule.Method, rule.Path)
                    && (rule.Predicate is null || rule.Predicate(recorded))
                )
                {
                    responder = rule.Respond;
                    break;
                }
            }
        }

        return responder?.Invoke()
            ?? new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(
                    $"No HTTP mock for {request.Method} {requestUri}",
                    Encoding.UTF8,
                    "text/plain"
                ),
            };
    }

    private void Add(
        HttpMethod method,
        string path,
        Func<RecordedRequest, bool>? predicate,
        Func<HttpResponseMessage> responder
    )
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path must not be empty.", nameof(path));
        }

        lock (_sync)
        {
            _rules.Add(new Rule(method, path, predicate, responder));
        }
    }

    private static bool Matches(RecordedRequest request, HttpMethod method, string path)
    {
        if (request.Method != method)
        {
            return false;
        }

        var querySeparator = path.IndexOf('?');
        var expectedPath = querySeparator < 0 ? path : path[..querySeparator];
        var expectedQuery = querySeparator < 0 ? null : path[(querySeparator + 1)..];
        if (expectedQuery is not null && request.Query.TrimStart('?') != expectedQuery)
        {
            return false;
        }

        var expectedSegments = Uri.UnescapeDataString(expectedPath)
            .Trim('/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries);
        var actualSegments = Uri.UnescapeDataString(request.Path)
            .Trim('/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (expectedSegments.Length != actualSegments.Length)
        {
            return false;
        }

        for (var index = 0; index < expectedSegments.Length; index++)
        {
            var expected = expectedSegments[index];
            if (expected.Length > 2 && expected.StartsWith('{') && expected.EndsWith('}'))
            {
                continue;
            }

            if (!string.Equals(expected, actualSegments[index], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}
