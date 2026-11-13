namespace Gainsway.NSwag.Testing;

/// <summary>A snapshot of an HTTP request sent by a generated client.</summary>
public sealed record RecordedRequest(
    HttpMethod Method,
    Uri Uri,
    string? Body,
    IReadOnlyDictionary<string, string[]> Headers
)
{
    public string Path => Uri.AbsolutePath;
    public string Query => Uri.Query;
}
