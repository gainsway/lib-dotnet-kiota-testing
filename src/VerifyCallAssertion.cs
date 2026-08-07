using System.Runtime.CompilerServices;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using NSubstitute.Exceptions;

namespace Gainsway.Kiota.Testing;

/// <summary>
/// A pending call-count assertion returned by every <c>Verify*Async&lt;TBuilder&gt;</c> method
/// (<c>VerifyGetAsync</c>, <c>VerifyPostAsync</c>, <c>VerifyPutAsync</c>, <c>VerifyPatchAsync</c>,
/// <c>VerifyDeleteAsync</c>).
/// </summary>
/// <remarks>
/// Matches a call regardless of which <see cref="IRequestAdapter"/> response shape the endpoint
/// actually uses (single object, collection, primitive, or no content) — callers never declare
/// a response type just to verify a call happened. Directly awaitable for a count-only assertion.
/// Chain <see cref="WithBody{TBody}"/> to additionally assert on the deserialized request body
/// for PUT/POST/PATCH.
/// </remarks>
/// <example>
/// <code>
/// // count only, whatever the response shape turns out to be
/// await mockClient.ApiInternal.Funds[fundId].VerifyPutAsync(Times.Once);
///
/// // count + body, in one assertion
/// await mockClient.ApiInternal.Funds[fundId]
///     .VerifyPutAsync(Times.Once)
///     .WithBody&lt;FundUpdateDto&gt;(body => body.RequiresMarket == true);
/// </code>
/// </example>
public readonly struct VerifyCallAssertion
{
    private readonly IRequestAdapter _mockAdapter;
    private readonly string _urlTemplate;
    private readonly Dictionary<string, object> _pathParameters;
    private readonly Method _method;
    private readonly Times _times;
    private readonly Func<RequestInformation, bool>? _requestInfoPredicate;

    internal VerifyCallAssertion(
        IRequestAdapter mockAdapter,
        string urlTemplate,
        Dictionary<string, object> pathParameters,
        Method method,
        Times times,
        Func<RequestInformation, bool>? requestInfoPredicate
    )
    {
        _mockAdapter = mockAdapter;
        _urlTemplate = urlTemplate;
        _pathParameters = pathParameters;
        _method = method;
        _times = times;
        _requestInfoPredicate = requestInfoPredicate;
    }

    /// <summary>
    /// Awaits the call-count assertion only.
    /// </summary>
    public TaskAwaiter GetAwaiter() => VerifyCountAsync().GetAwaiter();

    /// <summary>
    /// Additionally asserts that at least one matching call's deserialized request body
    /// satisfies <paramref name="bodyPredicate"/>.
    /// </summary>
    /// <typeparam name="TBody">
    /// The request body type (must implement <see cref="IParsable"/> and expose a static
    /// <c>CreateFromDiscriminatorValue(IParseNode)</c> factory method, as every
    /// Kiota-generated model does).
    /// </typeparam>
    /// <param name="bodyPredicate">A predicate over the deserialized request body.</param>
    /// <returns>A task that completes once both assertions have been evaluated.</returns>
    /// <remarks>
    /// The call-count assertion runs first, with the same semantics as awaiting this struct
    /// directly — so <c>VerifyPutAsync(Times.Never).WithBody(...)</c> still fails on any call
    /// existing at all, before the body predicate is ever considered.
    /// </remarks>
    /// <exception cref="ReceivedCallsException">
    /// Thrown when the call count doesn't match, or when it matches but no matching call's
    /// body satisfies <paramref name="bodyPredicate"/>.
    /// </exception>
    public async Task WithBody<TBody>(Func<TBody, bool> bodyPredicate)
        where TBody : IParsable
    {
        await VerifyCountAsync();

        if (_times.Count is 0)
        {
            // Times.Never: nothing was expected to be sent, so there is no body to inspect.
            return;
        }

        await RequestBuilderMockExtensions.VerifyRequestBodyCoreAsync(
            _mockAdapter,
            _urlTemplate,
            _pathParameters,
            _method,
            bodyPredicate,
            _requestInfoPredicate
        );
    }

    private Task VerifyCountAsync() =>
        RequestBuilderMockExtensions.VerifyAnyResponseShapeCoreAsync(
            _mockAdapter,
            _urlTemplate,
            _pathParameters,
            _method,
            _times,
            _requestInfoPredicate
        );
}
