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

    #region Query Parameter Extension Tests

    [Test]
    public void TryGetQueryParameter_WithExactMatch_ShouldReturnTrue()
    {
        // Arrange
        var requestInfo = new RequestInformation { UrlTemplate = "/api/test" };
        requestInfo.QueryParameters.Add("select", "id,name");

        // Act
        var result = requestInfo.TryGetQueryParameter("select", out var value);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(value, Is.EqualTo("id,name"));
    }

    [Test]
    public void TryGetQueryParameter_WithODataStyle_ShouldReturnTrue()
    {
        // Arrange
        var requestInfo = new RequestInformation { UrlTemplate = "/api/test" };
        requestInfo.QueryParameters.Add("$select", "id,name");

        // Act
        var result = requestInfo.TryGetQueryParameter("select", out var value);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(value, Is.EqualTo("id,name"));
    }

    [Test]
    public void TryGetQueryParameter_WithUrlEncodedODataStyle_ShouldReturnTrue()
    {
        // Arrange
        var requestInfo = new RequestInformation { UrlTemplate = "/api/test" };
        requestInfo.QueryParameters.Add("%24select", "id,name");

        // Act
        var result = requestInfo.TryGetQueryParameter("select", out var value);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(value, Is.EqualTo("id,name"));
    }

    [Test]
    public void TryGetQueryParameter_WithKebabCase_ShouldReturnTrue()
    {
        // Arrange
        var requestInfo = new RequestInformation { UrlTemplate = "/api/test" };
        requestInfo.QueryParameters.Add("order-by", "name");

        // Act
        var result = requestInfo.TryGetQueryParameter("orderBy", out var value);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(value, Is.EqualTo("name"));
    }

    [Test]
    public void TryGetQueryParameter_WithPascalCase_ShouldReturnTrue()
    {
        // Arrange
        var requestInfo = new RequestInformation { UrlTemplate = "/api/test" };
        requestInfo.QueryParameters.Add("OrderBy", "name");

        // Act
        var result = requestInfo.TryGetQueryParameter("orderBy", out var value);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(value, Is.EqualTo("name"));
    }

    [Test]
    public void TryGetQueryParameter_WithNotFound_ShouldReturnFalse()
    {
        // Arrange
        var requestInfo = new RequestInformation { UrlTemplate = "/api/test" };
        requestInfo.QueryParameters.Add("select", "id,name");

        // Act
        var result = requestInfo.TryGetQueryParameter("filter", out var value);

        // Assert
        Assert.That(result, Is.False);
        Assert.That(value, Is.Null);
    }

    [Test]
    public void TryGetQueryParameter_WithNullRequestInfo_ShouldReturnFalse()
    {
        // Arrange
        RequestInformation? requestInfo = null;

        // Act
        var result = requestInfo.TryGetQueryParameter("select", out var value);

        // Assert
        Assert.That(result, Is.False);
        Assert.That(value, Is.Null);
    }

    [Test]
    public void GetQueryParameter_WithExactMatch_ShouldReturnValue()
    {
        // Arrange
        var requestInfo = new RequestInformation { UrlTemplate = "/api/test" };
        requestInfo.QueryParameters.Add("select", "id,name");

        // Act
        var value = requestInfo.GetQueryParameter("select");

        // Assert
        Assert.That(value, Is.EqualTo("id,name"));
    }

    [Test]
    public void GetQueryParameter_WithODataStyle_ShouldReturnValue()
    {
        // Arrange
        var requestInfo = new RequestInformation { UrlTemplate = "/api/test" };
        requestInfo.QueryParameters.Add("$filter", "status eq 'active'");

        // Act
        var value = requestInfo.GetQueryParameter("filter");

        // Assert
        Assert.That(value, Is.EqualTo("status eq 'active'"));
    }

    [Test]
    public void GetQueryParameter_WithNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var requestInfo = new RequestInformation { UrlTemplate = "/api/test" };
        requestInfo.QueryParameters.Add("select", "id,name");

        // Act & Assert
        var ex = Assert.Throws<KeyNotFoundException>(() => requestInfo.GetQueryParameter("filter"));

        // Verify error message is helpful
        Assert.That(ex?.Message, Does.Contain("filter"));
        Assert.That(ex?.Message, Does.Contain("not found"));
        Assert.That(ex?.Message, Does.Contain("Tried the following naming variations"));
        Assert.That(ex?.Message, Does.Contain("Available query parameter keys"));
        Assert.That(ex?.Message, Does.Contain("select"));
    }

    [Test]
    public void GetQueryParameter_WithNoQueryParameters_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var requestInfo = new RequestInformation { UrlTemplate = "/api/test" };
        // Don't add any query parameters

        // Act & Assert
        var ex = Assert.Throws<KeyNotFoundException>(() => requestInfo.GetQueryParameter("select"));

        // Verify error message indicates no query parameters
        Assert.That(ex?.Message, Does.Contain("none - no query parameters"));
    }

    [Test]
    public void GetQueryParameter_WithMultipleNamingConventions_ShouldFindCorrectOne()
    {
        // Arrange - Test multiple query parameters with different naming conventions
        var requestInfo = new RequestInformation { UrlTemplate = "/api/test" };
        requestInfo.QueryParameters.Add("$select", "id,name");
        requestInfo.QueryParameters.Add("order-by", "name");
        requestInfo.QueryParameters.Add("PageSize", "10");

        // Act
        var selectValue = requestInfo.GetQueryParameter("select");
        var orderByValue = requestInfo.GetQueryParameter("orderBy");
        var pageSizeValue = requestInfo.GetQueryParameter("pageSize");

        // Assert
        Assert.That(selectValue, Is.EqualTo("id,name"));
        Assert.That(orderByValue, Is.EqualTo("name"));
        Assert.That(pageSizeValue, Is.EqualTo("10"));
    }

    [Test]
    public void GetQueryParameter_InPredicateWithODataParameter_ShouldWork()
    {
        // Arrange - Simulate real-world usage in a predicate
        var requestInfo = new RequestInformation
        {
            HttpMethod = Method.GET,
            UrlTemplate = "/api/items{?$select,$filter}",
        };
        requestInfo.QueryParameters.Add("$select", "id,name");
        requestInfo.QueryParameters.Add("$filter", "status eq 'active'");

        // Act - Use in a predicate like you would in mock setup
        Func<RequestInformation, bool> predicate = req =>
            req.HttpMethod == Method.GET
            && req.GetQueryParameter("select").ToString() == "id,name"
            && req.GetQueryParameter("filter").ToString() == "status eq 'active'";

        var result = predicate(requestInfo);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void TryGetQueryParameter_InPredicateWithMissingParameter_ShouldReturnFalse()
    {
        // Arrange - Simulate checking if optional parameter exists
        var requestInfo = new RequestInformation
        {
            HttpMethod = Method.GET,
            UrlTemplate = "/api/items{?$select}",
        };
        requestInfo.QueryParameters.Add("$select", "id,name");
        // Don't add $filter parameter

        // Act - Use in a predicate with optional parameter checking
        Func<RequestInformation, bool> predicate = req =>
        {
            // Check required parameter
            if (!req.TryGetQueryParameter("select", out var selectValue))
                return false;

            // Check optional parameter - should not fail if missing
            var hasFilter = req.TryGetQueryParameter("filter", out var filterValue);

            return selectValue.ToString() == "id,name" && !hasFilter;
        };

        var result = predicate(requestInfo);

        // Assert
        Assert.That(result, Is.True);
    }

    #endregion
}
