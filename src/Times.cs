namespace Gainsway.NSwag.Testing;

/// <summary>Expected call-count assertion for recorded HTTP requests.</summary>
public sealed class Times
{
    private Times(int? count) => Count = count;

    internal int? Count { get; }

    public static Times AtLeastOnce { get; } = new(null);
    public static Times Once { get; } = new(1);
    public static Times Never { get; } = new(0);

    public static Times Exactly(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return new Times(count);
    }

    public static implicit operator Times(int count) => Exactly(count);

    public override string ToString() =>
        Count is null ? "at least once" : $"exactly {Count} time(s)";
}
