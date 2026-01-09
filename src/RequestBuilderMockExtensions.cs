using System.Reflection;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Gainsway.Kiota.Testing;

/// <summary>
/// Provides type-safe extension methods for mocking Kiota-generated request builders directly.
/// This approach eliminates URL string matching and provides compile-time safety.
/// </summary>
public static class RequestBuilderMockExtensions
{
    /// <summary>
    /// Mocks a GET request that returns a single object (IParsable).
    /// </summary>
    /// <typeparam name="TBuilder">The request builder type.</typeparam>
    /// <typeparam name="TResponse">The response type (must implement IParsable).</typeparam>
    /// <param name="requestBuilder">The Kiota-generated request builder.</param>
    /// <param name="response">The object to return when this endpoint is called.</param>
    /// <param name="requestInfoPredicate">Optional additional conditions to match the request.</param>
    /// <returns>The request builder for fluent chaining.</returns>
    /// <example>
    /// <code>
    /// var fundId = Guid.NewGuid();
    /// var fund = new Fund { Id = fundId, Name = "Test Fund" };
    ///
    /// // Type-safe mocking - no URL strings!
    /// _client.Api.Funds[fundId].MockGetAsync(fund);
    ///
    /// // With additional conditions
    /// _client.Api.Funds[fundId].MockGetAsync(
    ///     fund,
    ///     req => req.Headers.ContainsKey("Authorization")
    /// );
    /// </code>
    /// </example>
    public static TBuilder MockGetAsync<TBuilder, TResponse>(
        this TBuilder requestBuilder,
        TResponse? response,
        Func<RequestInformation, bool>? requestInfoPredicate = null
    )
        where TBuilder : BaseRequestBuilder
        where TResponse : IParsable
    {
        var mockAdapter = GetMockAdapter(requestBuilder);
        var (urlTemplate, pathParameters) = GetBuilderInfo(requestBuilder);

        mockAdapter
            .SendAsync<TResponse>(
                Arg.Is<RequestInformation>(req =>
                    MatchesBuilder(req, urlTemplate, pathParameters, Method.GET)
                    && (requestInfoPredicate == null || requestInfoPredicate(req))
                ),
                Arg.Any<ParsableFactory<TResponse>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(response);

        return requestBuilder;
    }

    /// <summary>
    /// Mocks a GET request that throws an exception.
    /// </summary>
    /// <typeparam name="TBuilder">The request builder type.</typeparam>
    /// <typeparam name="TResponse">The response type (must implement IParsable).</typeparam>
    /// <param name="requestBuilder">The Kiota-generated request builder.</param>
    /// <param name="exception">The exception to throw when this endpoint is called.</param>
    /// <param name="requestInfoPredicate">Optional additional conditions to match the request.</param>
    /// <returns>The request builder for fluent chaining.</returns>
    /// <example>
    /// <code>
    /// var nonExistentId = Guid.NewGuid();
    ///
    /// _client.Api.Funds[nonExistentId].MockGetAsync&lt;FundItemRequestBuilder, Fund&gt;(
    ///     new ApiException("Fund not found") { ResponseStatusCode = 404 }
    /// );
    /// </code>
    /// </example>
    public static TBuilder MockGetAsync<TBuilder, TResponse>(
        this TBuilder requestBuilder,
        Exception exception,
        Func<RequestInformation, bool>? requestInfoPredicate = null
    )
        where TBuilder : BaseRequestBuilder
        where TResponse : IParsable
    {
        var mockAdapter = GetMockAdapter(requestBuilder);
        var (urlTemplate, pathParameters) = GetBuilderInfo(requestBuilder);

        mockAdapter
            .SendAsync<TResponse>(
                Arg.Is<RequestInformation>(req =>
                    MatchesBuilder(req, urlTemplate, pathParameters, Method.GET)
                    && (requestInfoPredicate == null || requestInfoPredicate(req))
                ),
                Arg.Any<ParsableFactory<TResponse>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>>(),
                Arg.Any<CancellationToken>()
            )
            .Throws(exception);

        return requestBuilder;
    }

    /// <summary>
    /// Mocks a GET request that throws an exception.
    /// [DEPRECATED: Use MockGetAsync&lt;TBuilder, TResponse&gt;(Exception) overload instead]
    /// </summary>
    /// <typeparam name="TBuilder">The request builder type.</typeparam>
    /// <typeparam name="TResponse">The response type (must implement IParsable).</typeparam>
    /// <param name="requestBuilder">The Kiota-generated request builder.</param>
    /// <param name="exception">The exception to throw when this endpoint is called.</param>
    /// <param name="requestInfoPredicate">Optional additional conditions to match the request.</param>
    /// <returns>The request builder for fluent chaining.</returns>
    [Obsolete(
        "Use MockGetAsync<TBuilder, TResponse>(Exception) overload instead. This method will be removed in a future version."
    )]
    public static TBuilder MockGetAsyncException<TBuilder, TResponse>(
        this TBuilder requestBuilder,
        Exception exception,
        Func<RequestInformation, bool>? requestInfoPredicate = null
    )
        where TBuilder : BaseRequestBuilder
        where TResponse : IParsable
    {
        return MockGetAsync<TBuilder, TResponse>(requestBuilder, exception, requestInfoPredicate);
    }

    /// <summary>
    /// Mocks a GET request that returns a string.
    /// </summary>
    /// <typeparam name="TBuilder">The request builder type.</typeparam>
    /// <param name="requestBuilder">The Kiota-generated request builder.</param>
    /// <param name="response">The string to return when this endpoint is called.</param>
    /// <param name="requestInfoPredicate">Optional additional conditions to match the request.</param>
    /// <returns>The request builder for fluent chaining.</returns>
    /// <example>
    /// <code>
    /// _client.Api.Status.MockGetAsync("operational");
    /// </code>
    /// </example>
    public static TBuilder MockGetAsync<TBuilder>(
        this TBuilder requestBuilder,
        string? response,
        Func<RequestInformation, bool>? requestInfoPredicate = null
    )
        where TBuilder : BaseRequestBuilder
    {
        var mockAdapter = GetMockAdapter(requestBuilder);
        var (urlTemplate, pathParameters) = GetBuilderInfo(requestBuilder);

        mockAdapter
            .SendPrimitiveAsync<string>(
                Arg.Is<RequestInformation>(req =>
                    MatchesBuilder(req, urlTemplate, pathParameters, Method.GET)
                    && (requestInfoPredicate == null || requestInfoPredicate(req))
                ),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(response);

        return requestBuilder;
    }

    /// <summary>
    /// Mocks a GET request for a string that throws an exception.
    /// </summary>
    /// <typeparam name="TBuilder">The request builder type.</typeparam>
    /// <param name="requestBuilder">The Kiota-generated request builder.</param>
    /// <param name="exception">The exception to throw when this endpoint is called.</param>
    /// <param name="requestInfoPredicate">Optional additional conditions to match the request.</param>
    /// <returns>The request builder for fluent chaining.</returns>
    /// <example>
    /// <code>
    /// _client.Api.Status.MockGetAsync(new ApiException("Service unavailable") { ResponseStatusCode = 503 });
    /// </code>
    /// </example>
    public static TBuilder MockGetAsync<TBuilder>(
        this TBuilder requestBuilder,
        Exception exception,
        Func<RequestInformation, bool>? requestInfoPredicate = null
    )
        where TBuilder : BaseRequestBuilder
    {
        var mockAdapter = GetMockAdapter(requestBuilder);
        var (urlTemplate, pathParameters) = GetBuilderInfo(requestBuilder);

        mockAdapter
            .SendPrimitiveAsync<string>(
                Arg.Is<RequestInformation>(req =>
                    MatchesBuilder(req, urlTemplate, pathParameters, Method.GET)
                    && (requestInfoPredicate == null || requestInfoPredicate(req))
                ),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>>(),
                Arg.Any<CancellationToken>()
            )
            .Throws(exception);

        return requestBuilder;
    }

    /// <summary>
    /// Mocks a GET request that returns a collection of objects.
    /// </summary>
    /// <typeparam name="TBuilder">The request builder type.</typeparam>
    /// <typeparam name="TResponse">The response item type (must implement IParsable).</typeparam>
    /// <param name="requestBuilder">The Kiota-generated request builder.</param>
    /// <param name="response">The collection to return when this endpoint is called.</param>
    /// <param name="requestInfoPredicate">Optional additional conditions to match the request.</param>
    /// <returns>The request builder for fluent chaining.</returns>
    /// <example>
    /// <code>
    /// var activities = new List&lt;Activity&gt;
    /// {
    ///     new Activity { Id = Guid.NewGuid(), Name = "Activity 1" },
    ///     new Activity { Id = Guid.NewGuid(), Name = "Activity 2" }
    /// };
    ///
    /// _client.Api.Funds[fundId].Activities.MockGetCollectionAsync(activities);
    /// </code>
    /// </example>
    public static TBuilder MockGetCollectionAsync<TBuilder, TResponse>(
        this TBuilder requestBuilder,
        IEnumerable<TResponse>? response,
        Func<RequestInformation, bool>? requestInfoPredicate = null
    )
        where TBuilder : BaseRequestBuilder
        where TResponse : IParsable
    {
        var mockAdapter = GetMockAdapter(requestBuilder);
        var (urlTemplate, pathParameters) = GetBuilderInfo(requestBuilder);

        mockAdapter
            .SendCollectionAsync<TResponse>(
                Arg.Is<RequestInformation>(req =>
                    MatchesBuilder(req, urlTemplate, pathParameters, Method.GET)
                    && (requestInfoPredicate == null || requestInfoPredicate(req))
                ),
                Arg.Any<ParsableFactory<TResponse>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(response);

        return requestBuilder;
    }

    /// <summary>
    /// Mocks a GET request that returns a collection and throws an exception.
    /// </summary>
    /// <typeparam name="TBuilder">The request builder type.</typeparam>
    /// <typeparam name="TResponse">The response item type (must implement IParsable).</typeparam>
    /// <param name="requestBuilder">The Kiota-generated request builder.</param>
    /// <param name="exception">The exception to throw when this endpoint is called.</param>
    /// <param name="requestInfoPredicate">Optional additional conditions to match the request.</param>
    /// <returns>The request builder for fluent chaining.</returns>
    /// <example>
    /// <code>
    /// _client.Api.Funds[fundId].Activities.MockGetCollectionAsync&lt;ActivitiesRequestBuilder, Activity&gt;(
    ///     new ApiException("Internal Server Error") { ResponseStatusCode = 500 }
    /// );
    /// </code>
    /// </example>
    public static TBuilder MockGetCollectionAsync<TBuilder, TResponse>(
        this TBuilder requestBuilder,
        Exception exception,
        Func<RequestInformation, bool>? requestInfoPredicate = null
    )
        where TBuilder : BaseRequestBuilder
        where TResponse : IParsable
    {
        var mockAdapter = GetMockAdapter(requestBuilder);
        var (urlTemplate, pathParameters) = GetBuilderInfo(requestBuilder);

        mockAdapter
            .SendCollectionAsync<TResponse>(
                Arg.Is<RequestInformation>(req =>
                    MatchesBuilder(req, urlTemplate, pathParameters, Method.GET)
                    && (requestInfoPredicate == null || requestInfoPredicate(req))
                ),
                Arg.Any<ParsableFactory<TResponse>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>>(),
                Arg.Any<CancellationToken>()
            )
            .Throws(exception);

        return requestBuilder;
    }

    /// <summary>
    /// Mocks a GET request that returns a collection and throws an exception.
    /// [DEPRECATED: Use MockGetCollectionAsync&lt;TBuilder, TResponse&gt;(Exception) overload instead]
    /// </summary>
    /// <typeparam name="TBuilder">The request builder type.</typeparam>
    /// <typeparam name="TResponse">The response item type (must implement IParsable).</typeparam>
    /// <param name="requestBuilder">The Kiota-generated request builder.</param>
    /// <param name="exception">The exception to throw when this endpoint is called.</param>
    /// <param name="requestInfoPredicate">Optional additional conditions to match the request.</param>
    /// <returns>The request builder for fluent chaining.</returns>
    [Obsolete(
        "Use MockGetCollectionAsync<TBuilder, TResponse>(Exception) overload instead. This method will be removed in a future version."
    )]
    public static TBuilder MockGetCollectionAsyncException<TBuilder, TResponse>(
        this TBuilder requestBuilder,
        Exception exception,
        Func<RequestInformation, bool>? requestInfoPredicate = null
    )
        where TBuilder : BaseRequestBuilder
        where TResponse : IParsable
    {
        return MockGetCollectionAsync<TBuilder, TResponse>(
            requestBuilder,
            exception,
            requestInfoPredicate
        );
    }

    /// <summary>
    /// Mocks a POST request that returns a single object.
    /// </summary>
    /// <typeparam name="TBuilder">The request builder type.</typeparam>
    /// <typeparam name="TResponse">The response type (must implement IParsable).</typeparam>
    /// <param name="requestBuilder">The Kiota-generated request builder.</param>
    /// <param name="response">The object to return when this endpoint is called.</param>
    /// <param name="requestInfoPredicate">Optional additional conditions to match the request.</param>
    /// <returns>The request builder for fluent chaining.</returns>
    /// <example>
    /// <code>
    /// var newFund = new Fund { Id = Guid.NewGuid(), Name = "New Fund" };
    ///
    /// _client.Api.Funds.MockPostAsync(newFund);
    ///
    /// // With body validation
    /// _client.Api.Funds.MockPostAsync(
    ///     newFund,
    ///     req => req.Content != null
    /// );
    /// </code>
    /// </example>
    public static TBuilder MockPostAsync<TBuilder, TResponse>(
        this TBuilder requestBuilder,
        TResponse? response,
        Func<RequestInformation, bool>? requestInfoPredicate = null
    )
        where TBuilder : BaseRequestBuilder
        where TResponse : IParsable
    {
        var mockAdapter = GetMockAdapter(requestBuilder);
        var (urlTemplate, pathParameters) = GetBuilderInfo(requestBuilder);

        mockAdapter
            .SendAsync<TResponse>(
                Arg.Is<RequestInformation>(req =>
                    MatchesBuilder(req, urlTemplate, pathParameters, Method.POST)
                    && (requestInfoPredicate == null || requestInfoPredicate(req))
                ),
                Arg.Any<ParsableFactory<TResponse>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(response);

        return requestBuilder;
    }

    /// <summary>
    /// Mocks a POST request that throws an exception.
    /// </summary>
    /// <typeparam name="TBuilder">The request builder type.</typeparam>
    /// <typeparam name="TResponse">The response type (must implement IParsable).</typeparam>
    /// <param name="requestBuilder">The Kiota-generated request builder.</param>
    /// <param name="exception">The exception to throw when this endpoint is called.</param>
    /// <param name="requestInfoPredicate">Optional additional conditions to match the request.</param>
    /// <returns>The request builder for fluent chaining.</returns>
    /// <example>
    /// <code>
    /// _client.Api.Funds.MockPostAsync&lt;FundsRequestBuilder, Fund&gt;(
    ///     new ApiException("Validation failed") { ResponseStatusCode = 400 }
    /// );
    /// </code>
    /// </example>
    public static TBuilder MockPostAsync<TBuilder, TResponse>(
        this TBuilder requestBuilder,
        Exception exception,
        Func<RequestInformation, bool>? requestInfoPredicate = null
    )
        where TBuilder : BaseRequestBuilder
        where TResponse : IParsable
    {
        var mockAdapter = GetMockAdapter(requestBuilder);
        var (urlTemplate, pathParameters) = GetBuilderInfo(requestBuilder);

        mockAdapter
            .SendAsync<TResponse>(
                Arg.Is<RequestInformation>(req =>
                    MatchesBuilder(req, urlTemplate, pathParameters, Method.POST)
                    && (requestInfoPredicate == null || requestInfoPredicate(req))
                ),
                Arg.Any<ParsableFactory<TResponse>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>>(),
                Arg.Any<CancellationToken>()
            )
            .Throws(exception);

        return requestBuilder;
    }

    /// <summary>
    /// Mocks a POST request that returns a collection of objects.
    /// </summary>
    /// <typeparam name="TBuilder">The request builder type.</typeparam>
    /// <typeparam name="TResponse">The response item type (must implement IParsable).</typeparam>
    /// <param name="requestBuilder">The Kiota-generated request builder.</param>
    /// <param name="response">The collection to return when this endpoint is called.</param>
    /// <param name="requestInfoPredicate">Optional additional conditions to match the request.</param>
    /// <returns>The request builder for fluent chaining.</returns>
    /// <example>
    /// <code>
    /// var activities = new List&lt;Activity&gt;
    /// {
    ///     new Activity { Id = Guid.NewGuid(), Name = "Activity 1" },
    ///     new Activity { Id = Guid.NewGuid(), Name = "Activity 2" }
    /// };
    ///
    /// _client.Api.Funds[fundId].Activities.MockPostCollectionAsync(activities);
    /// </code>
    /// </example>
    public static TBuilder MockPostCollectionAsync<TBuilder, TResponse>(
        this TBuilder requestBuilder,
        IEnumerable<TResponse>? response,
        Func<RequestInformation, bool>? requestInfoPredicate = null
    )
        where TBuilder : BaseRequestBuilder
        where TResponse : IParsable
    {
        var mockAdapter = GetMockAdapter(requestBuilder);
        var (urlTemplate, pathParameters) = GetBuilderInfo(requestBuilder);

        mockAdapter
            .SendCollectionAsync<TResponse>(
                Arg.Is<RequestInformation>(req =>
                    MatchesBuilder(req, urlTemplate, pathParameters, Method.POST)
                    && (requestInfoPredicate == null || requestInfoPredicate(req))
                ),
                Arg.Any<ParsableFactory<TResponse>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(response);

        return requestBuilder;
    }

    /// <summary>
    /// Mocks a POST request that returns a collection and throws an exception.
    /// </summary>
    /// <typeparam name="TBuilder">The request builder type.</typeparam>
    /// <typeparam name="TResponse">The response item type (must implement IParsable).</typeparam>
    /// <param name="requestBuilder">The Kiota-generated request builder.</param>
    /// <param name="exception">The exception to throw when this endpoint is called.</param>
    /// <param name="requestInfoPredicate">Optional additional conditions to match the request.</param>
    /// <returns>The request builder for fluent chaining.</returns>
    /// <example>
    /// <code>
    /// _client.Api.Funds[fundId].Activities.MockPostCollectionAsync&lt;ActivitiesRequestBuilder, Activity&gt;(
    ///     new ApiException("Validation failed") { ResponseStatusCode = 400 }
    /// );
    /// </code>
    /// </example>
    public static TBuilder MockPostCollectionAsync<TBuilder, TResponse>(
        this TBuilder requestBuilder,
        Exception exception,
        Func<RequestInformation, bool>? requestInfoPredicate = null
    )
        where TBuilder : BaseRequestBuilder
        where TResponse : IParsable
    {
        var mockAdapter = GetMockAdapter(requestBuilder);
        var (urlTemplate, pathParameters) = GetBuilderInfo(requestBuilder);

        mockAdapter
            .SendCollectionAsync<TResponse>(
                Arg.Is<RequestInformation>(req =>
                    MatchesBuilder(req, urlTemplate, pathParameters, Method.POST)
                    && (requestInfoPredicate == null || requestInfoPredicate(req))
                ),
                Arg.Any<ParsableFactory<TResponse>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>>(),
                Arg.Any<CancellationToken>()
            )
            .Throws(exception);

        return requestBuilder;
    }

    /// <summary>
    /// Mocks a PUT request that returns a single object.
    /// </summary>
    /// <typeparam name="TBuilder">The request builder type.</typeparam>
    /// <typeparam name="TResponse">The response type (must implement IParsable).</typeparam>
    /// <param name="requestBuilder">The Kiota-generated request builder.</param>
    /// <param name="response">The object to return when this endpoint is called.</param>
    /// <param name="requestInfoPredicate">Optional additional conditions to match the request.</param>
    /// <returns>The request builder for fluent chaining.</returns>
    public static TBuilder MockPutAsync<TBuilder, TResponse>(
        this TBuilder requestBuilder,
        TResponse? response,
        Func<RequestInformation, bool>? requestInfoPredicate = null
    )
        where TBuilder : BaseRequestBuilder
        where TResponse : IParsable
    {
        var mockAdapter = GetMockAdapter(requestBuilder);
        var (urlTemplate, pathParameters) = GetBuilderInfo(requestBuilder);

        mockAdapter
            .SendAsync<TResponse>(
                Arg.Is<RequestInformation>(req =>
                    MatchesBuilder(req, urlTemplate, pathParameters, Method.PUT)
                    && (requestInfoPredicate == null || requestInfoPredicate(req))
                ),
                Arg.Any<ParsableFactory<TResponse>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(response);

        return requestBuilder;
    }

    /// <summary>
    /// Mocks a PUT request that throws an exception.
    /// </summary>
    /// <typeparam name="TBuilder">The request builder type.</typeparam>
    /// <typeparam name="TResponse">The response type (must implement IParsable).</typeparam>
    /// <param name="requestBuilder">The Kiota-generated request builder.</param>
    /// <param name="exception">The exception to throw when this endpoint is called.</param>
    /// <param name="requestInfoPredicate">Optional additional conditions to match the request.</param>
    /// <returns>The request builder for fluent chaining.</returns>
    public static TBuilder MockPutAsync<TBuilder, TResponse>(
        this TBuilder requestBuilder,
        Exception exception,
        Func<RequestInformation, bool>? requestInfoPredicate = null
    )
        where TBuilder : BaseRequestBuilder
        where TResponse : IParsable
    {
        var mockAdapter = GetMockAdapter(requestBuilder);
        var (urlTemplate, pathParameters) = GetBuilderInfo(requestBuilder);

        mockAdapter
            .SendAsync<TResponse>(
                Arg.Is<RequestInformation>(req =>
                    MatchesBuilder(req, urlTemplate, pathParameters, Method.PUT)
                    && (requestInfoPredicate == null || requestInfoPredicate(req))
                ),
                Arg.Any<ParsableFactory<TResponse>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>>(),
                Arg.Any<CancellationToken>()
            )
            .Throws(exception);

        return requestBuilder;
    }

    /// <summary>
    /// Mocks a PATCH request that returns a single object.
    /// </summary>
    /// <typeparam name="TBuilder">The request builder type.</typeparam>
    /// <typeparam name="TResponse">The response type (must implement IParsable).</typeparam>
    /// <param name="requestBuilder">The Kiota-generated request builder.</param>
    /// <param name="response">The object to return when this endpoint is called.</param>
    /// <param name="requestInfoPredicate">Optional additional conditions to match the request.</param>
    /// <returns>The request builder for fluent chaining.</returns>
    public static TBuilder MockPatchAsync<TBuilder, TResponse>(
        this TBuilder requestBuilder,
        TResponse? response,
        Func<RequestInformation, bool>? requestInfoPredicate = null
    )
        where TBuilder : BaseRequestBuilder
        where TResponse : IParsable
    {
        var mockAdapter = GetMockAdapter(requestBuilder);
        var (urlTemplate, pathParameters) = GetBuilderInfo(requestBuilder);

        mockAdapter
            .SendAsync<TResponse>(
                Arg.Is<RequestInformation>(req =>
                    MatchesBuilder(req, urlTemplate, pathParameters, Method.PATCH)
                    && (requestInfoPredicate == null || requestInfoPredicate(req))
                ),
                Arg.Any<ParsableFactory<TResponse>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(response);

        return requestBuilder;
    }

    /// <summary>
    /// Mocks a PATCH request that throws an exception.
    /// </summary>
    /// <typeparam name="TBuilder">The request builder type.</typeparam>
    /// <typeparam name="TResponse">The response type (must implement IParsable).</typeparam>
    /// <param name="requestBuilder">The Kiota-generated request builder.</param>
    /// <param name="exception">The exception to throw when this endpoint is called.</param>
    /// <param name="requestInfoPredicate">Optional additional conditions to match the request.</param>
    /// <returns>The request builder for fluent chaining.</returns>
    public static TBuilder MockPatchAsync<TBuilder, TResponse>(
        this TBuilder requestBuilder,
        Exception exception,
        Func<RequestInformation, bool>? requestInfoPredicate = null
    )
        where TBuilder : BaseRequestBuilder
        where TResponse : IParsable
    {
        var mockAdapter = GetMockAdapter(requestBuilder);
        var (urlTemplate, pathParameters) = GetBuilderInfo(requestBuilder);

        mockAdapter
            .SendAsync<TResponse>(
                Arg.Is<RequestInformation>(req =>
                    MatchesBuilder(req, urlTemplate, pathParameters, Method.PATCH)
                    && (requestInfoPredicate == null || requestInfoPredicate(req))
                ),
                Arg.Any<ParsableFactory<TResponse>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>>(),
                Arg.Any<CancellationToken>()
            )
            .Throws(exception);

        return requestBuilder;
    }

    /// <summary>
    /// Mocks a DELETE request (no content response).
    /// </summary>
    /// <typeparam name="TBuilder">The request builder type.</typeparam>
    /// <param name="requestBuilder">The Kiota-generated request builder.</param>
    /// <param name="requestInfoPredicate">Optional additional conditions to match the request.</param>
    /// <returns>The request builder for fluent chaining.</returns>
    /// <example>
    /// <code>
    /// var fundId = Guid.NewGuid();
    ///
    /// _client.Api.Funds[fundId].MockDeleteAsync();
    /// </code>
    /// </example>
    public static TBuilder MockDeleteAsync<TBuilder>(
        this TBuilder requestBuilder,
        Func<RequestInformation, bool>? requestInfoPredicate = null
    )
        where TBuilder : BaseRequestBuilder
    {
        var mockAdapter = GetMockAdapter(requestBuilder);
        var (urlTemplate, pathParameters) = GetBuilderInfo(requestBuilder);

        mockAdapter
            .SendNoContentAsync(
                Arg.Is<RequestInformation>(req =>
                    MatchesBuilder(req, urlTemplate, pathParameters, Method.DELETE)
                    && (requestInfoPredicate == null || requestInfoPredicate(req))
                ),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(Task.CompletedTask);

        return requestBuilder;
    }

    /// <summary>
    /// Mocks a DELETE request that throws an exception.
    /// </summary>
    /// <typeparam name="TBuilder">The request builder type.</typeparam>
    /// <param name="requestBuilder">The Kiota-generated request builder.</param>
    /// <param name="exception">The exception to throw when this endpoint is called.</param>
    /// <param name="requestInfoPredicate">Optional additional conditions to match the request.</param>
    /// <returns>The request builder for fluent chaining.</returns>
    /// <example>
    /// <code>
    /// _client.Api.Funds[fundId].MockDeleteAsync(
    ///     new ApiException("Conflict") { ResponseStatusCode = 409 }
    /// );
    /// </code>
    /// </example>
    public static TBuilder MockDeleteAsync<TBuilder>(
        this TBuilder requestBuilder,
        Exception exception,
        Func<RequestInformation, bool>? requestInfoPredicate = null
    )
        where TBuilder : BaseRequestBuilder
    {
        var mockAdapter = GetMockAdapter(requestBuilder);
        var (urlTemplate, pathParameters) = GetBuilderInfo(requestBuilder);

        mockAdapter
            .SendNoContentAsync(
                Arg.Is<RequestInformation>(req =>
                    MatchesBuilder(req, urlTemplate, pathParameters, Method.DELETE)
                    && (requestInfoPredicate == null || requestInfoPredicate(req))
                ),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>>(),
                Arg.Any<CancellationToken>()
            )
            .Throws(exception);

        return requestBuilder;
    }

    /// <summary>
    /// Mocks a DELETE request that throws an exception.
    /// [DEPRECATED: Use MockDeleteAsync&lt;TBuilder&gt;(Exception) overload instead]
    /// </summary>
    /// <typeparam name="TBuilder">The request builder type.</typeparam>
    /// <param name="requestBuilder">The Kiota-generated request builder.</param>
    /// <param name="exception">The exception to throw when this endpoint is called.</param>
    /// <param name="requestInfoPredicate">Optional additional conditions to match the request.</param>
    /// <returns>The request builder for fluent chaining.</returns>
    [Obsolete(
        "Use MockDeleteAsync<TBuilder>(Exception) overload instead. This method will be removed in a future version."
    )]
    public static TBuilder MockDeleteAsyncException<TBuilder>(
        this TBuilder requestBuilder,
        Exception exception,
        Func<RequestInformation, bool>? requestInfoPredicate = null
    )
        where TBuilder : BaseRequestBuilder
    {
        return MockDeleteAsync<TBuilder>(requestBuilder, exception, requestInfoPredicate);
    }

    /// <summary>
    /// Mocks a DELETE request that returns a single object.
    /// Some APIs return data in DELETE responses (e.g., returning the deleted object or confirmation data).
    /// </summary>
    /// <typeparam name="TBuilder">The request builder type.</typeparam>
    /// <typeparam name="TResponse">The response type (must implement IParsable).</typeparam>
    /// <param name="requestBuilder">The Kiota-generated request builder.</param>
    /// <param name="response">The object to return when this endpoint is called.</param>
    /// <param name="requestInfoPredicate">Optional additional conditions to match the request.</param>
    /// <returns>The request builder for fluent chaining.</returns>
    /// <example>
    /// <code>
    /// var fundId = Guid.NewGuid();
    /// var deletedFund = new Fund { Id = fundId, Name = "Deleted Fund", Status = FundStatus.Deleted };
    ///
    /// _client.Api.Funds[fundId].MockDeleteAsync(deletedFund);
    ///
    /// // With body validation
    /// _client.Api.Funds[fundId].MockDeleteAsync(
    ///     deletedFund,
    ///     req => req.Content != null
    /// );
    /// </code>
    /// </example>
    public static TBuilder MockDeleteAsync<TBuilder, TResponse>(
        this TBuilder requestBuilder,
        TResponse? response,
        Func<RequestInformation, bool>? requestInfoPredicate = null
    )
        where TBuilder : BaseRequestBuilder
        where TResponse : IParsable
    {
        var mockAdapter = GetMockAdapter(requestBuilder);
        var (urlTemplate, pathParameters) = GetBuilderInfo(requestBuilder);

        mockAdapter
            .SendAsync<TResponse>(
                Arg.Is<RequestInformation>(req =>
                    MatchesBuilder(req, urlTemplate, pathParameters, Method.DELETE)
                    && (requestInfoPredicate == null || requestInfoPredicate(req))
                ),
                Arg.Any<ParsableFactory<TResponse>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(response);

        return requestBuilder;
    }

    /// <summary>
    /// Mocks a DELETE request that returns a single object and throws an exception.
    /// </summary>
    /// <typeparam name="TBuilder">The request builder type.</typeparam>
    /// <typeparam name="TResponse">The response type (must implement IParsable).</typeparam>
    /// <param name="requestBuilder">The Kiota-generated request builder.</param>
    /// <param name="exception">The exception to throw when this endpoint is called.</param>
    /// <param name="requestInfoPredicate">Optional additional conditions to match the request.</param>
    /// <returns>The request builder for fluent chaining.</returns>
    /// <example>
    /// <code>
    /// _client.Api.Funds[fundId].MockDeleteAsync&lt;FundItemRequestBuilder, Fund&gt;(
    ///     new ApiException("Cannot delete fund with active transactions") { ResponseStatusCode = 409 }
    /// );
    /// </code>
    /// </example>
    public static TBuilder MockDeleteAsync<TBuilder, TResponse>(
        this TBuilder requestBuilder,
        Exception exception,
        Func<RequestInformation, bool>? requestInfoPredicate = null
    )
        where TBuilder : BaseRequestBuilder
        where TResponse : IParsable
    {
        var mockAdapter = GetMockAdapter(requestBuilder);
        var (urlTemplate, pathParameters) = GetBuilderInfo(requestBuilder);

        mockAdapter
            .SendAsync<TResponse>(
                Arg.Is<RequestInformation>(req =>
                    MatchesBuilder(req, urlTemplate, pathParameters, Method.DELETE)
                    && (requestInfoPredicate == null || requestInfoPredicate(req))
                ),
                Arg.Any<ParsableFactory<TResponse>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>>(),
                Arg.Any<CancellationToken>()
            )
            .Throws(exception);

        return requestBuilder;
    }

    /// <summary>
    /// Mocks a DELETE request that returns a collection of objects.
    /// Some APIs return multiple items in DELETE responses (e.g., bulk delete operations).
    /// </summary>
    /// <typeparam name="TBuilder">The request builder type.</typeparam>
    /// <typeparam name="TResponse">The response item type (must implement IParsable).</typeparam>
    /// <param name="requestBuilder">The Kiota-generated request builder.</param>
    /// <param name="response">The collection to return when this endpoint is called.</param>
    /// <param name="requestInfoPredicate">Optional additional conditions to match the request.</param>
    /// <returns>The request builder for fluent chaining.</returns>
    /// <example>
    /// <code>
    /// var deletedFunds = new List&lt;Fund&gt;
    /// {
    ///     new Fund { Id = Guid.NewGuid(), Name = "Fund 1", Status = FundStatus.Deleted },
    ///     new Fund { Id = Guid.NewGuid(), Name = "Fund 2", Status = FundStatus.Deleted }
    /// };
    ///
    /// _client.Api.Funds.MockDeleteCollectionAsync(deletedFunds);
    /// </code>
    /// </example>
    public static TBuilder MockDeleteCollectionAsync<TBuilder, TResponse>(
        this TBuilder requestBuilder,
        IEnumerable<TResponse>? response,
        Func<RequestInformation, bool>? requestInfoPredicate = null
    )
        where TBuilder : BaseRequestBuilder
        where TResponse : IParsable
    {
        var mockAdapter = GetMockAdapter(requestBuilder);
        var (urlTemplate, pathParameters) = GetBuilderInfo(requestBuilder);

        mockAdapter
            .SendCollectionAsync<TResponse>(
                Arg.Is<RequestInformation>(req =>
                    MatchesBuilder(req, urlTemplate, pathParameters, Method.DELETE)
                    && (requestInfoPredicate == null || requestInfoPredicate(req))
                ),
                Arg.Any<ParsableFactory<TResponse>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(response);

        return requestBuilder;
    }

    /// <summary>
    /// Mocks a DELETE request that returns a collection and throws an exception.
    /// </summary>
    /// <typeparam name="TBuilder">The request builder type.</typeparam>
    /// <typeparam name="TResponse">The response item type (must implement IParsable).</typeparam>
    /// <param name="requestBuilder">The Kiota-generated request builder.</param>
    /// <param name="exception">The exception to throw when this endpoint is called.</param>
    /// <param name="requestInfoPredicate">Optional additional conditions to match the request.</param>
    /// <returns>The request builder for fluent chaining.</returns>
    /// <example>
    /// <code>
    /// _client.Api.Funds.MockDeleteCollectionAsync&lt;FundsRequestBuilder, Fund&gt;(
    ///     new ApiException("Bulk delete not allowed") { ResponseStatusCode = 403 }
    /// );
    /// </code>
    /// </example>
    public static TBuilder MockDeleteCollectionAsync<TBuilder, TResponse>(
        this TBuilder requestBuilder,
        Exception exception,
        Func<RequestInformation, bool>? requestInfoPredicate = null
    )
        where TBuilder : BaseRequestBuilder
        where TResponse : IParsable
    {
        var mockAdapter = GetMockAdapter(requestBuilder);
        var (urlTemplate, pathParameters) = GetBuilderInfo(requestBuilder);

        mockAdapter
            .SendCollectionAsync<TResponse>(
                Arg.Is<RequestInformation>(req =>
                    MatchesBuilder(req, urlTemplate, pathParameters, Method.DELETE)
                    && (requestInfoPredicate == null || requestInfoPredicate(req))
                ),
                Arg.Any<ParsableFactory<TResponse>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>>(),
                Arg.Any<CancellationToken>()
            )
            .Throws(exception);

        return requestBuilder;
    }

    /// <summary>
    /// Gets the URL template and path parameters from a request builder using reflection.
    /// </summary>
    private static (string urlTemplate, Dictionary<string, object> pathParameters) GetBuilderInfo(
        BaseRequestBuilder requestBuilder
    )
    {
        var urlTemplateProperty = typeof(BaseRequestBuilder).GetProperty(
            "UrlTemplate",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
        );

        var pathParametersProperty = typeof(BaseRequestBuilder).GetProperty(
            "PathParameters",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
        );

        if (urlTemplateProperty == null || pathParametersProperty == null)
        {
            throw new InvalidOperationException(
                "Could not find UrlTemplate or PathParameters properties on BaseRequestBuilder. "
                    + "This may indicate a version mismatch with Microsoft.Kiota.Abstractions."
            );
        }

        var urlTemplate = urlTemplateProperty.GetValue(requestBuilder) as string;
        var pathParameters =
            pathParametersProperty.GetValue(requestBuilder) as IDictionary<string, object>;

        if (urlTemplate == null || pathParameters == null)
        {
            throw new InvalidOperationException(
                "UrlTemplate or PathParameters are null. "
                    + "Ensure the request builder was properly initialized."
            );
        }

        return (urlTemplate, new Dictionary<string, object>(pathParameters));
    }

    /// <summary>
    /// Gets the mock IRequestAdapter from a request builder using reflection.
    /// </summary>
    private static IRequestAdapter GetMockAdapter(BaseRequestBuilder requestBuilder)
    {
        var adapterProperty = typeof(BaseRequestBuilder).GetProperty(
            "RequestAdapter",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
        );

        if (adapterProperty == null)
        {
            throw new InvalidOperationException(
                "Could not find RequestAdapter property on BaseRequestBuilder. "
                    + "This may indicate a version mismatch with Microsoft.Kiota.Abstractions."
            );
        }

        var adapter = adapterProperty.GetValue(requestBuilder) as IRequestAdapter;
        if (adapter == null)
        {
            throw new InvalidOperationException(
                "RequestAdapter is null. Ensure the request builder was created with GetMockableClient."
            );
        }

        return adapter;
    }

    /// <summary>
    /// Matches a request to a specific request builder by comparing URL template,
    /// path parameters, and HTTP method.
    /// </summary>
    private static bool MatchesBuilder(
        RequestInformation req,
        string urlTemplate,
        Dictionary<string, object> pathParameters,
        Method expectedMethod
    )
    {
        // Check HTTP method
        if (req.HttpMethod != expectedMethod)
        {
            return false;
        }

        // Check URL template match
        if (!string.Equals(req.UrlTemplate, urlTemplate, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Check path parameters match
        // Compare only the path parameters (excluding baseurl)
        foreach (var param in pathParameters)
        {
            if (param.Key == "baseurl")
                continue;

            if (!req.PathParameters.TryGetValue(param.Key, out var reqValue))
            {
                return false;
            }

            // Compare values (convert both to strings for comparison)
            var expectedStr = param.Value?.ToString() ?? string.Empty;
            var actualStr = reqValue?.ToString() ?? string.Empty;

            if (!string.Equals(expectedStr, actualStr, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}
