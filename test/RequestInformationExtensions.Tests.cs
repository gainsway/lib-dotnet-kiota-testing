using System.Linq.Expressions;
using Microsoft.Kiota.Abstractions;

namespace Gainsway.Kiota.Testing.Tests;

[TestFixture]
public class RequestInformationExtensionsTests
{
    [Test]
    public void And_WithTwoPredicates_ShouldCombineCorrectly()
    {
        // Arrange
        Expression<Predicate<RequestInformation>> predicate1 = req =>
            req.PathParameters.ContainsKey("id");
        Expression<Predicate<RequestInformation>> predicate2 = req => req.HttpMethod == Method.GET;

        // Act
        var combined = predicate1.And(predicate2);

        // Assert
        Assert.That(combined, Is.Not.Null);
        Assert.That(combined.Parameters, Has.Count.EqualTo(1));
        Assert.That(combined.Parameters[0].Type, Is.EqualTo(typeof(RequestInformation)));
    }

    [Test]
    public void And_WithBothPredicatesTrue_ShouldReturnTrue()
    {
        // Arrange
        var requestInfo = new RequestInformation
        {
            HttpMethod = Method.GET,
            UrlTemplate = "/api/test",
        };
        requestInfo.PathParameters.Add("id", "123");

        Expression<Predicate<RequestInformation>> predicate1 = req =>
            req.PathParameters.ContainsKey("id");
        Expression<Predicate<RequestInformation>> predicate2 = req => req.HttpMethod == Method.GET;

        // Act
        var combined = predicate1.And(predicate2);
        var compiledPredicate = combined.Compile();
        var result = compiledPredicate(requestInfo);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void And_WithFirstPredicateFalse_ShouldReturnFalse()
    {
        // Arrange
        var requestInfo = new RequestInformation
        {
            HttpMethod = Method.GET,
            UrlTemplate = "/api/test",
        };
        // Not adding "id" to PathParameters

        Expression<Predicate<RequestInformation>> predicate1 = req =>
            req.PathParameters.ContainsKey("id");
        Expression<Predicate<RequestInformation>> predicate2 = req => req.HttpMethod == Method.GET;

        // Act
        var combined = predicate1.And(predicate2);
        var compiledPredicate = combined.Compile();
        var result = compiledPredicate(requestInfo);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void And_WithSecondPredicateFalse_ShouldReturnFalse()
    {
        // Arrange
        var requestInfo = new RequestInformation
        {
            HttpMethod = Method.POST, // Different method
            UrlTemplate = "/api/test",
        };
        requestInfo.PathParameters.Add("id", "123");

        Expression<Predicate<RequestInformation>> predicate1 = req =>
            req.PathParameters.ContainsKey("id");
        Expression<Predicate<RequestInformation>> predicate2 = req => req.HttpMethod == Method.GET;

        // Act
        var combined = predicate1.And(predicate2);
        var compiledPredicate = combined.Compile();
        var result = compiledPredicate(requestInfo);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void And_WithBothPredicatesFalse_ShouldReturnFalse()
    {
        // Arrange
        var requestInfo = new RequestInformation
        {
            HttpMethod = Method.POST,
            UrlTemplate = "/api/test",
        };
        // Not adding "id" to PathParameters

        Expression<Predicate<RequestInformation>> predicate1 = req =>
            req.PathParameters.ContainsKey("id");
        Expression<Predicate<RequestInformation>> predicate2 = req => req.HttpMethod == Method.GET;

        // Act
        var combined = predicate1.And(predicate2);
        var compiledPredicate = combined.Compile();
        var result = compiledPredicate(requestInfo);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void And_WithComplexPredicates_ShouldCombineCorrectly()
    {
        // Arrange
        var fundId = Guid.NewGuid();
        var requestInfo = new RequestInformation
        {
            HttpMethod = Method.PUT,
            UrlTemplate = "/api/funds/{id}",
        };
        requestInfo.PathParameters.Add("id", fundId);

        Expression<Predicate<RequestInformation>> predicate1 = req =>
            req.PathParameters["id"].ToString() == fundId.ToString();
        Expression<Predicate<RequestInformation>> predicate2 = req => req.HttpMethod == Method.PUT;

        // Act
        var combined = predicate1.And(predicate2);
        var compiledPredicate = combined.Compile();
        var result = compiledPredicate(requestInfo);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void And_WithMultiplePathParameters_ShouldCombineCorrectly()
    {
        // Arrange
        var fundId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var requestInfo = new RequestInformation
        {
            HttpMethod = Method.POST,
            UrlTemplate = "/api/funds/{fundId}/activities/{activityId}/modify",
        };
        requestInfo.PathParameters.Add("fundId", fundId);
        requestInfo.PathParameters.Add("activityId", activityId);

        Expression<Predicate<RequestInformation>> predicate1 = req =>
            req.PathParameters["fundId"].ToString() == fundId.ToString();
        Expression<Predicate<RequestInformation>> predicate2 = req =>
            req.PathParameters["activityId"].ToString() == activityId.ToString();

        // Act
        var combined = predicate1.And(predicate2);
        var compiledPredicate = combined.Compile();
        var result = compiledPredicate(requestInfo);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void And_WithUrlTemplateMatching_ShouldCombineCorrectly()
    {
        // Arrange
        var requestInfo = new RequestInformation
        {
            HttpMethod = Method.GET,
            UrlTemplate = "/api/funds/{id}/activities",
        };
        requestInfo.PathParameters.Add("id", "123");

        Expression<Predicate<RequestInformation>> predicate1 = req =>
            req.UrlTemplate!.Contains("/activities");
        Expression<Predicate<RequestInformation>> predicate2 = req =>
            req.PathParameters["id"].ToString() == "123";

        // Act
        var combined = predicate1.And(predicate2);
        var compiledPredicate = combined.Compile();
        var result = compiledPredicate(requestInfo);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void And_ChainedMultipleTimes_ShouldWorkCorrectly()
    {
        // Arrange
        var requestInfo = new RequestInformation
        {
            HttpMethod = Method.GET,
            UrlTemplate = "/api/test",
        };
        requestInfo.PathParameters.Add("id", "123");

        Expression<Predicate<RequestInformation>> predicate1 = req =>
            req.PathParameters.ContainsKey("id");
        Expression<Predicate<RequestInformation>> predicate2 = req => req.HttpMethod == Method.GET;
        Expression<Predicate<RequestInformation>> predicate3 = req =>
            req.UrlTemplate!.Contains("/api/");

        // Act
        var combined = predicate1.And(predicate2).And(predicate3);
        var compiledPredicate = combined.Compile();
        var result = compiledPredicate(requestInfo);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void And_WithNullableValues_ShouldHandleCorrectly()
    {
        // Arrange
        var requestInfo = new RequestInformation
        {
            HttpMethod = Method.GET,
            UrlTemplate = "/api/test",
        };
        requestInfo.PathParameters.Add("optionalParam", null!);

        Expression<Predicate<RequestInformation>> predicate1 = req =>
            req.PathParameters.ContainsKey("optionalParam");
        Expression<Predicate<RequestInformation>> predicate2 = req => req.HttpMethod == Method.GET;

        // Act
        var combined = predicate1.And(predicate2);
        var compiledPredicate = combined.Compile();
        var result = compiledPredicate(requestInfo);

        // Assert
        Assert.That(result, Is.True);
    }
}
