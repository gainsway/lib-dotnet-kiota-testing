using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;

namespace Gainsway.Kiota.Testing.Tests;

/// <summary>
/// Integration tests that actually invoke mocked Kiota client methods.
/// These tests verify the complete flow: setup mock -> invoke client method -> verify response.
/// </summary>
[TestFixture]
public class IntegrationTests
{
    [Test]
    public async Task MockClientResponse_WithSingleObject_ShouldReturnMockedObject()
    {
        // Arrange
        var client = KiotaClientMockExtensions.GetMockableClient<TestApiClient>();
        var fundId = Guid.NewGuid();
        var expectedFund = new FundDto { Id = fundId.ToString(), Name = "Test Fund" };

        client.MockClientResponse(
            "/api/funds/{id}",
            expectedFund,
            req => req.PathParameters["id"].ToString() == fundId.ToString()
        );

        // Act
        var result = await client.GetFundAsync(fundId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(fundId.ToString()));
        Assert.That(result.Name, Is.EqualTo("Test Fund"));
    }

    [Test]
    public async Task MockClientResponse_WithString_ShouldReturnMockedString()
    {
        // Arrange
        var client = KiotaClientMockExtensions.GetMockableClient<TestApiClient>();
        var expectedStatus = "active";

        client.MockClientResponse("/api/status", expectedStatus);

        // Act
        var result = await client.GetStatusAsync();

        // Assert
        Assert.That(result, Is.EqualTo("active"));
    }

    [Test]
    public async Task MockClientCollectionResponse_WithMultipleItems_ShouldReturnCollection()
    {
        // Arrange
        var client = KiotaClientMockExtensions.GetMockableClient<TestApiClient>();
        var fundId = Guid.NewGuid();
        var expectedActivities = new List<ActivityDto>
        {
            new ActivityDto { Id = Guid.NewGuid().ToString(), Name = "Activity 1" },
            new ActivityDto { Id = Guid.NewGuid().ToString(), Name = "Activity 2" },
            new ActivityDto { Id = Guid.NewGuid().ToString(), Name = "Activity 3" },
        };

        client.MockClientCollectionResponse(
            "/api/funds/{fundId}/activities",
            expectedActivities,
            req => req.PathParameters["fundId"].ToString() == fundId.ToString()
        );

        // Act
        var result = await client.GetActivitiesAsync(fundId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result.First().Name, Is.EqualTo("Activity 1"));
        Assert.That(result.Last().Name, Is.EqualTo("Activity 3"));
    }

    [Test]
    public async Task MockClientNoContentResponse_ShouldCompleteSuccessfully()
    {
        // Arrange
        var client = KiotaClientMockExtensions.GetMockableClient<TestApiClient>();
        var fundId = Guid.NewGuid();

        client.MockClientNoContentResponse(
            "/api/funds/{id}",
            req => req.PathParameters["id"].ToString() == fundId.ToString()
        );

        // Act & Assert - Should not throw
        await client.DeleteFundAsync(fundId);
        Assert.Pass("Delete operation completed without exceptions");
    }

    [Test]
    public async Task MockClientResponse_WithMultiplePathParameters_ShouldMatchCorrectly()
    {
        // Arrange
        var client = KiotaClientMockExtensions.GetMockableClient<TestApiClient>();
        var fundId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var expectedActivity = new ActivityDto
        {
            Id = activityId.ToString(),
            Name = "Modified Activity",
        };

        client.MockClientResponse(
            "/api/funds/{fundId}/activities/{activityId}/modify",
            expectedActivity,
            req =>
                req.PathParameters["fundId"].ToString() == fundId.ToString()
                && req.PathParameters["activityId"].ToString() == activityId.ToString()
        );

        // Act
        var result = await client.ModifyActivityAsync(fundId, activityId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(activityId.ToString()));
        Assert.That(result.Name, Is.EqualTo("Modified Activity"));
    }

    [Test]
    public async Task MockClientResponse_WithNullResponse_ShouldReturnNull()
    {
        // Arrange
        var client = KiotaClientMockExtensions.GetMockableClient<TestApiClient>();
        var fundId = Guid.NewGuid();

        client.MockClientResponse<TestApiClient, FundDto>(
            "/api/funds/{id}",
            null,
            req => req.PathParameters["id"].ToString() == fundId.ToString()
        );

        // Act
        var result = await client.GetFundAsync(fundId);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task MockClientResponse_MultipleDifferentEndpoints_ShouldNotInterfere()
    {
        // Arrange
        var client = KiotaClientMockExtensions.GetMockableClient<TestApiClient>();
        var fundId1 = Guid.NewGuid();
        var fundId2 = Guid.NewGuid();

        var fund1 = new FundDto { Id = fundId1.ToString(), Name = "Fund 1" };
        var fund2 = new FundDto { Id = fundId2.ToString(), Name = "Fund 2" };
        var activities = new List<ActivityDto>
        {
            new ActivityDto { Id = Guid.NewGuid().ToString(), Name = "Activity 1" },
        };

        // Mock different endpoints
        client.MockClientResponse(
            "/api/funds/{id}",
            fund1,
            req => req.PathParameters["id"].ToString() == fundId1.ToString()
        );

        client.MockClientResponse(
            "/api/funds/{id}",
            fund2,
            req => req.PathParameters["id"].ToString() == fundId2.ToString()
        );

        client.MockClientCollectionResponse(
            "/api/funds/{fundId}/activities",
            activities,
            req => req.PathParameters["fundId"].ToString() == fundId1.ToString()
        );

        // Act
        var resultFund1 = await client.GetFundAsync(fundId1);
        var resultFund2 = await client.GetFundAsync(fundId2);
        var resultActivities = await client.GetActivitiesAsync(fundId1);

        // Assert
        Assert.That(resultFund1, Is.Not.Null);
        Assert.That(resultFund1.Id, Is.EqualTo(fundId1.ToString()));
        Assert.That(resultFund1.Name, Is.EqualTo("Fund 1"));

        Assert.That(resultFund2, Is.Not.Null);
        Assert.That(resultFund2.Id, Is.EqualTo(fundId2.ToString()));
        Assert.That(resultFund2.Name, Is.EqualTo("Fund 2"));

        Assert.That(resultActivities, Has.Count.EqualTo(1));
        Assert.That(resultActivities.First().Name, Is.EqualTo("Activity 1"));
    }

    [Test]
    public void MockClientResponseException_WithNotFoundException_ShouldSetupWithoutError()
    {
        // Arrange
        var client = KiotaClientMockExtensions.GetMockableClient<TestApiClient>();
        var nonExistentId = Guid.NewGuid();
        var exception = new ApiException("Fund not found") { ResponseStatusCode = 404 };

        // Act & Assert - verifies mock setup doesn't throw
        Assert.DoesNotThrow(() =>
        {
            client.MockClientResponseException<TestApiClient, FundDto>(
                "/api/funds/{id}",
                exception,
                req => req.PathParameters["id"].ToString() == nonExistentId.ToString()
            );
        });
    }

    [Test]
    public void MockClientResponse_WithPostMethodPredicate_ShouldSetupWithoutError()
    {
        // Arrange
        var client = KiotaClientMockExtensions.GetMockableClient<TestApiClient>();
        var newFund = new FundDto { Id = Guid.NewGuid().ToString(), Name = "New Fund" };

        // Act & Assert - verifies POST mock setup doesn't throw
        Assert.DoesNotThrow(() =>
        {
            client.MockClientResponse(
                "/api/funds",
                newFund,
                req => req.HttpMethod == Method.POST && req.Content != null
            );
        });
    }

    [Test]
    public void MockClientResponse_WithPutMethodPredicate_ShouldSetupWithoutError()
    {
        // Arrange
        var client = KiotaClientMockExtensions.GetMockableClient<TestApiClient>();
        var existingId = Guid.NewGuid();
        var updatedFund = new FundDto { Id = existingId.ToString(), Name = "Updated Fund" };

        // Act & Assert - verifies PUT mock setup doesn't throw
        Assert.DoesNotThrow(() =>
        {
            client.MockClientResponse(
                "/api/funds/{id}",
                updatedFund,
                req =>
                    req.HttpMethod == Method.PUT
                    && req.PathParameters["id"].ToString() == existingId.ToString()
                    && req.Content != null
            );
        });
    }
}

#region Test Client Implementation

/// <summary>
/// Test implementation of a Kiota-generated API client.
/// This mimics the structure of actual Kiota-generated clients.
/// </summary>
public class TestApiClient : BaseRequestBuilder
{
    public TestApiClient(IRequestAdapter requestAdapter)
        : base(requestAdapter, "{+baseurl}", new Dictionary<string, object>()) { }

    /// <summary>
    /// Get a fund by ID
    /// </summary>
    public async Task<FundDto?> GetFundAsync(
        Guid fundId,
        CancellationToken cancellationToken = default
    )
    {
        var requestInfo = new RequestInformation
        {
            HttpMethod = Method.GET,
            UrlTemplate = "{+baseurl}/api/funds/{id}",
        };
        requestInfo.PathParameters.Add("id", fundId);

        return await RequestAdapter.SendAsync(
            requestInfo,
            FundDto.CreateFromDiscriminatorValue,
            default,
            cancellationToken
        );
    }

    /// <summary>
    /// Get status as a string
    /// </summary>
    public async Task<string?> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var requestInfo = new RequestInformation
        {
            HttpMethod = Method.GET,
            UrlTemplate = "{+baseurl}/api/status",
        };

        return await RequestAdapter.SendPrimitiveAsync<string>(
            requestInfo,
            default,
            cancellationToken
        );
    }

    /// <summary>
    /// Get activities for a fund
    /// </summary>
    public async Task<List<ActivityDto>> GetActivitiesAsync(
        Guid fundId,
        CancellationToken cancellationToken = default
    )
    {
        var requestInfo = new RequestInformation
        {
            HttpMethod = Method.GET,
            UrlTemplate = "{+baseurl}/api/funds/{fundId}/activities",
        };
        requestInfo.PathParameters.Add("fundId", fundId);

        var result = await RequestAdapter.SendCollectionAsync(
            requestInfo,
            ActivityDto.CreateFromDiscriminatorValue,
            default,
            cancellationToken
        );

        return result?.ToList() ?? new List<ActivityDto>();
    }

    /// <summary>
    /// Delete a fund
    /// </summary>
    public async Task DeleteFundAsync(Guid fundId, CancellationToken cancellationToken = default)
    {
        var requestInfo = new RequestInformation
        {
            HttpMethod = Method.DELETE,
            UrlTemplate = "{+baseurl}/api/funds/{id}",
        };
        requestInfo.PathParameters.Add("id", fundId);

        await RequestAdapter.SendNoContentAsync(requestInfo, default, cancellationToken);
    }

    /// <summary>
    /// Modify an activity
    /// </summary>
    public async Task<ActivityDto?> ModifyActivityAsync(
        Guid fundId,
        Guid activityId,
        CancellationToken cancellationToken = default
    )
    {
        var requestInfo = new RequestInformation
        {
            HttpMethod = Method.POST,
            UrlTemplate = "{+baseurl}/api/funds/{fundId}/activities/{activityId}/modify",
        };
        requestInfo.PathParameters.Add("fundId", fundId);
        requestInfo.PathParameters.Add("activityId", activityId);

        return await RequestAdapter.SendAsync(
            requestInfo,
            ActivityDto.CreateFromDiscriminatorValue,
            default,
            cancellationToken
        );
    }
}

/// <summary>
/// Test DTO for Fund entity with factory method
/// </summary>
public class FundDto : IParsable
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public static FundDto CreateFromDiscriminatorValue(IParseNode parseNode)
    {
        return new FundDto();
    }

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
/// Test DTO for Activity entity with factory method
/// </summary>
public class ActivityDto : IParsable
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public static ActivityDto CreateFromDiscriminatorValue(IParseNode parseNode)
    {
        return new ActivityDto();
    }

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
