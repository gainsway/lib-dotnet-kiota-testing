using System.Reflection;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Serialization.Json;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.Exceptions;

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
    /// Mocks a POST request (no content response).
    /// </summary>
    /// <typeparam name="TBuilder">The request builder type.</typeparam>
    /// <param name="requestBuilder">The Kiota-generated request builder.</param>
    /// <param name="requestInfoPredicate">Optional additional conditions to match the request.</param>
    /// <returns>The request builder for fluent chaining.</returns>
    /// <remarks>
    /// For POST endpoints that return a body, use
    /// <see cref="MockPostAsync{TBuilder, TResponse}(TBuilder, TResponse, Func{RequestInformation, bool})"/>
    /// instead.
    /// </remarks>
    /// <example>
    /// <code>
    /// _client.Api.Funds[fundId].Status.MarkSetupComplete.MockPostAsync();
    /// </code>
    /// </example>
    public static TBuilder MockPostAsync<TBuilder>(
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
                    MatchesBuilder(req, urlTemplate, pathParameters, Method.POST)
                    && (requestInfoPredicate == null || requestInfoPredicate(req))
                ),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(Task.CompletedTask);

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
    /// Mocks a PUT request (no content response).
    /// </summary>
    /// <typeparam name="TBuilder">The request builder type.</typeparam>
    /// <param name="requestBuilder">The Kiota-generated request builder.</param>
    /// <param name="requestInfoPredicate">Optional additional conditions to match the request.</param>
    /// <returns>The request builder for fluent chaining.</returns>
    /// <remarks>
    /// For PUT endpoints that return a body, use
    /// <see cref="MockPutAsync{TBuilder, TResponse}(TBuilder, TResponse, Func{RequestInformation, bool})"/>
    /// instead.
    /// </remarks>
    /// <example>
    /// <code>
    /// _client.ApiInternal.Funds[fundId].MockPutAsync();
    /// </code>
    /// </example>
    public static TBuilder MockPutAsync<TBuilder>(
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
                    MatchesBuilder(req, urlTemplate, pathParameters, Method.PUT)
                    && (requestInfoPredicate == null || requestInfoPredicate(req))
                ),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(Task.CompletedTask);

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
    /// Mocks a PATCH request (no content response).
    /// </summary>
    /// <typeparam name="TBuilder">The request builder type.</typeparam>
    /// <param name="requestBuilder">The Kiota-generated request builder.</param>
    /// <param name="requestInfoPredicate">Optional additional conditions to match the request.</param>
    /// <returns>The request builder for fluent chaining.</returns>
    /// <remarks>
    /// For PATCH endpoints that return a body, use
    /// <see cref="MockPatchAsync{TBuilder, TResponse}(TBuilder, TResponse, Func{RequestInformation, bool})"/>
    /// instead. This is the common case for partial-update endpoints backed by a bare
    /// <c>[HttpPatch]</c> returning <c>IActionResult</c>/<c>ActionResult</c> with no body.
    /// </remarks>
    /// <example>
    /// <code>
    /// _client.Api.FundApplications["self"][id].MockPatchAsync();
    /// </code>
    /// </example>
    public static TBuilder MockPatchAsync<TBuilder>(
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
                    MatchesBuilder(req, urlTemplate, pathParameters, Method.PATCH)
                    && (requestInfoPredicate == null || requestInfoPredicate(req))
                ),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(Task.CompletedTask);

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
    /// Implementation behind <see cref="VerifyCallAssertion.WithBody{TBody}"/>: scans every
    /// recorded call to <paramref name="mockAdapter"/> matching the given
    /// method/URL/path-parameters/predicate, deserializes each candidate's body, and succeeds on
    /// the first one satisfying <paramref name="bodyPredicate"/>.
    /// </summary>
    /// <remarks>
    /// Requires the mock client to have been created with
    /// <see cref="KiotaClientMockExtensions.GetMockableClient{T}"/>, which wires a real
    /// <c>JsonSerializationWriterFactory</c> — without it, <c>RequestInformation.Content</c>
    /// stays null and this always fails with a "no matching call" assertion.
    /// </remarks>
    internal static async Task VerifyRequestBodyCoreAsync<TBody>(
        IRequestAdapter mockAdapter,
        string urlTemplate,
        Dictionary<string, object> pathParameters,
        Method expectedMethod,
        Func<TBody, bool> bodyPredicate,
        Func<RequestInformation, bool>? requestInfoPredicate
    )
        where TBody : IParsable
    {
        var parsableFactory = GetParsableFactory<TBody>();
        var parseNodeFactory = new JsonParseNodeFactory();

        var candidates = mockAdapter
            .ReceivedCalls()
            .Select(call => call.GetArguments().OfType<RequestInformation>().FirstOrDefault())
            .OfType<RequestInformation>()
            .Where(req =>
                MatchesBuilder(req, urlTemplate, pathParameters, expectedMethod)
                && (requestInfoPredicate == null || requestInfoPredicate(req))
                && req.Content != null
            );

        var rejectedBodies = new List<string>();

        foreach (var req in candidates)
        {
            var contentType = req.Headers.TryGetValue("Content-Type", out var contentTypes)
                ? contentTypes.FirstOrDefault() ?? parseNodeFactory.ValidContentType
                : parseNodeFactory.ValidContentType;

            using var buffered = new MemoryStream();
            await req.Content.CopyToAsync(buffered);
            buffered.Position = 0;

            var parseNode = await parseNodeFactory.GetRootParseNodeAsync(contentType, buffered);
            var body = (TBody)parseNode.GetObjectValue(parsableFactory);

            if (bodyPredicate(body))
            {
                return;
            }

            buffered.Position = 0;
            using var reader = new StreamReader(buffered);
            rejectedBodies.Add(await reader.ReadToEndAsync());
        }

        if (rejectedBodies.Count == 0)
        {
            throw new ReceivedCallsException(
                $"Expected a {expectedMethod} request to {urlTemplate} whose body satisfies "
                    + "the given predicate, but no matching call was found."
            );
        }

        var bodiesText = string.Join(
            Environment.NewLine,
            rejectedBodies.Select((json, i) => $"  [{i}]: {json}")
        );

        throw new ReceivedCallsException(
            $"Expected a {expectedMethod} request to {urlTemplate} whose body satisfies "
                + $"the given predicate. {rejectedBodies.Count} matching call(s) were found by "
                + $"method/URL/path-parameters, but none had a body satisfying the predicate. "
                + $"Actual bod{(rejectedBodies.Count == 1 ? "y" : "ies")} received:{Environment.NewLine}{bodiesText}"
        );
    }

    /// <summary>
    /// Resolves the static <c>CreateFromDiscriminatorValue(IParseNode)</c> factory method that
    /// every Kiota-generated model exposes, as a <see cref="ParsableFactory{TBody}"/> delegate.
    /// </summary>
    private static ParsableFactory<TBody> GetParsableFactory<TBody>()
        where TBody : IParsable
    {
        var factoryMethod = typeof(TBody).GetMethod(
            "CreateFromDiscriminatorValue",
            BindingFlags.Public | BindingFlags.Static
        );

        if (factoryMethod == null)
        {
            throw new InvalidOperationException(
                $"{typeof(TBody).Name} does not expose a static CreateFromDiscriminatorValue(IParseNode) "
                    + "method. Every Kiota-generated model has one; this type may not be a generated model."
            );
        }

        return (ParsableFactory<TBody>)
            Delegate.CreateDelegate(typeof(ParsableFactory<TBody>), factoryMethod);
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
    /// Verifies that a GET request was sent to this endpoint the expected number of times.
    /// </summary>
    /// <typeparam name="TBuilder">The request builder type.</typeparam>
    /// <param name="requestBuilder">The Kiota-generated request builder.</param>
    /// <param name="times">
    /// The expected number of calls. Defaults to <see cref="Times.AtLeastOnce"/>, which
    /// asserts the endpoint was called at least once without pinning an exact count.
    /// Use <see cref="Times.Never"/> to assert it was never called.
    /// </param>
    /// <param name="requestInfoPredicate">Optional additional conditions the request must match.</param>
    /// <returns>
    /// A pending <see cref="VerifyCallAssertion"/>. Await it directly for a count-only check.
    /// </returns>
    /// <remarks>
    /// Matches the call regardless of the endpoint's response shape (single object, collection,
    /// or primitive) — no response type argument needed, unlike a raw NSubstitute
    /// <c>Received().SendAsync&lt;T&gt;(...)</c> assertion, which is pinned to one shape.
    /// This must be awaited. NSubstitute evaluates the assertion when the returned task is
    /// created, but leaving it unawaited produces a compiler warning and can mask failures.
    /// </remarks>
    /// <example>
    /// <code>
    /// var fundId = Guid.NewGuid();
    /// _client.Api.Funds[fundId].MockGetAsync(fund);
    ///
    /// await _service.GetFundAsync(fundId);
    ///
    /// // Called at least once - type-safe, no URL strings, no response type argument!
    /// await _client.Api.Funds[fundId].VerifyGetAsync();
    ///
    /// // Exact counts
    /// await _client.Api.Funds[fundId].VerifyGetAsync(Times.Once);
    /// await _client.Api.Funds[fundId].VerifyGetAsync(Times.Exactly(3));
    ///
    /// // Assert an endpoint was never called
    /// await _client.Api.Funds[otherId].VerifyGetAsync(Times.Never);
    /// </code>
    /// </example>
    public static VerifyCallAssertion VerifyGetAsync<TBuilder>(
        this TBuilder requestBuilder,
        Times? times = null,
        Func<RequestInformation, bool>? requestInfoPredicate = null
    )
        where TBuilder : BaseRequestBuilder =>
        VerifyAnyResponseShapeAsync(requestBuilder, Method.GET, times, requestInfoPredicate);

    /// <summary>
    /// Verifies that a POST request was sent to this endpoint the expected number of times.
    /// </summary>
    /// <typeparam name="TBuilder">The request builder type.</typeparam>
    /// <param name="requestBuilder">The Kiota-generated request builder.</param>
    /// <param name="times">
    /// The expected number of calls. Defaults to <see cref="Times.AtLeastOnce"/>.
    /// </param>
    /// <param name="requestInfoPredicate">Optional additional conditions the request must match.</param>
    /// <returns>
    /// A pending <see cref="VerifyCallAssertion"/>. Await it directly for a count-only check,
    /// or chain <see cref="VerifyCallAssertion.WithBody{TBody}"/> to also assert on the
    /// deserialized request body.
    /// </returns>
    /// <remarks>
    /// Matches the call regardless of the endpoint's response shape (single object, collection,
    /// or no content) — a bare <c>[HttpPost]</c> action/status-transition endpoint and one
    /// returning a created resource verify the same way.
    /// </remarks>
    /// <example>
    /// <code>
    /// await _client.Api.Funds.VerifyPostAsync(Times.Once);
    /// await _client.Api.Funds[fundId].Status.MarkSetupComplete.VerifyPostAsync(Times.Once);
    /// </code>
    /// </example>
    public static VerifyCallAssertion VerifyPostAsync<TBuilder>(
        this TBuilder requestBuilder,
        Times? times = null,
        Func<RequestInformation, bool>? requestInfoPredicate = null
    )
        where TBuilder : BaseRequestBuilder =>
        VerifyAnyResponseShapeAsync(requestBuilder, Method.POST, times, requestInfoPredicate);

    /// <summary>
    /// Verifies that a PUT request was sent to this endpoint the expected number of times.
    /// </summary>
    /// <typeparam name="TBuilder">The request builder type.</typeparam>
    /// <param name="requestBuilder">The Kiota-generated request builder.</param>
    /// <param name="times">
    /// The expected number of calls. Defaults to <see cref="Times.AtLeastOnce"/>.
    /// </param>
    /// <param name="requestInfoPredicate">Optional additional conditions the request must match.</param>
    /// <returns>
    /// A pending <see cref="VerifyCallAssertion"/>. Await it directly for a count-only check,
    /// or chain <see cref="VerifyCallAssertion.WithBody{TBody}"/> to also assert on the
    /// deserialized request body.
    /// </returns>
    /// <remarks>
    /// Matches the call regardless of the endpoint's response shape (single object or no
    /// content) — a fire-and-forget replication PUT between services and one returning the
    /// updated resource verify the same way.
    /// </remarks>
    /// <example>
    /// <code>
    /// await _client.Api.Funds[fundId].VerifyPutAsync(Times.Once);
    ///
    /// // count + body, in one assertion
    /// await _client.ApiInternal.Funds[fundId]
    ///     .VerifyPutAsync(Times.Once)
    ///     .WithBody&lt;FundUpdateDto&gt;(body => body.RequiresMarket == true);
    /// </code>
    /// </example>
    public static VerifyCallAssertion VerifyPutAsync<TBuilder>(
        this TBuilder requestBuilder,
        Times? times = null,
        Func<RequestInformation, bool>? requestInfoPredicate = null
    )
        where TBuilder : BaseRequestBuilder =>
        VerifyAnyResponseShapeAsync(requestBuilder, Method.PUT, times, requestInfoPredicate);

    /// <summary>
    /// Verifies that a PATCH request was sent to this endpoint the expected number of times.
    /// </summary>
    /// <typeparam name="TBuilder">The request builder type.</typeparam>
    /// <param name="requestBuilder">The Kiota-generated request builder.</param>
    /// <param name="times">
    /// The expected number of calls. Defaults to <see cref="Times.AtLeastOnce"/>.
    /// </param>
    /// <param name="requestInfoPredicate">Optional additional conditions the request must match.</param>
    /// <returns>
    /// A pending <see cref="VerifyCallAssertion"/>. Await it directly for a count-only check,
    /// or chain <see cref="VerifyCallAssertion.WithBody{TBody}"/> to also assert on the
    /// deserialized request body.
    /// </returns>
    /// <remarks>
    /// Matches the call regardless of the endpoint's response shape (single object or no
    /// content) — the common partial-update case, a bare <c>[HttpPatch]</c> returning
    /// <c>IActionResult</c>/<c>ActionResult</c> with no body, verifies the same way as one
    /// returning the patched resource.
    /// </remarks>
    /// <example>
    /// <code>
    /// await _client.Api.FundApplications["self"][id].VerifyPatchAsync(Times.Once);
    /// </code>
    /// </example>
    public static VerifyCallAssertion VerifyPatchAsync<TBuilder>(
        this TBuilder requestBuilder,
        Times? times = null,
        Func<RequestInformation, bool>? requestInfoPredicate = null
    )
        where TBuilder : BaseRequestBuilder =>
        VerifyAnyResponseShapeAsync(requestBuilder, Method.PATCH, times, requestInfoPredicate);

    /// <summary>
    /// Verifies that a DELETE request was sent to this endpoint the expected number of times.
    /// </summary>
    /// <typeparam name="TBuilder">The request builder type.</typeparam>
    /// <param name="requestBuilder">The Kiota-generated request builder.</param>
    /// <param name="times">
    /// The expected number of calls. Defaults to <see cref="Times.AtLeastOnce"/>.
    /// </param>
    /// <param name="requestInfoPredicate">Optional additional conditions the request must match.</param>
    /// <returns>
    /// A pending <see cref="VerifyCallAssertion"/>. Await it directly for a count-only check.
    /// </returns>
    /// <remarks>
    /// Matches the call regardless of the endpoint's response shape (no content, single object,
    /// or collection) — no response type argument needed.
    /// </remarks>
    /// <example>
    /// <code>
    /// await _client.Api.Funds[fundId].VerifyDeleteAsync(Times.Once);
    /// </code>
    /// </example>
    public static VerifyCallAssertion VerifyDeleteAsync<TBuilder>(
        this TBuilder requestBuilder,
        Times? times = null,
        Func<RequestInformation, bool>? requestInfoPredicate = null
    )
        where TBuilder : BaseRequestBuilder =>
        VerifyAnyResponseShapeAsync(requestBuilder, Method.DELETE, times, requestInfoPredicate);

    /// <summary>
    /// Builds the pending assertion behind every collapsed <c>Verify*Async&lt;TBuilder&gt;(Times)</c>
    /// method. The count check (run by <see cref="VerifyAnyResponseShapeCoreAsync"/> when the
    /// returned <see cref="VerifyCallAssertion"/> is awaited) matches calls across every
    /// <see cref="IRequestAdapter"/> response shape (<c>SendAsync</c>, <c>SendCollectionAsync</c>,
    /// <c>SendPrimitiveAsync</c>, <c>SendPrimitiveCollectionAsync</c>, <c>SendNoContentAsync</c>)
    /// instead of asserting against one specific overload — so verification never needs to know,
    /// or be told via a type argument, what shape the endpoint's response actually is.
    /// </summary>
    private static VerifyCallAssertion VerifyAnyResponseShapeAsync<TBuilder>(
        TBuilder requestBuilder,
        Method expectedMethod,
        Times? times,
        Func<RequestInformation, bool>? requestInfoPredicate
    )
        where TBuilder : BaseRequestBuilder
    {
        var mockAdapter = GetMockAdapter(requestBuilder);
        var (urlTemplate, pathParameters) = GetBuilderInfo(requestBuilder);

        return new VerifyCallAssertion(
            mockAdapter,
            urlTemplate,
            pathParameters,
            expectedMethod,
            times ?? Times.AtLeastOnce,
            requestInfoPredicate
        );
    }

    /// <summary>
    /// Shared implementation behind <see cref="VerifyAnyResponseShapeAsync{TBuilder}"/> and
    /// <see cref="VerifyCallAssertion"/>'s count check.
    /// </summary>
    internal static Task VerifyAnyResponseShapeCoreAsync(
        IRequestAdapter mockAdapter,
        string urlTemplate,
        Dictionary<string, object> pathParameters,
        Method expectedMethod,
        Times times,
        Func<RequestInformation, bool>? requestInfoPredicate
    )
    {
        var matchCount = mockAdapter
            .ReceivedCalls()
            .Select(call => call.GetArguments().OfType<RequestInformation>().FirstOrDefault())
            .OfType<RequestInformation>()
            .Count(req =>
                MatchesBuilder(req, urlTemplate, pathParameters, expectedMethod)
                && (requestInfoPredicate == null || requestInfoPredicate(req))
            );

        var satisfied = times.Count is int exactCount ? matchCount == exactCount : matchCount >= 1;

        if (!satisfied)
        {
            throw new ReceivedCallsException(
                $"Expected a {expectedMethod} request to {urlTemplate} {times}, "
                    + $"but received {matchCount} matching call(s)."
            );
        }

        return Task.CompletedTask;
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
