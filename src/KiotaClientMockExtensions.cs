using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Serialization.Json;
using NSubstitute;

namespace Gainsway.Kiota.Testing;

/// <summary>
/// Provides extension methods for mocking Kiota-generated client classes and their responses.
/// </summary>
public static class KiotaClientMockExtensions
{
    /// <summary>
    /// Normalizes a Kiota URL template by removing the {+baseurl} prefix and converting parameters to positional names.
    /// Path parameters become {pathParam1}, {pathParam2}, etc.
    /// Query parameters become {?queryParam1,queryParam2} etc.
    /// This allows patterns to match regardless of parameter naming while preserving position for validation.
    /// </summary>
    /// <param name="urlTemplate">The URL template to normalize.</param>
    /// <returns>The normalized URL template with positional parameter names.</returns>
    /// <remarks>
    /// This method is useful for verification scenarios where you want to validate the URL structure
    /// and parameter positions without worrying about exact parameter names.
    ///
    /// Examples:
    /// - "/api/funds/{fundId}" becomes "/api/funds/{pathParam1}"
    /// - "/api/fundapplications/{id}/submissions/{version}/review" becomes "/api/fundapplications/{pathParam1}/submissions/{pathParam2}/review"
    /// - "/api/items{?select,expand,filter}" becomes "/api/items{?queryParam1,queryParam2,queryParam3}"
    /// - "{+baseurl}/api/funds/{fund-id}{?select}" becomes "/api/funds/{pathParam1}{?queryParam1}"
    ///
    /// Use this in verification predicates:
    /// <code>
    /// var normalized = req.NormalizeUrlTemplate();
    /// Assert.That(normalized, Is.EqualTo("/api/funds/{pathParam1}/activities/{pathParam2}"));
    ///
    /// // Verify path parameters by position
    /// Assert.That(req.PathParameters.Values.ElementAt(0).ToString(), Is.EqualTo(fundId.ToString()));
    /// Assert.That(req.PathParameters.Values.ElementAt(1).ToString(), Is.EqualTo(activityId.ToString()));
    /// </code>
    /// </remarks>
    public static string NormalizeUrlTemplate(string urlTemplate)
    {
        // Step 1: Remove {+baseurl} prefix if present
        var cleanedUrl = urlTemplate.StartsWith("{+baseurl}")
            ? urlTemplate.Substring("{+baseurl}".Length)
            : urlTemplate;

        // Step 2: Normalize query parameters {?param1,param2} to {?queryParam1,queryParam2}
        cleanedUrl = Regex.Replace(
            cleanedUrl,
            @"\{\?([^}]+)\}",
            match =>
            {
                var queryParams = match.Groups[1].Value.Split(',');
                var normalizedParams = queryParams
                    .Select((_, index) => $"queryParam{index + 1}")
                    .ToArray();
                return $"{{?{string.Join(",", normalizedParams)}}}";
            }
        );

        // Step 3: Replace path parameters with positional names: {pathParam1}, {pathParam2}, etc.
        // This allows {id}, {fundId}, {fund-id}, {fund%2Did} to all match the same position
        // but maintains position validation so {fundId}/something/{activityId} matches structure
        var pathParamIndex = 1;
        cleanedUrl = Regex.Replace(
            cleanedUrl,
            @"\{([^?}][^}]*)\}",
            match => $"{{pathParam{pathParamIndex++}}}"
        );

        // Step 4: Ensure leading slash for consistent matching
        if (!cleanedUrl.StartsWith("/"))
        {
            cleanedUrl = "/" + cleanedUrl;
        }

        return cleanedUrl;
    }

    /// <summary>
    /// Gets the underlying mocked IRequestAdapter from a Kiota client for verification purposes.
    /// This allows you to use NSubstitute's verification methods (.Received(), .DidNotReceive(), etc.)
    /// to verify that the mock was called with specific parameters.
    /// </summary>
    /// <typeparam name="T">The type of the Kiota client (can be root client or request builder).</typeparam>
    /// <param name="client">The Kiota-generated client instance.</param>
    /// <returns>The mocked IRequestAdapter that can be used for verification.</returns>
    /// <example>
    /// <code>
    /// // Add using directive at the top of your test file
    /// using Gainsway.Kiota.Testing;
    ///
    /// // Setup mock
    /// var fundId = Guid.NewGuid();
    /// _mockClient.Api.Funds[fundId].MockGetAsync(fund);
    ///
    /// // Perform action
    /// await _service.GetFundAsync(fundId);
    ///
    /// // Verify the mock was called with correct HTTP method and URL structure
    /// var adapter = _mockClient.GetMockAdapter();
    /// await adapter.Received(1).SendAsync&lt;Fund&gt;(
    ///     Arg.Is&lt;RequestInformation&gt;(req =>
    ///         req.HttpMethod == Method.GET
    ///         &amp;&amp; req.NormalizeUrlTemplate() == "/api/funds/{pathParam1}"
    ///         &amp;&amp; req.PathParameters.Values.ElementAt(0).ToString() == fundId.ToString()
    ///     ),
    ///     Arg.Any&lt;ParsableFactory&lt;Fund&gt;&gt;(),
    ///     Arg.Any&lt;Dictionary&lt;string, ParsableFactory&lt;IParsable&gt;&gt;&gt;(),
    ///     Arg.Any&lt;CancellationToken&gt;()
    /// );
    ///
    /// // Example with multiple path parameters
    /// await adapter.Received(1).SendAsync&lt;FundApplicationDto&gt;(
    ///     Arg.Is&lt;RequestInformation&gt;(req =>
    ///         req.HttpMethod == Method.POST
    ///         &amp;&amp; req.NormalizeUrlTemplate() == "/api/fundapplications/{pathParam1}/submissions/{pathParam2}/review"
    ///         &amp;&amp; req.PathParameters.Values.ElementAt(0).ToString() == applicationId.ToString()
    ///         &amp;&amp; req.PathParameters.Values.ElementAt(1).ToString() == versionNumber.ToString()
    ///     ),
    ///     Arg.Any&lt;ParsableFactory&lt;FundApplicationDto&gt;&gt;(),
    ///     Arg.Any&lt;Dictionary&lt;string, ParsableFactory&lt;IParsable&gt;&gt;&gt;(),
    ///     Arg.Any&lt;CancellationToken&gt;()
    /// );
    ///
    /// // Example with query parameters
    /// await adapter.Received(1).SendAsync&lt;FundCollectionResponse&gt;(
    ///     Arg.Is&lt;RequestInformation&gt;(req =>
    ///         req.HttpMethod == Method.GET
    ///         &amp;&amp; req.NormalizeUrlTemplate() == "/api/funds{?queryParam1,queryParam2}"
    ///         &amp;&amp; req.QueryParameters.ContainsKey("$select")
    ///         &amp;&amp; req.QueryParameters.ContainsKey("$filter")
    ///     ),
    ///     Arg.Any&lt;ParsableFactory&lt;FundCollectionResponse&gt;&gt;(),
    ///     Arg.Any&lt;Dictionary&lt;string, ParsableFactory&lt;IParsable&gt;&gt;&gt;(),
    ///     Arg.Any&lt;CancellationToken&gt;()
    /// );
    /// </code>
    /// </example>
    public static IRequestAdapter GetMockAdapter<T>(this T client)
    {
        // Use the existing private GetRequestAdapter method which works with any type
        return GetRequestAdapter(client);
    }

    /// <summary>
    /// Creates a Kiota generated client class that can be mocked.
    /// </summary>
    /// <remarks>
    /// The mock <see cref="IRequestAdapter"/> is given a real
    /// <see cref="JsonSerializationWriterFactory"/> via its
    /// <see cref="IRequestAdapter.SerializationWriterFactory"/> property. Generated PUT/POST/PATCH
    /// methods call <c>RequestInformation.SetContentFromParsable</c>, which needs a working
    /// factory to serialize the request body into <c>RequestInformation.Content</c> — without
    /// this, <c>Content</c> would stay null and request-body assertions
    /// (<see cref="VerifyCallAssertion.WithBody{TBody}"/>) could never read anything back.
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public static T GetMockableClient<T>()
        where T : BaseRequestBuilder
    {
        IRequestAdapter _requestAdapterMock;
        _requestAdapterMock = Substitute.For<IRequestAdapter>();
        _requestAdapterMock.SerializationWriterFactory.Returns(
            new JsonSerializationWriterFactory()
        );

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
}
