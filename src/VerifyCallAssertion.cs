using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using NSubstitute.Exceptions;

namespace Gainsway.NSwag.Testing;

/// <summary>
/// An awaitable request-count assertion; can also verify the JSON request body.
/// </summary>
public readonly struct VerifyCallAssertion(
    MockHttpMessageHandler handler,
    HttpMethod method,
    string path,
    Times times,
    Func<RecordedRequest, bool>? predicate
)
{
    public TaskAwaiter GetAwaiter() => VerifyCountAsync().GetAwaiter();

    public async Task WithBody<TBody>(Func<TBody, bool> bodyPredicate)
    {
        ArgumentNullException.ThrowIfNull(bodyPredicate);
        await VerifyCountAsync();
        if (times.Count is 0)
        {
            return;
        }

        foreach (var request in handler.Matching(method, path, predicate))
        {
            if (string.IsNullOrWhiteSpace(request.Body))
            {
                continue;
            }

            TBody? value;
            try
            {
                value = JsonConvert.DeserializeObject<TBody>(
                    request.Body,
                    MockHttpMessageHandler.SerializerSettings
                );
            }
            catch (JsonException)
            {
                continue;
            }

            if (value is not null && bodyPredicate(value))
            {
                return;
            }
        }

        throw new ReceivedCallsException(
            $"No matching {method} {path} request had a body satisfying the assertion."
        );
    }

    private Task VerifyCountAsync()
    {
        var count = handler.Matching(method, path, predicate).Count;
        if (times.Count is { } expected && count != expected
            || times.Count is null && count < 1)
        {
            throw new ReceivedCallsException(
                $"Expected {method} {path} {times}, but observed {count} call(s)."
            );
        }

        return Task.CompletedTask;
    }
}
