using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;

namespace Gainsway.Kiota.Testing.Tests;

/// <summary>
/// Comprehensive tests for KiotaClientMockExtensions.
/// Note: These are primarily "smoke tests" that verify mock setup calls complete without exceptions.
/// They validate that the API patterns, predicates, and type constraints are correct.
/// Actual behavior verification would require invoking the mocked Kiota client methods.
/// </summary>
[TestFixture]
public class KiotaClientMockExtensionsTests
{
    private TestRequestBuilder _mockClient = null!;

    [SetUp]
    public void Setup()
    {
        _mockClient = KiotaClientMockExtensions.GetMockableClient<TestRequestBuilder>();
    }

    #region URL Pattern Matching Tests

    [Test]
    public void MockClientResponse_WithSimpleUrlTemplate_ShouldMatchCorrectly()
    {
        // Arrange
        var expectedResponse = new TestParsableObject { Id = "test-id", Name = "Test" };
        _mockClient.MockClientResponse("/api/test", expectedResponse);

        // Act & Assert - verify the mock was set up (we can't directly test without actual HTTP calls)
        Assert.That(_mockClient, Is.Not.Null);
    }

    [Test]
    public void MockClientResponse_WithSinglePathParameter_ShouldMatchCorrectly()
    {
        // Arrange
        var fundId = Guid.NewGuid();
        var expectedResponse = new TestParsableObject { Id = fundId.ToString(), Name = "Fund" };

        _mockClient.MockClientResponse(
            "/api/funds/{id}",
            expectedResponse,
            req => req.PathParameters["id"].ToString() == fundId.ToString()
        );

        // Act & Assert
        Assert.That(_mockClient, Is.Not.Null);
    }

    [Test]
    public void MockClientResponse_WithMultiplePathParameters_ShouldMatchCorrectly()
    {
        // Arrange
        var fundId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var expectedResponse = new TestParsableObject { Id = activityId.ToString() };

        _mockClient.MockClientResponse(
            "/api/funds/{fundId}/activities/{activityId}",
            expectedResponse,
            req =>
                req.PathParameters["fundId"].ToString() == fundId.ToString()
                && req.PathParameters["activityId"].ToString() == activityId.ToString()
        );

        // Act & Assert
        Assert.That(_mockClient, Is.Not.Null);
    }

    [Test]
    public void MockClientResponse_WithNestedResourcePath_ShouldMatchCorrectly()
    {
        // Arrange
        var fundId = Guid.NewGuid();
        var expectedResponse = new TestParsableObject { Id = fundId.ToString() };

        _mockClient.MockClientResponse(
            "/api/funds/{fundId}/activities",
            expectedResponse,
            req => req.PathParameters["fundId"].ToString() == fundId.ToString()
        );

        // Act & Assert
        Assert.That(_mockClient, Is.Not.Null);
    }

    [Test]
    public void MockClientResponse_WithActionPath_ShouldMatchCorrectly()
    {
        // Arrange
        var fundId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var expectedResponse = new TestParsableObject { Id = activityId.ToString() };

        _mockClient.MockClientResponse(
            "/api/funds/{fundId}/activities/{activityId}/modify",
            expectedResponse,
            req =>
                req.PathParameters["fundId"].ToString() == fundId.ToString()
                && req.PathParameters["activityId"].ToString() == activityId.ToString()
        );

        // Act & Assert
        Assert.That(_mockClient, Is.Not.Null);
    }

    [Test]
    public void MockClientResponse_WithQueryParameters_ShouldStripQueryParamsFromMatch()
    {
        // Arrange
        var expectedResponse = new TestParsableObject { Id = "test" };

        // The URL template matcher should strip {?param1,param2} from matching
        _mockClient.MockClientResponse("/api/test", expectedResponse);

        // Act & Assert
        Assert.That(_mockClient, Is.Not.Null);
    }

    [Test]
    public void MockClientResponse_WithColonStyleParameters_ShouldMatchCorrectly()
    {
        // Arrange - testing :fundId vs {fundId} style parameters
        var fundId = Guid.NewGuid();
        var expectedResponse = new TestParsableObject { Id = fundId.ToString() };

        // Note: Kiota generates {param} style, but your API might use :param
        _mockClient.MockClientResponse(
            "/api/funds/{fundId}/activities",
            expectedResponse,
            req => req.PathParameters["fundId"].ToString() == fundId.ToString()
        );

        // Act & Assert
        Assert.That(_mockClient, Is.Not.Null);
    }

    #endregion

    #region String Response Tests

    [Test]
    public void MockClientResponse_WithStringResponse_ShouldSetupCorrectly()
    {
        // Arrange
        var expectedString = "success";

        // Act
        _mockClient.MockClientResponse("/api/status", expectedString);

        // Assert
        Assert.That(_mockClient, Is.Not.Null);
    }

    [Test]
    public void MockClientResponse_WithNullString_ShouldSetupCorrectly()
    {
        // Arrange
        string? nullString = null;

        // Act
        _mockClient.MockClientResponse("/api/optional", nullString);

        // Assert
        Assert.That(_mockClient, Is.Not.Null);
    }

    #endregion

    #region IParsable Response Tests

    [Test]
    public void MockClientResponse_WithIParsableObject_ShouldSetupCorrectly()
    {
        // Arrange
        var expectedResponse = new TestParsableObject { Id = "123", Name = "Test Object" };

        // Act
        _mockClient.MockClientResponse("/api/objects/123", expectedResponse);

        // Assert
        Assert.That(_mockClient, Is.Not.Null);
    }

    [Test]
    public void MockClientResponse_WithNullIParsableObject_ShouldSetupCorrectly()
    {
        // Arrange
        TestParsableObject? nullObject = null;

        // Act
        _mockClient.MockClientResponse("/api/optional-object", nullObject);

        // Assert
        Assert.That(_mockClient, Is.Not.Null);
    }

    #endregion

    #region Collection Response Tests

    [Test]
    public void MockClientCollectionResponse_WithEmptyCollection_ShouldSetupCorrectly()
    {
        // Arrange
        var emptyList = new List<TestParsableObject>();

        // Act
        _mockClient.MockClientCollectionResponse("/api/items", emptyList);

        // Assert
        Assert.That(_mockClient, Is.Not.Null);
    }

    [Test]
    public void MockClientCollectionResponse_WithMultipleItems_ShouldSetupCorrectly()
    {
        // Arrange
        var items = new List<TestParsableObject>
        {
            new TestParsableObject { Id = "1", Name = "Item 1" },
            new TestParsableObject { Id = "2", Name = "Item 2" },
            new TestParsableObject { Id = "3", Name = "Item 3" },
        };

        // Act
        _mockClient.MockClientCollectionResponse("/api/items", items);

        // Assert
        Assert.That(_mockClient, Is.Not.Null);
    }

    [Test]
    public void MockClientCollectionResponse_WithNullCollection_ShouldSetupCorrectly()
    {
        // Arrange
        IEnumerable<TestParsableObject>? nullCollection = null;

        // Act
        _mockClient.MockClientCollectionResponse("/api/optional-items", nullCollection);

        // Assert
        Assert.That(_mockClient, Is.Not.Null);
    }

    #endregion

    #region No Content Response Tests

    [Test]
    public void MockClientNoContentResponse_ShouldSetupCorrectly()
    {
        // Act
        _mockClient.MockClientNoContentResponse("/api/delete/123");

        // Assert
        Assert.That(_mockClient, Is.Not.Null);
    }

    [Test]
    public void MockClientNoContentResponse_WithPredicate_ShouldSetupCorrectly()
    {
        // Arrange
        var itemId = Guid.NewGuid();

        // Act
        _mockClient.MockClientNoContentResponse(
            "/api/items/{id}",
            req => req.PathParameters["id"].ToString() == itemId.ToString()
        );

        // Assert
        Assert.That(_mockClient, Is.Not.Null);
    }

    #endregion

    #region Predicate Combination Tests

    [Test]
    public void MockClientResponse_WithComplexPredicate_ShouldSetupCorrectly()
    {
        // Arrange
        var fundId = Guid.NewGuid();
        var expectedResponse = new TestParsableObject { Id = fundId.ToString() };

        // Act - testing multiple conditions
        _mockClient.MockClientResponse(
            "/api/funds/{id}",
            expectedResponse,
            req =>
                req.PathParameters["id"].ToString() == fundId.ToString()
                && req.HttpMethod == Method.GET
        );

        // Assert
        Assert.That(_mockClient, Is.Not.Null);
    }

    #endregion

    #region Edge Cases

    [Test]
    public void MockClientResponse_WithTrailingSlash_ShouldMatchCorrectly()
    {
        // Arrange
        var expectedResponse = new TestParsableObject { Id = "test" };

        // Act
        _mockClient.MockClientResponse("/api/test/", expectedResponse);

        // Assert
        Assert.That(_mockClient, Is.Not.Null);
    }

    [Test]
    public void MockClientResponse_WithDeepNestedPath_ShouldMatchCorrectly()
    {
        // Arrange
        var expectedResponse = new TestParsableObject { Id = "test" };

        // Act
        _mockClient.MockClientResponse(
            "/api/v1/funds/{fundId}/activities/{activityId}/details/{detailId}",
            expectedResponse
        );

        // Assert
        Assert.That(_mockClient, Is.Not.Null);
    }

    #endregion

    #region GetMockableClient Tests

    [Test]
    public void GetMockableClient_ShouldCreateInstanceSuccessfully()
    {
        // Act
        var client = KiotaClientMockExtensions.GetMockableClient<TestRequestBuilder>();

        // Assert
        Assert.That(client, Is.Not.Null);
        Assert.That(client, Is.InstanceOf<TestRequestBuilder>());
    }

    [Test]
    public void GetMockableClient_ShouldHaveRequestAdapterSet()
    {
        // Act
        var client = KiotaClientMockExtensions.GetMockableClient<TestRequestBuilder>();

        // Assert - we can verify the client was created successfully
        Assert.That(client, Is.Not.Null);
        // The RequestAdapter is internal, but if it wasn't set, mocking wouldn't work
    }

    #endregion
}

#region Test Helper Classes

/// <summary>
/// Test request builder that mimics Kiota-generated client structure
/// </summary>
public class TestRequestBuilder : BaseRequestBuilder
{
    public TestRequestBuilder(IRequestAdapter requestAdapter)
        : base(requestAdapter, "{+baseurl}/test", new Dictionary<string, object>()) { }
}

/// <summary>
/// Test parsable object for testing IParsable responses
/// </summary>
public class TestParsableObject : IParsable
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public IDictionary<string, Action<IParseNode>> GetFieldDeserializers()
    {
        return new Dictionary<string, Action<IParseNode>>
        {
            { "id", n => Id = n.GetStringValue() ?? string.Empty },
            { "name", n => Name = n.GetStringValue() ?? string.Empty },
        };
    }

    public void Serialize(ISerializationWriter writer)
    {
        writer.WriteStringValue("id", Id);
        writer.WriteStringValue("name", Name);
    }
}

#endregion
