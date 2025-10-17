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

    #region NormalizeUrlTemplate Extension Tests

    [Test]
    public void NormalizeUrlTemplate_WithNullRequestInfo_ShouldReturnEmptyString()
    {
        // Arrange
        RequestInformation? requestInfo = null;

        // Act
        var normalized = RequestInformationExtensions.NormalizeUrlTemplate(requestInfo);

        // Assert
        Assert.That(normalized, Is.EqualTo(string.Empty));
    }

    [Test]
    public void NormalizeUrlTemplate_WithNullUrlTemplate_ShouldReturnEmptyString()
    {
        // Arrange
        var requestInfo = new RequestInformation { UrlTemplate = null };

        // Act
        var normalized = requestInfo.NormalizeUrlTemplate();

        // Assert
        Assert.That(normalized, Is.EqualTo(string.Empty));
    }

    [Test]
    public void NormalizeUrlTemplate_WithEmptyUrlTemplate_ShouldReturnEmptyString()
    {
        // Arrange
        var requestInfo = new RequestInformation { UrlTemplate = string.Empty };

        // Act
        var normalized = requestInfo.NormalizeUrlTemplate();

        // Assert
        Assert.That(normalized, Is.EqualTo(string.Empty));
    }

    [Test]
    public void NormalizeUrlTemplate_WithSimplePath_ShouldNormalize()
    {
        // Arrange
        var requestInfo = new RequestInformation { UrlTemplate = "/api/funds" };

        // Act
        var normalized = requestInfo.NormalizeUrlTemplate();

        // Assert
        Assert.That(normalized, Is.EqualTo("/api/funds"));
    }

    [Test]
    public void NormalizeUrlTemplate_WithSingleParameter_ShouldReplaceWithPathParam1()
    {
        // Arrange
        var requestInfo = new RequestInformation { UrlTemplate = "/api/funds/{id}" };

        // Act
        var normalized = requestInfo.NormalizeUrlTemplate();

        // Assert
        Assert.That(normalized, Is.EqualTo("/api/funds/{pathParam1}"));
    }

    [Test]
    public void NormalizeUrlTemplate_WithMultipleParameters_ShouldReplaceWithPathParams()
    {
        // Arrange
        var requestInfo = new RequestInformation
        {
            UrlTemplate = "/api/funds/{fundId}/activities/{activityId}",
        };

        // Act
        var normalized = requestInfo.NormalizeUrlTemplate();

        // Assert
        Assert.That(normalized, Is.EqualTo("/api/funds/{pathParam1}/activities/{pathParam2}"));
    }

    [Test]
    public void NormalizeUrlTemplate_WithBaseUrlPrefix_ShouldStripIt()
    {
        // Arrange
        var requestInfo = new RequestInformation { UrlTemplate = "{+baseurl}/api/funds/{id}" };

        // Act
        var normalized = requestInfo.NormalizeUrlTemplate();

        // Assert
        Assert.That(normalized, Is.EqualTo("/api/funds/{pathParam1}"));
    }

    [Test]
    public void NormalizeUrlTemplate_WithQueryParameters_ShouldNormalizeThem()
    {
        // Arrange
        var requestInfo = new RequestInformation
        {
            UrlTemplate = "/api/funds/{id}{?select,expand}",
        };

        // Act
        var normalized = requestInfo.NormalizeUrlTemplate();

        // Assert
        Assert.That(normalized, Is.EqualTo("/api/funds/{pathParam1}{?queryParam1,queryParam2}"));
    }

    [Test]
    public void NormalizeUrlTemplate_WithComplexKiotaTemplate_ShouldNormalizeCorrectly()
    {
        // Arrange
        var requestInfo = new RequestInformation
        {
            UrlTemplate =
                "{+baseurl}/api/fundapplications/{fundApplicationId}/submissions/{submissionVersionNumber}/review{?select,expand}",
        };

        // Act
        var normalized = requestInfo.NormalizeUrlTemplate();

        // Assert
        Assert.That(
            normalized,
            Is.EqualTo(
                "/api/fundapplications/{pathParam1}/submissions/{pathParam2}/review{?queryParam1,queryParam2}"
            )
        );
    }

    [Test]
    public void NormalizeUrlTemplate_InVerificationPredicate_ShouldWork()
    {
        // Arrange
        var fundId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var requestInfo = new RequestInformation
        {
            HttpMethod = Method.POST,
            UrlTemplate = "{+baseurl}/api/funds/{fund-id}/activities/{activity-id}/modify",
        };
        requestInfo.PathParameters.Add("fund-id", fundId);
        requestInfo.PathParameters.Add("activity-id", activityId);

        // Act - Create a predicate similar to what would be used in verification
        Predicate<RequestInformation> verificationPredicate = req =>
            req.HttpMethod == Method.POST
            && req.NormalizeUrlTemplate()
                == "/api/funds/{pathParam1}/activities/{pathParam2}/modify"
            && req.PathParameters.Values.ElementAt(0).ToString() == fundId.ToString()
            && req.PathParameters.Values.ElementAt(1).ToString() == activityId.ToString();

        var result = verificationPredicate(requestInfo);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void NormalizeUrlTemplate_WithDifferentParameterNamingConventions_ShouldNormalizeSame()
    {
        // Arrange - Different naming conventions should produce same normalized result
        var requestInfo1 = new RequestInformation { UrlTemplate = "/api/funds/{fundId}" };
        var requestInfo2 = new RequestInformation { UrlTemplate = "/api/funds/{fund-id}" };
        var requestInfo3 = new RequestInformation { UrlTemplate = "/api/funds/{fund%2Did}" };

        // Act
        var normalized1 = requestInfo1.NormalizeUrlTemplate();
        var normalized2 = requestInfo2.NormalizeUrlTemplate();
        var normalized3 = requestInfo3.NormalizeUrlTemplate();

        // Assert
        Assert.That(normalized1, Is.EqualTo("/api/funds/{pathParam1}"));
        Assert.That(normalized2, Is.EqualTo("/api/funds/{pathParam1}"));
        Assert.That(normalized3, Is.EqualTo("/api/funds/{pathParam1}"));
        Assert.That(normalized1, Is.EqualTo(normalized2));
        Assert.That(normalized2, Is.EqualTo(normalized3));
    }

    #endregion
}
