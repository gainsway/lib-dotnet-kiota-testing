using Gainsway.Kiota.Testing;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using NUnit.Framework;

namespace Gainsway.Kiota.Testing.Tests;

/// <summary>
/// Tests for the new type-safe request builder mocking API.
/// These tests demonstrate the improved API that eliminates URL string matching.
/// </summary>
[TestFixture]
public class RequestBuilderMockExtensionsTests
{
    private TypeSafeTestClient _mockClient;

    [SetUp]
    public void Setup()
    {
        _mockClient = KiotaClientMockExtensions.GetMockableClient<TypeSafeTestClient>();
    }

    [Test]
    public void MockGetAsync_WithSingleObject_ShouldSetupMockSuccessfully()
    {
        // Arrange
        var expectedResponse = new TestResponse { Value = "test-value" };
        var itemId = "123";

        // Act - Type-safe API setup
        _mockClient.Api.Items[itemId].MockGetAsync(expectedResponse);

        // Assert - Verify mock setup completed without exceptions
        Assert.That(_mockClient, Is.Not.Null);
        Assert.Pass(
            "Mock setup successful using type-safe API: _mockClient.Api.Items[itemId].MockGetAsync(response)"
        );
    }

    [Test]
    public void MockGetAsync_WithString_ShouldSetupMockSuccessfully()
    {
        // Arrange
        var expectedStatus = "operational";

        // Act - Type-safe API setup
        _mockClient.Api.Status.MockGetAsync(expectedStatus);

        // Assert - Verify mock setup completed without exceptions
        Assert.That(_mockClient, Is.Not.Null);
        Assert.Pass(
            "Mock setup successful using type-safe API: _mockClient.Api.Status.MockGetAsync(string)"
        );
    }

    [Test]
    public void MockGetCollectionAsync_ShouldSetupMockSuccessfully()
    {
        // Arrange
        var expectedItems = new List<TestResponse>
        {
            new TestResponse { Value = "item-1" },
            new TestResponse { Value = "item-2" },
            new TestResponse { Value = "item-3" },
        };

        // Act - Type-safe API setup
        _mockClient.Api.Items.MockGetCollectionAsync(expectedItems);

        // Assert - Verify mock setup completed without exceptions
        Assert.That(_mockClient, Is.Not.Null);
        Assert.Pass(
            "Mock setup successful using type-safe API: _mockClient.Api.Items.MockGetCollectionAsync(collection)"
        );
    }

    [Test]
    public void MockPostAsync_ShouldSetupMockSuccessfully()
    {
        // Arrange
        var createdResponse = new TestResponse { Value = "created-item" };

        // Act - Type-safe API setup
        _mockClient.Api.Items.MockPostAsync(createdResponse);

        // Assert - Verify mock setup completed without exceptions
        Assert.That(_mockClient, Is.Not.Null);
        Assert.Pass(
            "Mock setup successful using type-safe API: _mockClient.Api.Items.MockPostAsync(response)"
        );
    }

    [Test]
    public void MockDeleteAsync_ShouldSetupMockSuccessfully()
    {
        // Act - Type-safe API setup
        _mockClient.Api.Items["123"].MockDeleteAsync();

        // Assert - Verify mock setup completed without exceptions
        Assert.That(_mockClient, Is.Not.Null);
        Assert.Pass(
            "Mock setup successful using type-safe API: _mockClient.Api.Items[id].MockDeleteAsync()"
        );
    }

    [Test]
    public void MockGetAsyncException_ShouldSetupMockSuccessfully()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Item not found");

        // Act - Type-safe API setup with exception overload
        _mockClient
            .Api.Items["999"]
            .MockGetAsync<ItemRequestBuilder, TestResponse>(expectedException);

        // Assert - Verify mock setup completed without exceptions
        Assert.That(_mockClient, Is.Not.Null);
        Assert.Pass(
            "Mock setup successful using type-safe API: _mockClient.Api.Items[id].MockGetAsync(exception)"
        );
    }

    [Test]
    public void MockGetAsync_WithPredicate_ShouldSetupMockSuccessfully()
    {
        // Arrange
        var expectedResponse = new TestResponse { Value = "authorized-data" };

        // Act - Type-safe API setup with predicate
        _mockClient
            .Api.Items["123"]
            .MockGetAsync(expectedResponse, req => req.Headers.ContainsKey("Authorization"));

        // Assert - Verify mock setup completed without exceptions
        Assert.That(_mockClient, Is.Not.Null);
        Assert.Pass(
            "Mock setup successful using type-safe API with predicate: _mockClient.Api.Items[id].MockGetAsync(response, predicate)"
        );
    }

    [Test]
    public void MockGetAsync_WithDifferentIds_ShouldSetupMultipleMocksSuccessfully()
    {
        // Arrange
        var item1 = new TestResponse { Value = "item-1-data" };
        var item2 = new TestResponse { Value = "item-2-data" };

        // Act - Type-safe mocking with different IDs
        _mockClient.Api.Items["item-1"].MockGetAsync(item1);
        _mockClient.Api.Items["item-2"].MockGetAsync(item2);

        // Assert - Verify both mock setups completed without exceptions
        Assert.That(_mockClient, Is.Not.Null);
        Assert.Pass(
            "Multiple mocks setup successfully using type-safe API: _mockClient.Api.Items[id1].MockGetAsync() and _mockClient.Api.Items[id2].MockGetAsync()"
        );
    }
}

#region Test Helper Classes

/// <summary>
/// Extended TestRequestBuilder with Api structure for type-safe mocking
/// </summary>
public class TypeSafeTestClient : BaseRequestBuilder
{
    public ApiRequestBuilder Api { get; }

    // Expose RequestAdapter for testing
    public new IRequestAdapter RequestAdapter => base.RequestAdapter;

    public TypeSafeTestClient(IRequestAdapter requestAdapter)
        : base(requestAdapter, "{+baseurl}/test", new Dictionary<string, object>())
    {
        Api = new ApiRequestBuilder(requestAdapter);
    }
}

/// <summary>
/// Api request builder with Items and Status endpoints
/// </summary>
public class ApiRequestBuilder : BaseRequestBuilder
{
    public ItemsRequestBuilder Items { get; }
    public StatusRequestBuilder Status { get; }

    public ApiRequestBuilder(IRequestAdapter requestAdapter)
        : base(requestAdapter, "{+baseurl}/api", new Dictionary<string, object>())
    {
        Items = new ItemsRequestBuilder(requestAdapter);
        Status = new StatusRequestBuilder(requestAdapter);
    }
}

/// <summary>
/// Items collection request builder (supports indexer for item ID)
/// </summary>
public class ItemsRequestBuilder : BaseRequestBuilder
{
    // Store the adapter to access it in the indexer
    private readonly IRequestAdapter _requestAdapter;

    public ItemsRequestBuilder(IRequestAdapter requestAdapter)
        : base(requestAdapter, "{+baseurl}/api/items", new Dictionary<string, object>())
    {
        _requestAdapter = requestAdapter;
    }

    /// <summary>
    /// Indexer to get a specific item request builder by ID
    /// </summary>
    public ItemRequestBuilder this[string id] => new ItemRequestBuilder(_requestAdapter, id);
}

/// <summary>
/// Single item request builder (for operations on a specific item)
/// </summary>
public class ItemRequestBuilder : BaseRequestBuilder
{
    public ItemRequestBuilder(IRequestAdapter requestAdapter, string id)
        : base(
            requestAdapter,
            "{+baseurl}/api/items/{id}",
            new Dictionary<string, object> { { "id", id } }
        ) { }
}

/// <summary>
/// Status endpoint request builder
/// </summary>
public class StatusRequestBuilder : BaseRequestBuilder
{
    public StatusRequestBuilder(IRequestAdapter requestAdapter)
        : base(requestAdapter, "{+baseurl}/api/status", new Dictionary<string, object>()) { }
}

/// <summary>
/// Test response object (IParsable)
/// </summary>
public class TestResponse : IParsable
{
    public string Value { get; set; } = string.Empty;

    public static TestResponse CreateFromDiscriminatorValue(IParseNode parseNode)
    {
        return new TestResponse();
    }

    public IDictionary<string, Action<IParseNode>> GetFieldDeserializers()
    {
        return new Dictionary<string, Action<IParseNode>>
        {
            { "value", n => Value = n.GetStringValue() ?? string.Empty },
        };
    }

    public void Serialize(ISerializationWriter writer)
    {
        writer.WriteStringValue("value", Value);
    }
}

#endregion
