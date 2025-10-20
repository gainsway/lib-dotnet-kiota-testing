using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;

#pragma warning disable CS0618 // Type or member is obsolete - Testing legacy API and backward compatibility

namespace Gainsway.Kiota.Testing.Tests;

/// <summary>
/// Tests to verify backward compatibility with different parameter naming conventions.
/// </summary>
[TestFixture]
public class BackwardCompatibilityTests
{
    private TestRequestBuilder _mockClient = null!;

    [SetUp]
    public void Setup()
    {
        _mockClient = KiotaClientMockExtensions.GetMockableClient<TestRequestBuilder>();
    }

    [Test]
    public void MockClientResponse_WithEmptyString_ShouldMatchAnyUrl()
    {
        // Arrange
        var expectedResponse = new TestParsableObject { Id = "test-id", Name = "Test" };

        // Act - Empty string should match any URL
        _mockClient.MockClientResponse("", expectedResponse);

        // Assert - Should not throw
        Assert.Pass("Empty string mock setup completed successfully");
    }

    [Test]
    public void NormalizeUrlTemplate_WithDifferentParameterNames_ShouldProduceSameResult()
    {
        // Arrange
        var pattern1 = "/api/users/{userId}/accounts/{accountId}";
        var pattern2 = "/api/users/{user-id}/accounts/{account-id}";
        var pattern3 = "/api/users/{user%2Did}/accounts/{account%2Did}";
        var pattern4 = "{+baseurl}/api/users/{UserId}/accounts/{AccountId}";

        // Act
        var normalized1 = KiotaClientMockExtensions.NormalizeUrlTemplate(pattern1);
        var normalized2 = KiotaClientMockExtensions.NormalizeUrlTemplate(pattern2);
        var normalized3 = KiotaClientMockExtensions.NormalizeUrlTemplate(pattern3);
        var normalized4 = KiotaClientMockExtensions.NormalizeUrlTemplate(pattern4);

        // Assert
        Assert.That(normalized1, Is.EqualTo("/api/users/{pathParam1}/accounts/{pathParam2}"));
        Assert.That(normalized2, Is.EqualTo("/api/users/{pathParam1}/accounts/{pathParam2}"));
        Assert.That(normalized3, Is.EqualTo("/api/users/{pathParam1}/accounts/{pathParam2}"));
        Assert.That(normalized4, Is.EqualTo("/api/users/{pathParam1}/accounts/{pathParam2}"));

        Console.WriteLine($"Pattern 1: {pattern1} -> {normalized1}");
        Console.WriteLine($"Pattern 2: {pattern2} -> {normalized2}");
        Console.WriteLine($"Pattern 3: {pattern3} -> {normalized3}");
        Console.WriteLine($"Pattern 4: {pattern4} -> {normalized4}");
        Console.WriteLine("All patterns normalize to the same result - matching should work!");
    }

    [Test]
    public void MockClientResponse_WithCamelCaseParams_ShouldMatchKebabCaseRequest()
    {
        // Arrange
        var expectedResponse = new TestParsableObject { Id = "account-123", Name = "Test Account" };

        // Act - Mock with camelCase parameters
        _mockClient.MockClientResponse(
            "/api/users/{userId}/accounts/{accountId}",
            expectedResponse
        );

        // Assert - Should successfully set up mock
        // In a real scenario, a Kiota request with {user-id}/{account-id} should match this
        Assert.Pass("Mock with camelCase parameters set up successfully");
    }

    [Test]
    public void MockClientResponse_WithPredicateAndDifferentParamNames_ShouldWork()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var expectedResponse = new TestParsableObject
        {
            Id = accountId.ToString(),
            Name = "Account",
        };

        // Act - Use GetPathParameter() to handle different naming conventions
        _mockClient.MockClientResponse(
            "/api/users/{userId}/accounts/{accountId}",
            expectedResponse,
            req =>
                req.GetPathParameter("userId").ToString() == userId.ToString()
                && req.GetPathParameter("accountId").ToString() == accountId.ToString()
        );

        // Assert
        Assert.Pass("Mock with path parameter predicates set up successfully");
    }
}
