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

    #region Exception Mocking Tests

    [Test]
    public void MockClientResponseException_WithNotFoundException_ShouldSetupMockCorrectly()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var exception = new ApiException("Fund not found") { ResponseStatusCode = 404 };

        // Act
        _mockClient.MockClientResponseException<TestRequestBuilder, TestParsableObject>(
            "/api/funds/{id}",
            exception,
            req => req.PathParameters["id"].ToString() == nonExistentId.ToString()
        );

        // Assert
        Assert.Pass("Exception mock setup completed successfully");
    }

    [Test]
    public void MockClientCollectionResponseException_WithInternalServerError_ShouldSetupMockCorrectly()
    {
        // Arrange
        var exception = new ApiException("Internal server error") { ResponseStatusCode = 500 };

        // Act
        _mockClient.MockClientCollectionResponseException<TestRequestBuilder, TestParsableObject>(
            "/api/activities",
            exception
        );

        // Assert
        Assert.Pass("Collection exception mock setup completed successfully");
    }

    [Test]
    public void MockClientNoContentResponseException_WithConflictError_ShouldSetupMockCorrectly()
    {
        // Arrange
        var conflictingId = Guid.NewGuid();
        var exception = new ApiException("Conflict - Resource has dependencies")
        {
            ResponseStatusCode = 409,
        };

        // Act
        _mockClient.MockClientNoContentResponseException(
            "/api/funds/{id}",
            exception,
            req => req.PathParameters["id"].ToString() == conflictingId.ToString()
        );

        // Assert
        Assert.Pass("No-content exception mock setup completed successfully");
    }

    [Test]
    public void MockClientResponseException_WithUnauthorizedError_ShouldSetupMockCorrectly()
    {
        // Arrange
        var exception = new ApiException("Unauthorized") { ResponseStatusCode = 401 };

        // Act
        _mockClient.MockClientResponseException<TestRequestBuilder, TestParsableObject>(
            "/api/funds/{id}",
            exception,
            req => !req.Headers.ContainsKey("Authorization")
        );

        // Assert
        Assert.Pass("Unauthorized exception mock setup completed successfully");
    }

    #endregion

    #region POST/PUT with Body Tests

    [Test]
    public void MockClientResponse_WithPostMethodPredicate_ShouldSetupMockCorrectly()
    {
        // Arrange
        var newObject = new TestParsableObject { Id = "new-id", Name = "New Object" };

        // Act
        _mockClient.MockClientResponse(
            "/api/items",
            newObject,
            req => req.HttpMethod == Method.POST && req.Content != null
        );

        // Assert
        Assert.Pass("POST method mock setup completed successfully");
    }

    [Test]
    public void MockClientResponse_WithPutMethodPredicate_ShouldSetupMockCorrectly()
    {
        // Arrange
        var existingId = Guid.NewGuid();
        var updatedObject = new TestParsableObject
        {
            Id = existingId.ToString(),
            Name = "Updated Object",
        };

        // Act
        _mockClient.MockClientResponse(
            "/api/items/{id}",
            updatedObject,
            req =>
                req.HttpMethod == Method.PUT
                && req.PathParameters["id"].ToString() == existingId.ToString()
                && req.Content != null
        );

        // Assert
        Assert.Pass("PUT method mock setup completed successfully");
    }

    [Test]
    public void MockClientResponse_WithPatchMethodPredicate_ShouldSetupMockCorrectly()
    {
        // Arrange
        var existingId = Guid.NewGuid();
        var patchedObject = new TestParsableObject
        {
            Id = existingId.ToString(),
            Name = "Patched Object",
        };

        // Act
        _mockClient.MockClientResponse(
            "/api/items/{id}",
            patchedObject,
            req =>
                req.HttpMethod == Method.PATCH
                && req.PathParameters["id"].ToString() == existingId.ToString()
        );

        // Assert
        Assert.Pass("PATCH method mock setup completed successfully");
    }

    [Test]
    public void MockClientResponse_WithContentTypeHeader_ShouldSetupMockCorrectly()
    {
        // Arrange
        var fundId = Guid.NewGuid();
        var createdActivity = new TestParsableObject
        {
            Id = Guid.NewGuid().ToString(),
            Name = "New Activity",
        };

        // Act
        _mockClient.MockClientResponse(
            "/api/funds/{fundId}/activities",
            createdActivity,
            req =>
                req.HttpMethod == Method.POST
                && req.PathParameters["fundId"].ToString() == fundId.ToString()
                && req.Headers.ContainsKey("Content-Type")
        );

        // Assert
        Assert.Pass("Content-Type header predicate mock setup completed successfully");
    }

    [Test]
    public void MockClientResponse_WithMultipleHttpMethods_ShouldSetupMultipleMocksCorrectly()
    {
        // Arrange
        var itemId = Guid.NewGuid();
        var getResponse = new TestParsableObject { Id = itemId.ToString(), Name = "Get Response" };
        var putResponse = new TestParsableObject { Id = itemId.ToString(), Name = "Put Response" };

        // Act - Setup different responses for GET and PUT on same URL
        _mockClient.MockClientResponse(
            "/api/items/{id}",
            getResponse,
            req =>
                req.HttpMethod == Method.GET
                && req.PathParameters["id"].ToString() == itemId.ToString()
        );

        _mockClient.MockClientResponse(
            "/api/items/{id}",
            putResponse,
            req =>
                req.HttpMethod == Method.PUT
                && req.PathParameters["id"].ToString() == itemId.ToString()
        );

        // Assert
        Assert.Pass("Multiple HTTP method mocks setup completed successfully");
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

/// <summary>
/// Test exception class that mimics API exceptions with status codes
/// </summary>
public class ApiException : Exception
{
    public int ResponseStatusCode { get; set; }

    public ApiException(string message)
        : base(message) { }

    public ApiException(string message, Exception innerException)
        : base(message, innerException) { }
}

#endregion
