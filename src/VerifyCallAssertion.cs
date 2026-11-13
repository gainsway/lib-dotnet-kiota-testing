using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using NSubstitute.Exceptions;

namespace Gainsway.NSwag.Testing;

/// <summary>
/// An awaitable request-count assertion; can also verify the JSON request body.
/// </summary>
public readonly struct VerifyCallAssertion
{
    private readonly MockHttpMessageHandler _handler;
    private readonly HttpMethod _method;
    private readonly string _path;
    private readonly Times _times;
    private readonly Func<RecordedRequest, bool>? _predicate;

    internal VerifyCallAssertion(
        MockHttpMessageHandler handler,
        HttpMethod method,
        string path,
        Times times,
        Func<RecordedRequest, bool>? predicate
    )
    {
        _handler = handler;
        _method = method;
        _path = path;
        _times = times;
        _predicate = predicate;
    }

    public TaskAwaiter GetAwaiter() => VerifyCountAsync().GetAwaiter();

    public async Task WithBody<TBody>(Func<TBody, bool> bodyPredicate)
    {
        ArgumentNullException.ThrowIfNull(bodyPredicate);
        await VerifyCountAsync();
        if (_times.Count is 0)
        {
            return;
        }

        foreach (var request in _handler.Matching(_method, _path, _predicate))
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
            $"No matching {_method} {_path} request had a body satisfying the assertion."
        );
    }

    private Task VerifyCountAsync()
    {
        var count = _handler.Matching(_method, _path, _predicate).Count;
        if (_times.Count is { } expected && count != expected || _times.Count is null && count < 1)
        {
            throw new ReceivedCallsException(
                $"Expected {_method} {_path} {_times}, but observed {count} call(s)."
            );
        }

        return Task.CompletedTask;
    }
}
