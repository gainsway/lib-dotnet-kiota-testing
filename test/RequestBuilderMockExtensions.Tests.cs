using Gainsway.Kiota.Testing;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using NSubstitute.Exceptions;
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
    public async Task VerifyGetAsync_WhenEndpointWasCalled_ShouldPass()
    {
        // Arrange
        var itemId = "123";
        var expectedResponse = new TestResponse { Value = "test-value" };
        _mockClient.Api.Items[itemId].MockGetAsync(expectedResponse);

        // Act
        var result = await _mockClient.Api.Items[itemId].GetAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value, Is.EqualTo("test-value"));

        await _mockClient.Api.Items[itemId].VerifyGetAsync(Times.Once);
    }

    [Test]
    public async Task VerifyGetAsync_WithNoTimesArgument_ShouldAssertAtLeastOnce()
    {
        // Arrange
        var itemId = "123";
        _mockClient.Api.Items[itemId].MockGetAsync(new TestResponse { Value = "test-value" });

        // Act - called twice
        await _mockClient.Api.Items[itemId].GetAsync();
        await _mockClient.Api.Items[itemId].GetAsync();

        // Assert - default is "at least once", so two calls satisfy it
        await _mockClient.Api.Items[itemId].VerifyGetAsync();
    }

    [Test]
    public async Task VerifyGetAsync_WithNoTimesArgument_ShouldThrowWhenNeverCalled()
    {
        // Arrange
        var itemId = "123";
        _mockClient.Api.Items[itemId].MockGetAsync(new TestResponse { Value = "test-value" });

        // Act - deliberately no call

        // Assert - "at least once" must fail when there were no calls
        Assert.ThrowsAsync<ReceivedCallsException>(async () =>
            await _mockClient.Api.Items[itemId].VerifyGetAsync()
        );
    }

    [Test]
    public async Task VerifyGetAsync_WithTimesNever_ShouldPassWhenNotCalled()
    {
        // Arrange
        var itemId = "123";
        _mockClient.Api.Items[itemId].MockGetAsync(new TestResponse { Value = "test-value" });

        // Act - deliberately no call

        // Assert
        await _mockClient.Api.Items[itemId].VerifyGetAsync(Times.Never);
    }

    [Test]
    public async Task VerifyGetAsync_WithTimesNever_ShouldThrowWhenCalled()
    {
        // Arrange
        var itemId = "123";
        _mockClient.Api.Items[itemId].MockGetAsync(new TestResponse { Value = "test-value" });

        // Act
        await _mockClient.Api.Items[itemId].GetAsync();

        // Assert
        Assert.ThrowsAsync<ReceivedCallsException>(async () =>
            await _mockClient.Api.Items[itemId].VerifyGetAsync(Times.Never)
        );
    }

    [Test]
    public async Task VerifyGetAsync_WithTimesExactly_ShouldMatchCallCount()
    {
        // Arrange
        var itemId = "123";
        _mockClient.Api.Items[itemId].MockGetAsync(new TestResponse { Value = "test-value" });

        // Act - called three times
        await _mockClient.Api.Items[itemId].GetAsync();
        await _mockClient.Api.Items[itemId].GetAsync();
        await _mockClient.Api.Items[itemId].GetAsync();

        // Assert
        await _mockClient.Api.Items[itemId].VerifyGetAsync(Times.Exactly(3));
    }

    [Test]
    public async Task VerifyGetAsync_WithIntLiteral_ShouldConvertToExactCount()
    {
        // Arrange
        var itemId = "123";
        _mockClient.Api.Items[itemId].MockGetAsync(new TestResponse { Value = "test-value" });

        // Act
        await _mockClient.Api.Items[itemId].GetAsync();
        await _mockClient.Api.Items[itemId].GetAsync();

        // Assert - implicit int conversion keeps arbitrary counts terse
        await _mockClient.Api.Items[itemId].VerifyGetAsync(2);
    }

    [Test]
    public async Task VerifyGetAsync_WhenCallCountDiffers_ShouldThrow()
    {
        // Arrange
        var itemId = "123";
        _mockClient.Api.Items[itemId].MockGetAsync(new TestResponse { Value = "test-value" });

        // Act - called once
        await _mockClient.Api.Items[itemId].GetAsync();

        // Assert - expecting two calls must fail, proving the assertion is real
        Assert.ThrowsAsync<ReceivedCallsException>(async () =>
            await _mockClient.Api.Items[itemId].VerifyGetAsync(Times.Exactly(2))
        );
    }

    [Test]
    public void Times_Exactly_WithNegativeCount_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Times.Exactly(-1));
    }

    [Test]
    public async Task VerifyGetAsync_ShouldDistinguishBetweenPathParameters()
    {
        // Arrange
        var calledId = "123";
        var otherId = "456";
        _mockClient.Api.Items[calledId].MockGetAsync(new TestResponse { Value = "called" });
        _mockClient.Api.Items[otherId].MockGetAsync(new TestResponse { Value = "other" });

        // Act - only one of the two endpoints is called
        await _mockClient.Api.Items[calledId].GetAsync();

        // Assert - verification is scoped to the exact path parameter
        await _mockClient.Api.Items[calledId].VerifyGetAsync(Times.Once);
        await _mockClient.Api.Items[otherId].VerifyGetAsync(Times.Never);
    }

    [Test]
    public async Task VerifyGetAsync_ForPrimitive_ShouldVerifyCall()
    {
        // Arrange
        _mockClient.Api.Status.MockGetAsync("operational");

        // Act
        var result = await _mockClient.Api.Status.GetAsync();

        // Assert
        Assert.That(result, Is.EqualTo("operational"));

        await _mockClient.Api.Status.VerifyGetAsync(Times.Once);
        Assert.ThrowsAsync<ReceivedCallsException>(async () =>
            await _mockClient.Api.Status.VerifyGetAsync(Times.Never)
        );
    }

    [Test]
    public async Task VerifyGetAsync_ForCollection_ShouldVerifyCall()
    {
        // Arrange
        _mockClient.Api.Items.MockGetCollectionAsync(
            new List<TestResponse> { new TestResponse { Value = "item-1" } }
        );

        // Act
        var result = await _mockClient.Api.Items.GetAsync();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));

        await _mockClient.Api.Items.VerifyGetAsync(Times.Once);
        Assert.ThrowsAsync<ReceivedCallsException>(async () =>
            await _mockClient.Api.Items.VerifyGetAsync(Times.Never)
        );
    }

    [Test]
    public async Task VerifyPostAsync_ShouldVerifyCall()
    {
        // Arrange
        _mockClient.Api.Items.MockPostAsync(new TestResponse { Value = "created" });

        // Act
        var result = await _mockClient.Api.Items.PostAsync();

        // Assert
        Assert.That(result!.Value, Is.EqualTo("created"));

        await _mockClient.Api.Items.VerifyPostAsync(Times.Once);
        Assert.ThrowsAsync<ReceivedCallsException>(async () =>
            await _mockClient.Api.Items.VerifyPostAsync(Times.Never)
        );
    }

    [Test]
    public async Task VerifyPostAsync_ForCollection_ShouldVerifyCall()
    {
        // Arrange
        _mockClient.Api.Items.MockPostCollectionAsync(
            new List<TestResponse> { new TestResponse { Value = "created-1" } }
        );

        // Act
        var result = await _mockClient.Api.Items.PostCollectionAsync();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));

        await _mockClient.Api.Items.VerifyPostAsync(Times.Once);
        Assert.ThrowsAsync<ReceivedCallsException>(async () =>
            await _mockClient.Api.Items.VerifyPostAsync(Times.Never)
        );
    }

    [Test]
    public async Task VerifyPutAsync_ShouldVerifyCall()
    {
        // Arrange
        var itemId = "123";
        _mockClient.Api.Items[itemId].MockPutAsync(new TestResponse { Value = "updated" });

        // Act
        var result = await _mockClient.Api.Items[itemId].PutAsync();

        // Assert
        Assert.That(result!.Value, Is.EqualTo("updated"));

        await _mockClient.Api.Items[itemId].VerifyPutAsync(Times.Once);
        Assert.ThrowsAsync<ReceivedCallsException>(async () =>
            await _mockClient.Api.Items[itemId].VerifyPutAsync(Times.Never)
        );
    }

    [Test]
    public async Task VerifyPutAsync_ShouldMatch_RegardlessOfWhetherTheRealCallReturnedAResponse()
    {
        // Proves the collapse actually works: one VerifyPutAsync() call, with no response type
        // argument, correctly matches whichever IRequestAdapter member the real call happened
        // to invoke - SendAsync<T> here, SendNoContentAsync in the sibling no-content test below.
        var withResponseId = "123";
        var noContentId = "456";
        _mockClient.Api.Items[withResponseId].MockPutAsync(new TestResponse { Value = "updated" });
        _mockClient.Api.Items[noContentId].MockPutAsync();

        await _mockClient.Api.Items[withResponseId].PutAsync();
        await _mockClient.Api.Items[noContentId].PutNoContentAsync(new TestRequest { Flag = true });

        await _mockClient.Api.Items[withResponseId].VerifyPutAsync(Times.Once);
        await _mockClient.Api.Items[noContentId].VerifyPutAsync(Times.Once);
    }

    [Test]
    public async Task VerifyPatchAsync_ShouldVerifyCall()
    {
        // Arrange
        var itemId = "123";
        _mockClient.Api.Items[itemId].MockPatchAsync(new TestResponse { Value = "patched" });

        // Act
        var result = await _mockClient.Api.Items[itemId].PatchAsync();

        // Assert
        Assert.That(result!.Value, Is.EqualTo("patched"));

        await _mockClient.Api.Items[itemId].VerifyPatchAsync(Times.Once);
        Assert.ThrowsAsync<ReceivedCallsException>(async () =>
            await _mockClient.Api.Items[itemId].VerifyPatchAsync(Times.Never)
        );
    }

    [Test]
    public async Task VerifyPutAsync_ForNoContent_ShouldVerifyCall()
    {
        // Arrange
        var itemId = "123";
        _mockClient.Api.Items[itemId].MockPutAsync();

        // Act
        await _mockClient.Api.Items[itemId].PutNoContentAsync(new TestRequest { Flag = true });

        // Assert
        await _mockClient.Api.Items[itemId].VerifyPutAsync(Times.Once);
        Assert.ThrowsAsync<ReceivedCallsException>(async () =>
            await _mockClient.Api.Items[itemId].VerifyPutAsync(Times.Never)
        );
    }

    [Test]
    public async Task VerifyPatchAsync_ForNoContent_ShouldVerifyCall()
    {
        // Arrange
        var itemId = "123";
        _mockClient.Api.Items[itemId].MockPatchAsync();

        // Act
        await _mockClient.Api.Items[itemId].PatchNoContentAsync(new TestRequest { Flag = true });

        // Assert
        await _mockClient.Api.Items[itemId].VerifyPatchAsync(Times.Once);
        Assert.ThrowsAsync<ReceivedCallsException>(async () =>
            await _mockClient.Api.Items[itemId].VerifyPatchAsync(Times.Never)
        );
    }

    [Test]
    public async Task VerifyPostAsync_ForNoContent_ShouldVerifyCall()
    {
        // Arrange
        _mockClient.Api.Items.MockPostAsync();

        // Act
        await _mockClient.Api.Items.PostNoContentAsync(new TestRequest { Flag = true });

        // Assert
        await _mockClient.Api.Items.VerifyPostAsync(Times.Once);
        Assert.ThrowsAsync<ReceivedCallsException>(async () =>
            await _mockClient.Api.Items.VerifyPostAsync(Times.Never)
        );
    }

    [Test]
    public async Task VerifyPutAsync_WithBody_ShouldPass_WhenCountAndBodyBothMatch()
    {
        // Arrange
        var itemId = "123";
        _mockClient.Api.Items[itemId].MockPutAsync();

        // Act
        await _mockClient.Api.Items[itemId].PutNoContentAsync(new TestRequest { Flag = true });

        // Assert - count + body in a single chained assertion
        await _mockClient
            .Api.Items[itemId]
            .VerifyPutAsync(Times.Once)
            .WithBody<TestRequest>(body => body.Flag == true);
    }

    [Test]
    public async Task VerifyPutAsync_WithBody_ShouldThrow_WhenCountMatchesButBodyDoesNot()
    {
        // Arrange
        var itemId = "123";
        _mockClient.Api.Items[itemId].MockPutAsync();
        await _mockClient.Api.Items[itemId].PutNoContentAsync(new TestRequest { Flag = false });

        // Act - one call happened (satisfying Times.Once), but its body doesn't match
        var ex = Assert.ThrowsAsync<ReceivedCallsException>(async () =>
            await _mockClient
                .Api.Items[itemId]
                .VerifyPutAsync(Times.Once)
                .WithBody<TestRequest>(body => body.Flag == true)
        );

        // Assert - the actual (rejected) body is included, so a wrong-value bug is
        // diagnosable from the failure message alone, without attaching a debugger.
        Assert.That(ex!.Message, Does.Contain("\"flag\":false"));
    }

    [Test]
    public void VerifyPutAsync_WithBody_ShouldThrow_WhenCountDoesNotMatch()
    {
        // Arrange - count check must run and fail before the body is ever inspected
        var itemId = "123";
        _mockClient.Api.Items[itemId].MockPutAsync();

        // Act - deliberately no call

        // Assert
        Assert.ThrowsAsync<ReceivedCallsException>(async () =>
            await _mockClient
                .Api.Items[itemId]
                .VerifyPutAsync(Times.Once)
                .WithBody<TestRequest>(body => body.Flag == true)
        );
    }

    [Test]
    public async Task VerifyPutAsync_WithBody_ShouldPass_WhenTimesNeverAndNoCallWasMade()
    {
        // Arrange - Times.Never + WithBody: nothing was expected, so there's no body to check
        var itemId = "123";
        _mockClient.Api.Items[itemId].MockPutAsync();

        // Act - deliberately no call

        // Assert
        await _mockClient
            .Api.Items[itemId]
            .VerifyPutAsync(Times.Never)
            .WithBody<TestRequest>(body => body.Flag == true);
    }

    [Test]
    public async Task VerifyPutAsync_AwaitedDirectly_ShouldStillWork_AfterWithBodyWasAdded()
    {
        // Confirms the pre-WithBody call shape still compiles and behaves the same way -
        // VerifyCallAssertion must remain directly awaitable, not just chainable.
        var itemId = "123";
        _mockClient.Api.Items[itemId].MockPutAsync();

        await _mockClient.Api.Items[itemId].PutNoContentAsync(new TestRequest { Flag = true });

        await _mockClient.Api.Items[itemId].VerifyPutAsync(Times.Once);
        Assert.ThrowsAsync<ReceivedCallsException>(async () =>
            await _mockClient.Api.Items[itemId].VerifyPutAsync(Times.Never)
        );
    }

    [Test]
    public async Task VerifyDeleteAsync_ForNoContent_ShouldVerifyCall()
    {
        // Arrange
        var itemId = "123";
        _mockClient.Api.Items[itemId].MockDeleteAsync();

        // Act
        await _mockClient.Api.Items[itemId].DeleteAsync();

        // Assert
        await _mockClient.Api.Items[itemId].VerifyDeleteAsync(Times.Once);
        Assert.ThrowsAsync<ReceivedCallsException>(async () =>
            await _mockClient.Api.Items[itemId].VerifyDeleteAsync(Times.Never)
        );
    }

    [Test]
    public async Task VerifyDeleteAsync_ForResponse_ShouldVerifyCall()
    {
        // Arrange
        var itemId = "123";
        _mockClient.Api.Items[itemId].MockDeleteAsync(new TestResponse { Value = "deleted" });

        // Act
        var result = await _mockClient.Api.Items[itemId].DeleteWithResponseAsync();

        // Assert
        Assert.That(result!.Value, Is.EqualTo("deleted"));

        await _mockClient.Api.Items[itemId].VerifyDeleteAsync(Times.Once);
        Assert.ThrowsAsync<ReceivedCallsException>(async () =>
            await _mockClient.Api.Items[itemId].VerifyDeleteAsync(Times.Never)
        );
    }

    [Test]
    public async Task VerifyDeleteAsync_ForCollection_ShouldVerifyCall()
    {
        // Arrange
        _mockClient.Api.Items.MockDeleteCollectionAsync(
            new List<TestResponse> { new TestResponse { Value = "deleted-1" } }
        );

        // Act
        var result = await _mockClient.Api.Items.DeleteCollectionAsync();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));

        await _mockClient.Api.Items.VerifyDeleteAsync(Times.Once);
        Assert.ThrowsAsync<ReceivedCallsException>(async () =>
            await _mockClient.Api.Items.VerifyDeleteAsync(Times.Never)
        );
    }

    [Test]
    public async Task Verify_ShouldDistinguishBetweenHttpMethods()
    {
        // Arrange - same endpoint, two different verbs
        var itemId = "123";
        _mockClient.Api.Items[itemId].MockPutAsync(new TestResponse { Value = "updated" });
        _mockClient.Api.Items[itemId].MockPatchAsync(new TestResponse { Value = "patched" });

        // Act - only PUT is called
        await _mockClient.Api.Items[itemId].PutAsync();

        // Assert - PATCH on the same URL must not count as a PUT
        await _mockClient.Api.Items[itemId].VerifyPutAsync(Times.Once);
        await _mockClient.Api.Items[itemId].VerifyPatchAsync(Times.Never);
    }

    [Test]
    public async Task Verify_WithRequestPredicate_ShouldNarrowMatch()
    {
        // Arrange
        var itemId = "123";
        _mockClient.Api.Items[itemId].MockGetAsync(new TestResponse { Value = "test-value" });

        // Act
        await _mockClient.Api.Items[itemId].GetAsync();

        // Assert - a predicate that matches, and one that does not
        await _mockClient
            .Api.Items[itemId]
            .VerifyGetAsync(Times.Once, req => req.PathParameters.ContainsKey("id"));

        Assert.ThrowsAsync<ReceivedCallsException>(async () =>
            await _mockClient
                .Api.Items[itemId]
                .VerifyGetAsync(Times.Once, req => req.PathParameters.ContainsKey("nonexistent"))
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
    public void MockDeleteAsync_WithSingleObjectResponse_ShouldSetupMockSuccessfully()
    {
        // Arrange
        var deletedItem = new TestResponse { Value = "deleted-item" };

        // Act - Type-safe API setup for DELETE with response body
        _mockClient.Api.Items["123"].MockDeleteAsync(deletedItem);

        // Assert - Verify mock setup completed without exceptions
        Assert.That(_mockClient, Is.Not.Null);
        Assert.Pass(
            "Mock setup successful using type-safe API: _mockClient.Api.Items[id].MockDeleteAsync(response)"
        );
    }

    [Test]
    public void MockDeleteAsync_WithSingleObjectAndPredicate_ShouldSetupMockSuccessfully()
    {
        // Arrange
        var deletedItem = new TestResponse { Value = "deleted-item" };

        // Act - Type-safe API setup for DELETE with response body and predicate
        _mockClient.Api.Items["123"].MockDeleteAsync(deletedItem, req => req.Content != null);

        // Assert - Verify mock setup completed without exceptions
        Assert.That(_mockClient, Is.Not.Null);
        Assert.Pass(
            "Mock setup successful using type-safe API: _mockClient.Api.Items[id].MockDeleteAsync(response, predicate)"
        );
    }

    [Test]
    public void MockDeleteAsync_WithException_ShouldSetupMockSuccessfully()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Cannot delete item");

        // Act - Type-safe API setup for DELETE that throws exception
        _mockClient
            .Api.Items["123"]
            .MockDeleteAsync<ItemRequestBuilder, TestResponse>(expectedException);

        // Assert - Verify mock setup completed without exceptions
        Assert.That(_mockClient, Is.Not.Null);
        Assert.Pass(
            "Mock setup successful using type-safe API: _mockClient.Api.Items[id].MockDeleteAsync<TBuilder, TResponse>(exception)"
        );
    }

    [Test]
    public void MockDeleteCollectionAsync_ShouldSetupMockSuccessfully()
    {
        // Arrange
        var deletedItems = new List<TestResponse>
        {
            new TestResponse { Value = "deleted-item-1" },
            new TestResponse { Value = "deleted-item-2" },
        };

        // Act - Type-safe API setup for DELETE with collection response
        _mockClient.Api.Items.MockDeleteCollectionAsync(deletedItems);

        // Assert - Verify mock setup completed without exceptions
        Assert.That(_mockClient, Is.Not.Null);
        Assert.Pass(
            "Mock setup successful using type-safe API: _mockClient.Api.Items.MockDeleteCollectionAsync(collection)"
        );
    }

    [Test]
    public void MockDeleteCollectionAsync_WithPredicate_ShouldSetupMockSuccessfully()
    {
        // Arrange
        var deletedItems = new List<TestResponse>
        {
            new TestResponse { Value = "deleted-item-1" },
            new TestResponse { Value = "deleted-item-2" },
        };

        // Act - Type-safe API setup for DELETE with collection response and predicate
        _mockClient.Api.Items.MockDeleteCollectionAsync(deletedItems, req => req.Content != null);

        // Assert - Verify mock setup completed without exceptions
        Assert.That(_mockClient, Is.Not.Null);
        Assert.Pass(
            "Mock setup successful using type-safe API: _mockClient.Api.Items.MockDeleteCollectionAsync(collection, predicate)"
        );
    }

    [Test]
    public void MockDeleteCollectionAsync_WithException_ShouldSetupMockSuccessfully()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Bulk delete not allowed");

        // Act - Type-safe API setup for DELETE collection that throws exception
        _mockClient.Api.Items.MockDeleteCollectionAsync<ItemsRequestBuilder, TestResponse>(
            expectedException
        );

        // Assert - Verify mock setup completed without exceptions
        Assert.That(_mockClient, Is.Not.Null);
        Assert.Pass(
            "Mock setup successful using type-safe API: _mockClient.Api.Items.MockDeleteCollectionAsync<TBuilder, TResponse>(exception)"
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

    [Test]
    public void MockDelete_AllVariants_ShouldSetupSuccessfully()
    {
        // This test demonstrates all DELETE variants working together

        // Arrange
        var itemId = "item-1";
        var deletedItem = new TestResponse { Value = "deleted-item" };
        var deletedItems = new List<TestResponse>
        {
            new TestResponse { Value = "deleted-1" },
            new TestResponse { Value = "deleted-2" },
        };

        // Act - Setup all DELETE variants
        // 1. No content DELETE
        _mockClient.Api.Items[itemId].MockDeleteAsync();

        // 2. DELETE with single object response
        _mockClient.Api.Items["item-2"].MockDeleteAsync(deletedItem);

        // 3. DELETE with collection response
        _mockClient.Api.Items.MockDeleteCollectionAsync(deletedItems);

        // 4. DELETE with exception
        _mockClient
            .Api.Items["item-error"]
            .MockDeleteAsync<ItemRequestBuilder, TestResponse>(
                new InvalidOperationException("Cannot delete")
            );

        // Assert
        Assert.That(_mockClient, Is.Not.Null);
        Assert.Pass(
            "All DELETE variants setup successfully: no-content, single object, collection, and exception"
        );
    }
}

#region Test Helper Classes

/// <summary>
/// Base for the test builders, providing the RequestInformation construction that
/// Kiota-generated builders perform before calling the adapter.
/// </summary>
public abstract class TestRequestBuilderBase : BaseRequestBuilder
{
    protected TestRequestBuilderBase(
        IRequestAdapter requestAdapter,
        string urlTemplate,
        Dictionary<string, object> pathParameters
    )
        : base(requestAdapter, urlTemplate, pathParameters) { }

    /// <summary>
    /// Builds RequestInformation from this builder's own UrlTemplate and PathParameters.
    /// </summary>
    protected RequestInformation BuildRequest(Method method)
    {
        var requestInfo = new RequestInformation { HttpMethod = method, UrlTemplate = UrlTemplate };

        foreach (var param in PathParameters)
        {
            requestInfo.PathParameters.Add(param.Key, param.Value);
        }

        return requestInfo;
    }
}

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
public class ItemsRequestBuilder : TestRequestBuilderBase
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

    /// <summary>
    /// Sends a GET request returning a collection.
    /// </summary>
    public async Task<List<TestResponse>> GetAsync(CancellationToken cancellationToken = default)
    {
        var result = await RequestAdapter.SendCollectionAsync(
            BuildRequest(Method.GET),
            TestResponse.CreateFromDiscriminatorValue,
            default,
            cancellationToken
        );

        return result?.ToList() ?? new List<TestResponse>();
    }

    /// <summary>
    /// Sends a POST request returning a single object.
    /// </summary>
    public async Task<TestResponse?> PostAsync(CancellationToken cancellationToken = default)
    {
        return await RequestAdapter.SendAsync(
            BuildRequest(Method.POST),
            TestResponse.CreateFromDiscriminatorValue,
            default,
            cancellationToken
        );
    }

    /// <summary>
    /// Sends a POST request returning a collection.
    /// </summary>
    public async Task<List<TestResponse>> PostCollectionAsync(
        CancellationToken cancellationToken = default
    )
    {
        var result = await RequestAdapter.SendCollectionAsync(
            BuildRequest(Method.POST),
            TestResponse.CreateFromDiscriminatorValue,
            default,
            cancellationToken
        );

        return result?.ToList() ?? new List<TestResponse>();
    }

    /// <summary>
    /// Sends a POST request with a body that returns no content, mirroring a bare
    /// [HttpPost] action/status-transition endpoint.
    /// </summary>
    public async Task PostNoContentAsync(
        TestRequest body,
        CancellationToken cancellationToken = default
    )
    {
        var requestInfo = BuildRequest(Method.POST);
        requestInfo.SetContentFromParsable(RequestAdapter, "application/json", body);

        await RequestAdapter.SendNoContentAsync(requestInfo, default, cancellationToken);
    }

    /// <summary>
    /// Sends a DELETE request returning a collection.
    /// </summary>
    public async Task<List<TestResponse>> DeleteCollectionAsync(
        CancellationToken cancellationToken = default
    )
    {
        var result = await RequestAdapter.SendCollectionAsync(
            BuildRequest(Method.DELETE),
            TestResponse.CreateFromDiscriminatorValue,
            default,
            cancellationToken
        );

        return result?.ToList() ?? new List<TestResponse>();
    }
}

/// <summary>
/// Single item request builder (for operations on a specific item)
/// </summary>
public class ItemRequestBuilder : TestRequestBuilderBase
{
    public ItemRequestBuilder(IRequestAdapter requestAdapter, string id)
        : base(
            requestAdapter,
            "{+baseurl}/api/items/{id}",
            new Dictionary<string, object> { { "id", id } }
        ) { }

    /// <summary>
    /// Sends a GET request, mirroring how a Kiota-generated builder calls the adapter.
    /// </summary>
    public async Task<TestResponse?> GetAsync(CancellationToken cancellationToken = default)
    {
        return await RequestAdapter.SendAsync(
            BuildRequest(Method.GET),
            TestResponse.CreateFromDiscriminatorValue,
            default,
            cancellationToken
        );
    }

    /// <summary>
    /// Sends a PUT request returning a single object.
    /// </summary>
    public async Task<TestResponse?> PutAsync(CancellationToken cancellationToken = default)
    {
        return await RequestAdapter.SendAsync(
            BuildRequest(Method.PUT),
            TestResponse.CreateFromDiscriminatorValue,
            default,
            cancellationToken
        );
    }

    /// <summary>
    /// Sends a PUT request with a body that returns no content, mirroring a fire-and-forget
    /// replication write between services.
    /// </summary>
    public async Task PutNoContentAsync(
        TestRequest body,
        CancellationToken cancellationToken = default
    )
    {
        var requestInfo = BuildRequest(Method.PUT);
        requestInfo.SetContentFromParsable(RequestAdapter, "application/json", body);

        await RequestAdapter.SendNoContentAsync(requestInfo, default, cancellationToken);
    }

    /// <summary>
    /// Sends a PATCH request returning a single object.
    /// </summary>
    public async Task<TestResponse?> PatchAsync(CancellationToken cancellationToken = default)
    {
        return await RequestAdapter.SendAsync(
            BuildRequest(Method.PATCH),
            TestResponse.CreateFromDiscriminatorValue,
            default,
            cancellationToken
        );
    }

    /// <summary>
    /// Sends a PATCH request with a body that returns no content, mirroring a partial-update
    /// endpoint backed by a bare [HttpPatch] action.
    /// </summary>
    public async Task PatchNoContentAsync(
        TestRequest body,
        CancellationToken cancellationToken = default
    )
    {
        var requestInfo = BuildRequest(Method.PATCH);
        requestInfo.SetContentFromParsable(RequestAdapter, "application/json", body);

        await RequestAdapter.SendNoContentAsync(requestInfo, default, cancellationToken);
    }

    /// <summary>
    /// Sends a DELETE request that returns no content.
    /// </summary>
    public async Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        await RequestAdapter.SendNoContentAsync(
            BuildRequest(Method.DELETE),
            default,
            cancellationToken
        );
    }

    /// <summary>
    /// Sends a DELETE request that returns a single object.
    /// </summary>
    public async Task<TestResponse?> DeleteWithResponseAsync(
        CancellationToken cancellationToken = default
    )
    {
        return await RequestAdapter.SendAsync(
            BuildRequest(Method.DELETE),
            TestResponse.CreateFromDiscriminatorValue,
            default,
            cancellationToken
        );
    }
}

/// <summary>
/// Status endpoint request builder
/// </summary>
public class StatusRequestBuilder : TestRequestBuilderBase
{
    public StatusRequestBuilder(IRequestAdapter requestAdapter)
        : base(requestAdapter, "{+baseurl}/api/status", new Dictionary<string, object>()) { }

    /// <summary>
    /// Sends a GET request returning a primitive string.
    /// </summary>
    public async Task<string?> GetAsync(CancellationToken cancellationToken = default)
    {
        return await RequestAdapter.SendPrimitiveAsync<string>(
            BuildRequest(Method.GET),
            default,
            cancellationToken
        );
    }
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

/// <summary>
/// Test request body object (IParsable), for exercising VerifyRequestBodyAsync.
/// </summary>
public class TestRequest : IParsable
{
    public bool? Flag { get; set; }

    public static TestRequest CreateFromDiscriminatorValue(IParseNode parseNode)
    {
        return new TestRequest();
    }

    public IDictionary<string, Action<IParseNode>> GetFieldDeserializers()
    {
        return new Dictionary<string, Action<IParseNode>>
        {
            { "flag", n => Flag = n.GetBoolValue() },
        };
    }

    public void Serialize(ISerializationWriter writer)
    {
        writer.WriteBoolValue("flag", Flag);
    }
}

#endregion
