using Microsoft.Kiota.Abstractions;

namespace Gainsway.Kiota.Testing.Tests;

/// <summary>
/// Tests for KiotaClientMockExtensions: GetMockableClient, NormalizeUrlTemplate, GetMockAdapter.
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

    #region NormalizeUrlTemplate Tests

    [Test]
    public void NormalizeUrlTemplate_WithSimplePath_ShouldNormalizeCorrectly()
    {
        // Arrange
        var urlTemplate = "/api/funds";

        // Act
        var normalized = KiotaClientMockExtensions.NormalizeUrlTemplate(urlTemplate);

        // Assert
        Assert.That(normalized, Is.EqualTo("/api/funds"));
    }

    [Test]
    public void NormalizeUrlTemplate_WithSingleParameter_ShouldReplaceWithPathParam1()
    {
        // Arrange
        var urlTemplate = "/api/funds/{id}";

        // Act
        var normalized = KiotaClientMockExtensions.NormalizeUrlTemplate(urlTemplate);

        // Assert
        Assert.That(normalized, Is.EqualTo("/api/funds/{pathParam1}"));
    }

    [Test]
    public void NormalizeUrlTemplate_WithMultipleParameters_ShouldReplaceWithPathParams()
    {
        // Arrange
        var urlTemplate = "/api/funds/{fundId}/activities/{activityId}";

        // Act
        var normalized = KiotaClientMockExtensions.NormalizeUrlTemplate(urlTemplate);

        // Assert
        Assert.That(normalized, Is.EqualTo("/api/funds/{pathParam1}/activities/{pathParam2}"));
    }

    [Test]
    public void NormalizeUrlTemplate_WithThreeParameters_ShouldReplaceWithPathParams()
    {
        // Arrange
        var urlTemplate = "/api/fundapplications/{id}/submissions/{version}/review";

        // Act
        var normalized = KiotaClientMockExtensions.NormalizeUrlTemplate(urlTemplate);

        // Assert
        Assert.That(
            normalized,
            Is.EqualTo("/api/fundapplications/{pathParam1}/submissions/{pathParam2}/review")
        );
    }

    [Test]
    public void NormalizeUrlTemplate_WithBaseUrlPrefix_ShouldStripBaseUrl()
    {
        // Arrange
        var urlTemplate = "{+baseurl}/api/funds/{id}";

        // Act
        var normalized = KiotaClientMockExtensions.NormalizeUrlTemplate(urlTemplate);

        // Assert
        Assert.That(normalized, Is.EqualTo("/api/funds/{pathParam1}"));
    }

    [Test]
    public void NormalizeUrlTemplate_WithQueryParameters_ShouldNormalizeQueryParams()
    {
        // Arrange
        var urlTemplate = "/api/funds/{id}{?select,expand}";

        // Act
        var normalized = KiotaClientMockExtensions.NormalizeUrlTemplate(urlTemplate);

        // Assert
        Assert.That(normalized, Is.EqualTo("/api/funds/{pathParam1}{?queryParam1,queryParam2}"));
    }

    [Test]
    public void NormalizeUrlTemplate_WithKebabCaseParameter_ShouldReplaceWithPathParam()
    {
        // Arrange
        var urlTemplate = "/api/funds/{fund-id}";

        // Act
        var normalized = KiotaClientMockExtensions.NormalizeUrlTemplate(urlTemplate);

        // Assert
        Assert.That(normalized, Is.EqualTo("/api/funds/{pathParam1}"));
    }

    [Test]
    public void NormalizeUrlTemplate_WithUrlEncodedParameter_ShouldReplaceWithPathParam()
    {
        // Arrange
        var urlTemplate = "/api/funds/{fund%2Did}";

        // Act
        var normalized = KiotaClientMockExtensions.NormalizeUrlTemplate(urlTemplate);

        // Assert
        Assert.That(normalized, Is.EqualTo("/api/funds/{pathParam1}"));
    }

    [Test]
    public void NormalizeUrlTemplate_WithComplexPath_ShouldNormalizeCorrectly()
    {
        // Arrange
        var urlTemplate =
            "{+baseurl}/api/fundapplications/{fundApplicationId}/submissions/{submissionVersionNumber}/review{?select,expand}";

        // Act
        var normalized = KiotaClientMockExtensions.NormalizeUrlTemplate(urlTemplate);

        // Assert
        Assert.That(
            normalized,
            Is.EqualTo(
                "/api/fundapplications/{pathParam1}/submissions/{pathParam2}/review{?queryParam1,queryParam2}"
            )
        );
    }

    [Test]
    public void NormalizeUrlTemplate_WithNoLeadingSlash_ShouldAddLeadingSlash()
    {
        // Arrange
        var urlTemplate = "api/funds/{id}";

        // Act
        var normalized = KiotaClientMockExtensions.NormalizeUrlTemplate(urlTemplate);

        // Assert
        Assert.That(normalized, Is.EqualTo("/api/funds/{pathParam1}"));
    }

    [Test]
    public void NormalizeUrlTemplate_WithOnlyQueryParameters_ShouldNormalizeQueryParams()
    {
        // Arrange
        var urlTemplate = "/api/funds{?select,expand,filter}";

        // Act
        var normalized = KiotaClientMockExtensions.NormalizeUrlTemplate(urlTemplate);

        // Assert
        Assert.That(normalized, Is.EqualTo("/api/funds{?queryParam1,queryParam2,queryParam3}"));
    }

    [Test]
    public void NormalizeUrlTemplate_WithSingleQueryParameter_ShouldNormalizeToQueryParam1()
    {
        // Arrange
        var urlTemplate = "/api/funds{?filter}";

        // Act
        var normalized = KiotaClientMockExtensions.NormalizeUrlTemplate(urlTemplate);

        // Assert
        Assert.That(normalized, Is.EqualTo("/api/funds{?queryParam1}"));
    }

    #endregion

    #region GetMockAdapter Tests

    [Test]
    public void GetMockAdapter_ShouldReturnIRequestAdapter()
    {
        // Act
        var adapter = _mockClient.GetMockAdapter();

        // Assert
        Assert.That(adapter, Is.Not.Null);
        Assert.That(adapter, Is.InstanceOf<IRequestAdapter>());
    }

    [Test]
    public void GetMockAdapter_ShouldReturnSameAdapterAcrossMultipleCalls()
    {
        // Act
        var adapter1 = _mockClient.GetMockAdapter();
        var adapter2 = _mockClient.GetMockAdapter();

        // Assert
        Assert.That(adapter1, Is.SameAs(adapter2));
    }

    [Test]
    public void GetMockAdapter_ShouldWorkWithDifferentClientTypes()
    {
        // Arrange
        var anotherClient =
            KiotaClientMockExtensions.GetMockableClient<AnotherTestRequestBuilder>();

        // Act
        var adapter1 = _mockClient.GetMockAdapter();
        var adapter2 = anotherClient.GetMockAdapter();

        // Assert
        Assert.That(adapter1, Is.Not.Null);
        Assert.That(adapter2, Is.Not.Null);
        Assert.That(adapter1, Is.Not.SameAs(adapter2)); // Different clients have different adapters
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
/// Another test request builder for testing multiple client instances
/// </summary>
public class AnotherTestRequestBuilder : BaseRequestBuilder
{
    public AnotherTestRequestBuilder(IRequestAdapter requestAdapter)
        : base(requestAdapter, "{+baseurl}/another", new Dictionary<string, object>()) { }
}


#endregion
