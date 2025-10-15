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
    /// Helper method that performs URL template matching.
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
    /// <param name="urlTemplate">The URL template to match (e.g., "/api/funds/{*}").</param>
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
    /// 3. Normalize all path parameters to {*} wildcard for flexible matching
    /// 4. Compare the cleaned path with the provided pattern
    ///
    /// Path Parameter Matching:
    /// Kiota may generate different parameter names than what appears in your OpenAPI spec.
    /// For example, {id} might become {fund-id} or {fund%2Did} (URL-encoded).
    /// To handle this, all path parameters are normalized to {*} wildcards.
    ///
    /// Examples of valid matches:
    /// - Pattern "/api/funds/{*}" matches "{+baseurl}/api/funds/{id}"
    /// - Pattern "/api/funds/{*}" matches "{+baseurl}/api/funds/{fund-id}"
    /// - Pattern "/api/funds/{*}" matches "{+baseurl}/api/funds/{fund%2Did}"
    /// - Pattern "/api/funds/{*}/activities/{*}" matches "{+baseurl}/api/funds/{id}/activities/{activityId}"
    /// - Pattern "api/funds/{*}" works too (leading slash optional)
    ///
    /// Note: If you need to differentiate between similar endpoints (e.g., /api/funds/{id} vs /api/funds/{id}/metadata),
    /// use the full path with wildcards and optionally add a requestInfoPredicate to check other properties like HttpMethod.
    /// </remarks>
    private static Expression<Predicate<RequestInformation>> RequestInformationUrlTemplatePredicate(
        string urlTemplate
    )
    {
        // Normalize the pattern: ensure it starts with / for consistent matching
        var normalizedPattern = urlTemplate.StartsWith("/") ? urlTemplate : "/" + urlTemplate;

        return req => MatchesUrlTemplate(req, normalizedPattern, urlTemplate);
    }

    /// <summary>
    /// Normalizes a Kiota URL template by removing the {+baseurl} prefix and query parameter templates.
    /// Also handles URL-encoded parameter names like {fund%2Did} by converting them to simple placeholders.
    /// </summary>
    /// <param name="urlTemplate">The URL template to normalize.</param>
    /// <returns>The normalized URL path.</returns>
    private static string NormalizeUrlTemplate(string urlTemplate)
    {
        // Step 1: Remove {+baseurl} prefix if present
        var cleanedUrl = urlTemplate.StartsWith("{+baseurl}")
            ? urlTemplate.Substring("{+baseurl}".Length)
            : urlTemplate;

        // Step 2: Remove query parameter templates like {?param1,param2}
        cleanedUrl = Regex.Replace(cleanedUrl, @"\{\?.*?\}", string.Empty);

        // Step 3: Normalize URL-encoded parameter names
        // Kiota generates things like {fund%2Did} which is {fund-id} URL-encoded
        // We need to decode these so they match user patterns like {id}
        // Strategy: Replace any {paramName} with a normalized placeholder {*}
        cleanedUrl = Regex.Replace(cleanedUrl, @"\{[^}]+\}", "{*}");

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
    /// </summary>
    /// <typeparam name="T">The type of the request builder.</typeparam>
    /// <param name="requestBuilder">The Kiota-generated request builder instance.</param>
    /// <returns>The normalized URL template suitable for mocking.</returns>
    /// <example>
    /// <code>
    /// // Instead of hardcoding:
    /// mockedClient.MockClientResponse("/api/funds/{*}", fund);
    ///
    /// // Use Kiota's generated template:
    /// var urlTemplate = KiotaClientMockExtensions.GetUrlTemplate(mockClient.Api.Funds[fundId]);
    /// mockedClient.MockClientResponse(urlTemplate, fund);
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

        // Return the normalized template (strips {+baseurl}, converts params to {*})
        return NormalizeUrlTemplate(urlTemplate);
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
