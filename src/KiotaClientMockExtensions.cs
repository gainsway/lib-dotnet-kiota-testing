using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using NSubstitute;

namespace Gainsway.Kiota.Testing;

/// <summary>
/// Provides extension methods for mocking Kiota-generated client classes and their responses.
/// </summary>
public static class KiotaClientMockExtensions
{
    /// <summary>
    /// Helper method that performs URL template matching without normalizing parameter names to wildcards.
    /// Matches exact path structure while preserving parameter names.
    /// </summary>
    private static bool MatchesUrlTemplate(
        RequestInformation req,
        string normalizedPattern,
        string originalPattern
    )
    {
        if (string.IsNullOrEmpty(req.UrlTemplate))
        {
            return false;
        }

        var normalizedRequest = NormalizeUrlTemplate(req.UrlTemplate);
        return normalizedRequest.Equals(normalizedPattern, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Creates a predicate expression to match a <see cref="RequestInformation"/> object
    /// based on its URL template matching the specified pattern.
    /// </summary>
    /// <param name="urlTemplate">The URL template to match (e.g., "/api/funds/{fundId}").</param>
    /// <returns>An expression that evaluates to true if the URL template matches.</returns>
    /// <remarks>
    /// This method handles Kiota's URL template format which includes:
    /// - {+baseurl} prefix (stripped for matching)
    /// - Query parameter templates like {?param1,param2} (stripped for matching)
    /// - URL-encoded parameter names like {fund%2Did} which is {fund-id}
    ///
    /// Matching strategy:
    /// 1. Strip {+baseurl} prefix from the request's URL template
    /// 2. Strip query parameter templates {?...}
    /// 3. Normalize parameter name encoding (e.g., {fund%2Did} -> {fund-id})
    /// 4. Compare the normalized paths
    ///
    /// Parameter Name Handling:
    /// Unlike previous versions, this library does NOT normalize parameter names to wildcards.
    /// Instead, it preserves the specific parameter names in the URL pattern and provides
    /// smart parameter access methods that try multiple naming conventions automatically.
    ///
    /// When accessing path parameters in predicates, use the extension methods:
    /// - req.TryGetPathParameter("fundId", out var id) - Safe, returns bool
    /// - req.GetPathParameter("fundId") - Throws descriptive exception if not found
    ///
    /// These methods automatically try variations like: fundId, fund-id, fund%2Did, FundId
    ///
    /// Examples:
    /// - Pattern "/api/funds/{fundId}" matches "{+baseurl}/api/funds/{fund-id}"
    /// - Pattern "/api/funds/{fundId}" matches "{+baseurl}/api/funds/{fund%2Did}"
    /// - Pattern "/api/funds/{fundId}/activities/{activityId}" matches "{+baseurl}/api/funds/{fund-id}/activities/{activity-id}"
    ///
    /// The URL structure must match exactly - different endpoints require separate mocks:
    /// - "/api/funds/{id}" will NOT match "/api/funds/{id}/activities"
    /// </remarks>
    private static Expression<Predicate<RequestInformation>> RequestInformationUrlTemplatePredicate(
        string urlTemplate
    )
    {
        // Normalize the user-provided pattern (strips baseurl, query params, normalizes encoding)
        var normalizedPattern = NormalizeUrlTemplate(urlTemplate);

        return req => MatchesUrlTemplate(req, normalizedPattern, urlTemplate);
    }

    /// <summary>
    /// Normalizes a Kiota URL template by removing the {+baseurl} prefix, query parameter templates,
    /// and converting path parameters to positional tokens (token1, token2, etc.).
    /// This allows patterns to match regardless of parameter naming while preserving position for validation.
    /// </summary>
    /// <param name="urlTemplate">The URL template to normalize.</param>
    /// <returns>The normalized URL path with positional token parameters.</returns>
    private static string NormalizeUrlTemplate(string urlTemplate)
    {
        // Step 1: Remove {+baseurl} prefix if present
        var cleanedUrl = urlTemplate.StartsWith("{+baseurl}")
            ? urlTemplate.Substring("{+baseurl}".Length)
            : urlTemplate;

        // Step 2: Remove query parameter templates like {?param1,param2}
        cleanedUrl = Regex.Replace(cleanedUrl, @"\{\?.*?\}", string.Empty);

        // Step 3: Replace path parameters with positional tokens: {token1}, {token2}, etc.
        // This allows {id}, {fundId}, {fund-id}, {fund%2Did} to all match the same position
        // but maintains position validation so {fundId}/something/{activityId} matches structure
        var tokenIndex = 1;
        cleanedUrl = Regex.Replace(cleanedUrl, @"\{[^}]+\}", match => $"{{token{tokenIndex++}}}");

        // Step 4: Ensure leading slash for consistent matching
        if (!cleanedUrl.StartsWith("/"))
        {
            cleanedUrl = "/" + cleanedUrl;
        }

        return cleanedUrl;
    }

    /// <summary>
    /// Extracts and normalizes the URL template from a Kiota-generated request builder.
    /// This allows you to use the exact template from generated code instead of hardcoding paths.
    /// Returns the template with URL-decoded parameter names (e.g., {fund%2Did} -> {fund-id}).
    /// </summary>
    /// <typeparam name="T">The type of the request builder.</typeparam>
    /// <param name="requestBuilder">The Kiota-generated request builder instance.</param>
    /// <returns>The normalized URL template with preserved parameter names.</returns>
    /// <example>
    /// <code>
    /// // Get the exact template from Kiota's generated code
    /// var urlTemplate = KiotaClientMockExtensions.GetUrlTemplate(mockClient.Api.Funds[fundId]);
    /// // Returns: "/api/funds/{fund-id}" (if that's what Kiota generated)
    ///
    /// // Use it in your mock
    /// mockedClient.MockClientResponse(
    ///     urlTemplate,
    ///     fund,
    ///     req => req.GetPathParameter("fundId").ToString() == fundId.ToString()
    /// );
    /// </code>
    /// </example>
    public static string GetUrlTemplate<T>(T requestBuilder)
        where T : BaseRequestBuilder
    {
        // Access the UrlTemplate property from the base class
        var urlTemplateProperty = typeof(BaseRequestBuilder).GetProperty(
            "UrlTemplate",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public
        );

        if (urlTemplateProperty == null)
        {
            throw new InvalidOperationException(
                "Could not find UrlTemplate property on BaseRequestBuilder. "
                    + "This may indicate a breaking change in Kiota."
            );
        }

        var urlTemplate = urlTemplateProperty.GetValue(requestBuilder) as string;

        if (string.IsNullOrEmpty(urlTemplate))
        {
            throw new InvalidOperationException(
                $"UrlTemplate is null or empty for request builder of type {typeof(T).Name}"
            );
        }

        // Return the normalized template (strips {+baseurl}, decodes param names, but preserves them)
        return NormalizeUrlTemplate(urlTemplate);
    }

    /// <summary>
    /// Gets the underlying mocked IRequestAdapter from a Kiota client for verification purposes.
    /// This allows you to use NSubstitute's verification methods (.Received(), .DidNotReceive(), etc.)
    /// to verify that the mock was called with specific parameters.
    /// </summary>
    /// <typeparam name="T">The type of the request builder (Kiota client).</typeparam>
    /// <param name="requestBuilder">The Kiota-generated client instance.</param>
    /// <returns>The mocked IRequestAdapter that can be used for verification.</returns>
    /// <example>
    /// <code>
    /// // Setup mock
    /// var fundId = Guid.NewGuid();
    /// _mockClient.MockClientResponse("/api/funds/{id}", fund);
    ///
    /// // Perform action
    /// await _service.GetFundAsync(fundId);
    ///
    /// // Verify the mock was called
    /// var adapter = _mockClient.GetMockAdapter();
    /// await adapter.Received(1).SendAsync&lt;Fund&gt;(
    ///     Arg.Is&lt;RequestInformation&gt;(req =>
    ///         req.UrlTemplate.Contains("/api/funds/")
    ///         &amp;&amp; req.GetPathParameter("id").ToString() == fundId.ToString()
    ///     ),
    ///     Arg.Any&lt;ParsableFactory&lt;Fund&gt;&gt;(),
    ///     Arg.Any&lt;Dictionary&lt;string, ParsableFactory&lt;IParsable&gt;&gt;&gt;(),
    ///     Arg.Any&lt;CancellationToken&gt;()
    /// );
    /// </code>
    /// </example>
    public static IRequestAdapter GetMockAdapter<T>(T requestBuilder)
        where T : BaseRequestBuilder
    {
        // Access the RequestAdapter property from the base class
        var adapterProperty = typeof(BaseRequestBuilder).GetProperty(
            "RequestAdapter",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public
        );

        if (adapterProperty == null)
        {
            throw new InvalidOperationException(
                "Could not find RequestAdapter property on BaseRequestBuilder. "
                    + "This may indicate a breaking change in Kiota."
            );
        }

        var adapter = adapterProperty.GetValue(requestBuilder) as IRequestAdapter;

        if (adapter == null)
        {
            throw new InvalidOperationException(
                $"RequestAdapter is null for request builder of type {typeof(T).Name}. "
                    + "Ensure the client was created using GetMockableClient<T>()."
            );
        }

        return adapter;
    }

    /// <summary>
    /// Creates a Kiota generated client class that can be mocked.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static T GetMockableClient<T>()
        where T : BaseRequestBuilder
    {
        IRequestAdapter _requestAdapterMock;
        _requestAdapterMock = Substitute.For<IRequestAdapter>();

        var instance = Activator.CreateInstance(typeof(T), _requestAdapterMock);
        return instance as T
            ?? throw new InvalidOperationException(
                $"Unable to create an instance of {typeof(T).Name}."
            );
    }

    /// <summary>
    /// Retrieves the <see cref="IRequestAdapter"/> instance from a mocked Kiota client.
    /// </summary>
    /// <typeparam name="T">The type of the mocked Kiota client.</typeparam>
    /// <param name="mockedClient">The mocked client instance.</param>
    /// <returns>The <see cref="IRequestAdapter"/> instance associated with the mocked client.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the RequestAdapter property is not found on the mocked client.
    /// </exception>
    private static IRequestAdapter GetRequestAdapter<T>(T mockedClient)
    {
        return mockedClient!
                .GetType()
                ?.GetProperty("RequestAdapter", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(mockedClient) as IRequestAdapter
            ?? throw new InvalidOperationException(
                "RequestAdapter property not found on mocked client."
            );
    }

    /// <summary>
    /// Mocks a single object response for a Kiota client request.
    /// </summary>
    /// <typeparam name="T">The type of the Kiota client request builder.</typeparam>
    /// <typeparam name="R">The type of the response object.</typeparam>
    /// <param name="mockedClient">The mocked client request builder instance.</param>
    /// <param name="urlTemplate">The URL template to match the request.</param>
    /// <param name="returnObject">The object to return as the response.</param>
    /// <param name="requestInfoPredicate">
    /// An optional predicate to further filter the request information.
    /// </param>
    public static void MockClientResponse<T, R>(
        this T mockedClient,
        string urlTemplate,
        R? returnObject,
        Expression<Predicate<RequestInformation>>? requestInfoPredicate = null
    )
        where T : BaseRequestBuilder
        where R : IParsable
    {
        var requestAdapter = GetRequestAdapter(mockedClient);

        var requestInformationUrlTemplatePredicate = RequestInformationUrlTemplatePredicate(
            urlTemplate
        );
        var requestInformationPredicate =
            requestInfoPredicate != null
                ? requestInfoPredicate.And(requestInformationUrlTemplatePredicate)
                : requestInformationUrlTemplatePredicate;

        requestAdapter
            ?.SendAsync(
                Arg.Is(requestInformationPredicate),
                Arg.Any<ParsableFactory<R>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(returnObject);
    }

    /// <summary>
    /// Mocks a no-content response for a Kiota client request.
    /// </summary>
    /// <typeparam name="T">The type of the Kiota client request builder.</typeparam>
    /// <param name="mockedClient">The mocked client request builder instance.</param>
    /// <param name="urlTemplate">The URL template to match the request.</param>
    /// <param name="requestInfoPredicate">
    /// An optional predicate to further filter the request information.
    /// </param>
    public static void MockClientNoContentResponse<T>(
        this T mockedClient,
        string urlTemplate,
        Expression<Predicate<RequestInformation>>? requestInfoPredicate = null
    )
        where T : BaseRequestBuilder
    {
        var requestAdapter = GetRequestAdapter(mockedClient);

        var requestInformationUrlTemplatePredicate = RequestInformationUrlTemplatePredicate(
            urlTemplate
        );
        var requestInformationPredicate =
            requestInfoPredicate != null
                ? requestInfoPredicate.And(requestInformationUrlTemplatePredicate)
                : requestInformationUrlTemplatePredicate;

        requestAdapter
            ?.SendNoContentAsync(
                Arg.Is(requestInformationPredicate),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(Task.CompletedTask);
    }

    /// <summary>
    /// Mocks a collection response for a Kiota client request.
    /// </summary>
    /// <typeparam name="T">The type of the Kiota client request builder.</typeparam>
    /// <typeparam name="R">The type of the response objects in the collection.</typeparam>
    /// <param name="mockedClient">The mocked client request builder instance.</param>
    /// <param name="urlTemplate">The URL template to match the request.</param>
    /// <param name="returnObject">The collection of objects to return as the response.</param>
    /// <param name="requestInfoPredicate">
    /// An optional predicate to further filter the request information.
    /// </param>
    public static void MockClientCollectionResponse<T, R>(
        this T mockedClient,
        string urlTemplate,
        IEnumerable<R>? returnObject,
        Expression<Predicate<RequestInformation>>? requestInfoPredicate = null
    )
        where T : BaseRequestBuilder
        where R : IParsable
    {
        var requestAdapter = GetRequestAdapter(mockedClient);

        var requestInformationUrlTemplatePredicate = RequestInformationUrlTemplatePredicate(
            urlTemplate
        );
        var requestInformationPredicate =
            requestInfoPredicate != null
                ? requestInfoPredicate.And(requestInformationUrlTemplatePredicate)
                : requestInformationUrlTemplatePredicate;

        requestAdapter
            ?.SendCollectionAsync(
                Arg.Is(requestInformationPredicate),
                Arg.Any<ParsableFactory<R>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(returnObject);
    }

    /// <summary>
    /// Mocks a string response for a Kiota client request.
    /// </summary>
    /// <typeparam name="T">The type of the Kiota client request builder.</typeparam>
    /// <param name="mockedClient">The mocked client request builder instance.</param>
    /// <param name="urlTemplate">The URL template to match the request.</param>
    /// <param name="returnValue">The string to return as the response.</param>
    /// <param name="requestInfoPredicate">
    /// An optional predicate to further filter the request information.
    /// </param>
    public static void MockClientResponse<T>(
        this T mockedClient,
        string urlTemplate,
        string? returnValue,
        Expression<Predicate<RequestInformation>>? requestInfoPredicate = null
    )
        where T : BaseRequestBuilder
    {
        var requestAdapter = GetRequestAdapter(mockedClient);

        var requestInformationUrlTemplatePredicate = RequestInformationUrlTemplatePredicate(
            urlTemplate
        );
        var requestInformationPredicate =
            requestInfoPredicate != null
                ? requestInfoPredicate.And(requestInformationUrlTemplatePredicate)
                : requestInformationUrlTemplatePredicate;

        requestAdapter
            ?.SendPrimitiveAsync<string>(
                Arg.Is(requestInformationPredicate),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(returnValue);
    }

    /// <summary>
    /// Mocks an exception for a Kiota client request that returns a single object.
    /// </summary>
    /// <typeparam name="T">The type of the Kiota client request builder.</typeparam>
    /// <typeparam name="R">The type of the response object.</typeparam>
    /// <param name="mockedClient">The mocked client request builder instance.</param>
    /// <param name="urlTemplate">The URL template to match the request.</param>
    /// <param name="exception">The exception to throw when the request is made.</param>
    /// <param name="requestInfoPredicate">
    /// An optional predicate to further filter the request information.
    /// </param>
    public static void MockClientResponseException<T, R>(
        this T mockedClient,
        string urlTemplate,
        Exception exception,
        Expression<Predicate<RequestInformation>>? requestInfoPredicate = null
    )
        where T : BaseRequestBuilder
        where R : IParsable
    {
        var requestAdapter = GetRequestAdapter(mockedClient);

        var requestInformationUrlTemplatePredicate = RequestInformationUrlTemplatePredicate(
            urlTemplate
        );
        var requestInformationPredicate =
            requestInfoPredicate != null
                ? requestInfoPredicate.And(requestInformationUrlTemplatePredicate)
                : requestInformationUrlTemplatePredicate;

        requestAdapter
            ?.SendAsync(
                Arg.Is(requestInformationPredicate),
                Arg.Any<ParsableFactory<R>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(Task.FromException<R?>(exception));
    }

    /// <summary>
    /// Mocks an exception for a Kiota client request that returns no content.
    /// </summary>
    /// <typeparam name="T">The type of the Kiota client request builder.</typeparam>
    /// <param name="mockedClient">The mocked client request builder instance.</param>
    /// <param name="urlTemplate">The URL template to match the request.</param>
    /// <param name="exception">The exception to throw when the request is made.</param>
    /// <param name="requestInfoPredicate">
    /// An optional predicate to further filter the request information.
    /// </param>
    public static void MockClientNoContentResponseException<T>(
        this T mockedClient,
        string urlTemplate,
        Exception exception,
        Expression<Predicate<RequestInformation>>? requestInfoPredicate = null
    )
        where T : BaseRequestBuilder
    {
        var requestAdapter = GetRequestAdapter(mockedClient);

        var requestInformationUrlTemplatePredicate = RequestInformationUrlTemplatePredicate(
            urlTemplate
        );
        var requestInformationPredicate =
            requestInfoPredicate != null
                ? requestInfoPredicate.And(requestInformationUrlTemplatePredicate)
                : requestInformationUrlTemplatePredicate;

        requestAdapter
            ?.SendNoContentAsync(
                Arg.Is(requestInformationPredicate),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(Task.FromException(exception));
    }

    /// <summary>
    /// Mocks an exception for a Kiota client request that returns a collection.
    /// </summary>
    /// <typeparam name="T">The type of the Kiota client request builder.</typeparam>
    /// <typeparam name="R">The type of the response objects in the collection.</typeparam>
    /// <param name="mockedClient">The mocked client request builder instance.</param>
    /// <param name="urlTemplate">The URL template to match the request.</param>
    /// <param name="exception">The exception to throw when the request is made.</param>
    /// <param name="requestInfoPredicate">
    /// An optional predicate to further filter the request information.
    /// </param>
    public static void MockClientCollectionResponseException<T, R>(
        this T mockedClient,
        string urlTemplate,
        Exception exception,
        Expression<Predicate<RequestInformation>>? requestInfoPredicate = null
    )
        where T : BaseRequestBuilder
        where R : IParsable
    {
        var requestAdapter = GetRequestAdapter(mockedClient);

        var requestInformationUrlTemplatePredicate = RequestInformationUrlTemplatePredicate(
            urlTemplate
        );
        var requestInformationPredicate =
            requestInfoPredicate != null
                ? requestInfoPredicate.And(requestInformationUrlTemplatePredicate)
                : requestInformationUrlTemplatePredicate;

        requestAdapter
            ?.SendCollectionAsync(
                Arg.Is(requestInformationPredicate),
                Arg.Any<ParsableFactory<R>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(Task.FromException<IEnumerable<R>?>(exception));
    }

    /// <summary>
    /// Mocks an exception for a Kiota client request that returns a string.
    /// </summary>
    /// <typeparam name="T">The type of the Kiota client request builder.</typeparam>
    /// <param name="mockedClient">The mocked client request builder instance.</param>
    /// <param name="urlTemplate">The URL template to match the request.</param>
    /// <param name="exception">The exception to throw when the request is made.</param>
    /// <param name="requestInfoPredicate">
    /// An optional predicate to further filter the request information.
    /// </param>
    public static void MockClientResponseException<T>(
        this T mockedClient,
        string urlTemplate,
        Exception exception,
        Expression<Predicate<RequestInformation>>? requestInfoPredicate = null
    )
        where T : BaseRequestBuilder
    {
        var requestAdapter = GetRequestAdapter(mockedClient);

        var requestInformationUrlTemplatePredicate = RequestInformationUrlTemplatePredicate(
            urlTemplate
        );
        var requestInformationPredicate =
            requestInfoPredicate != null
                ? requestInfoPredicate.And(requestInformationUrlTemplatePredicate)
                : requestInformationUrlTemplatePredicate;

        requestAdapter
            ?.SendPrimitiveAsync<string>(
                Arg.Is(requestInformationPredicate),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(Task.FromException<string?>(exception));
    }
}
