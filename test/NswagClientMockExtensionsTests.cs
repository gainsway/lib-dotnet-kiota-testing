using System.Net;
using System.Text;
using Newtonsoft.Json;
using NSubstitute.Exceptions;

namespace Gainsway.NSwag.Testing.Tests;

[TestFixture]
public class NswagClientMockExtensionsTests
{
    private ExampleNswagClient _client = null!;

    [SetUp]
    public void SetUp() =>
        _client = NswagClientMockExtensions.GetMockableClient<ExampleNswagClient>();

    [Test]
    public async Task MockGetAsync_DeserializesResponseAndVerifiesExactPath()
    {
        var expected = new Item { Id = 42, Name = "test" };
        _client.MockGetAsync("/api/items/{id}", expected);

        var actual = await _client.GetItemAsync(42);

        Assert.That(actual?.Name, Is.EqualTo("test"));
        await _client.VerifyGetAsync("/api/items/42", Times.Once);
        await _client.VerifyGetAsync("/api/items/43", Times.Never);
    }

    [Test]
    public async Task MockGetCollectionAsync_ReturnsCollection()
    {
        _client.MockGetCollectionAsync("/api/items", new[] { new Item { Id = 1 } });

        var items = await _client.ListItemsAsync();

        Assert.That(items, Has.Length.EqualTo(1));
        await _client.VerifyGetAsync("/api/items", Times.Once);
    }

    [Test]
    public async Task VerifyPostAsync_WithBodyChecksSerializedRequest()
    {
        _client.MockPostAsync("/api/items");
        await _client.CreateItemAsync(new Item { Id = 7, Name = "created" });

        await _client
            .VerifyPostAsync("/api/items", Times.Once)
            .WithBody<Item>(item => item.Id == 7 && item.Name == "created");
    }

    [Test]
    public async Task VerifyPostAsync_WithBodyRejectsWrongBody()
    {
        _client.MockPostAsync("/api/items");
        await _client.CreateItemAsync(new Item { Id = 7 });

        await Assert.ThrowsAsync<ReceivedCallsException>(async () =>
            await _client.VerifyPostAsync("/api/items").WithBody<Item>(item => item.Id == 8)
        );
    }

    [Test]
    public async Task RequestMatching_UsesActualMethodAndQuery()
    {
        _client.MockGetAsync("/api/items/5?detail=true", new Item { Id = 5 });

        var item = await _client.GetItemAsync(5, detail: true);

        Assert.That(item?.Id, Is.EqualTo(5));
        await _client.VerifyGetAsync("/api/items/5?detail=true", Times.Once);
        await _client.VerifyGetAsync("/api/items/5?detail=false", Times.Never);
    }

    [Test]
    public async Task MockError_ProducesFailedHttpResponse()
    {
        _client.MockGetError("/api/items/99", HttpStatusCode.NotFound);

        var error = await Assert.ThrowsAsync<HttpRequestException>(() => _client.GetItemAsync(99));
        Assert.That(error!.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await _client.VerifyGetAsync("/api/items/99", Times.Once);
    }

    [Test]
    public async Task UnmatchedRequest_ReturnsNotFoundRatherThanCallingNetwork()
    {
        await Assert.ThrowsAsync<HttpRequestException>(() => _client.GetItemAsync(999));
    }

    [Test]
    public async Task MockException_RaisesTransportFailure()
    {
        _client.MockGetAsync("/api/items/1", new HttpRequestException("offline"));
        var error = await Assert.ThrowsAsync<HttpRequestException>(() => _client.GetItemAsync(1));
        Assert.That(error!.Message, Is.EqualTo("offline"));
    }

    [Test]
    public void MissingHttpClientConstructor_IsRejected()
    {
        Assert.Throws<MissingMethodException>(() =>
            NswagClientMockExtensions.GetMockableClient<object>()
        );
    }

    [Test]
    public async Task CapturedRequests_IncludePathAndBody()
    {
        _client.MockPostAsync("/api/items");
        await _client.CreateItemAsync(new Item { Id = 11 });

        var request = _client.GetMockRequests().Single();

        Assert.That(request.Method, Is.EqualTo(HttpMethod.Post));
        Assert.That(request.Path, Is.EqualTo("/api/items"));
        Assert.That(request.Body, Does.Contain("11"));
    }

    public sealed class Item
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    public sealed class ExampleNswagClient(HttpClient httpClient)
    {
        public async Task<Item?> GetItemAsync(int id, bool? detail = null)
        {
            var path = $"api/items/{id}";
            if (detail is not null)
            {
                path += $"?detail={detail.Value.ToString().ToLowerInvariant()}";
            }

            using var response = await httpClient.GetAsync(path);
            response.EnsureSuccessStatusCode();
            return JsonConvert.DeserializeObject<Item>(
                await response.Content.ReadAsStringAsync()
            );
        }

        public async Task<Item[]> ListItemsAsync()
        {
            using var response = await httpClient.GetAsync("api/items");
            response.EnsureSuccessStatusCode();
            return JsonConvert.DeserializeObject<Item[]>(
                await response.Content.ReadAsStringAsync()
            )!;
        }

        public async Task CreateItemAsync(Item item)
        {
            using var content = new StringContent(
                JsonConvert.SerializeObject(item),
                Encoding.UTF8,
                "application/json"
            );
            using var response = await httpClient.PostAsync("api/items", content);
            response.EnsureSuccessStatusCode();
        }
    }
}
