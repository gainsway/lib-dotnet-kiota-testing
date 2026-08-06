namespace Gainsway.Kiota.Testing;

/// <summary>
/// Specifies the number of calls expected by a <c>Verify*</c> assertion.
/// </summary>
/// <remarks>
/// Mirrors the semantics of NSubstitute's <c>Received()</c> overloads:
/// <see cref="AtLeastOnce"/> corresponds to <c>Received()</c>, while
/// <see cref="Exactly(int)"/> corresponds to <c>Received(n)</c>.
/// </remarks>
/// <example>
/// <code>
/// await _client.Api.Funds[fundId].VerifyGetAsync&lt;FundItemRequestBuilder, Fund&gt;();
/// await _client.Api.Funds[fundId].VerifyGetAsync&lt;FundItemRequestBuilder, Fund&gt;(Times.Once);
/// await _client.Api.Funds[otherId].VerifyGetAsync&lt;FundItemRequestBuilder, Fund&gt;(Times.Never);
/// await _client.Api.Funds[fundId].VerifyGetAsync&lt;FundItemRequestBuilder, Fund&gt;(Times.Exactly(3));
/// </code>
/// </example>
public sealed class Times
{
    private Times(int? count)
    {
        Count = count;
    }

    /// <summary>
    /// The exact number of expected calls, or <see langword="null"/> when any number of
    /// calls of at least one is acceptable.
    /// </summary>
    internal int? Count { get; }

    /// <summary>
    /// Asserts the endpoint was called at least once, without pinning an exact count.
    /// This is the default when no <see cref="Times"/> value is supplied.
    /// </summary>
    public static Times AtLeastOnce { get; } = new(null);

    /// <summary>
    /// Asserts the endpoint was called exactly once.
    /// </summary>
    public static Times Once { get; } = new(1);

    /// <summary>
    /// Asserts the endpoint was never called.
    /// </summary>
    public static Times Never { get; } = new(0);

    /// <summary>
    /// Asserts the endpoint was called exactly <paramref name="count"/> times.
    /// </summary>
    /// <param name="count">The exact number of expected calls. Must not be negative.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="count"/> is negative.
    /// </exception>
    public static Times Exactly(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return new Times(count);
    }

    /// <summary>
    /// Converts an integer to an exact call count, so <c>VerifyGetAsync(3)</c> is
    /// equivalent to <c>VerifyGetAsync(Times.Exactly(3))</c>.
    /// </summary>
    /// <param name="count">The exact number of expected calls. Must not be negative.</param>
    public static implicit operator Times(int count) => Exactly(count);

    /// <inheritdoc />
    public override string ToString() =>
        Count is null ? "at least once" : $"exactly {Count} time(s)";
}
