# Gainsway.Kiota.Testing

This library adds helpers and extensions to simplify Kiota generated Clients mocking.

## Usage

0. Add dependencies

```sh
dotnet add package Gainsway.Kiota.Testing
```

1. Create a Mocked Client
Use `GetClientMock<T>()` to create an instance of a Kiota-generated client with a mocked `IRequestAdapter`:

```csharp
var mockedClient = KiotaClientMockExtensions.GetClientMock<MyKiotaClient>();
```

2. Mock a Single Object Response
Use `MockClientResponse` to specify what the client should return for a given URL template:

```csharp
mockedClient.MockClientResponse(
    urlTemplate: "/items/{itemId}",
    returnObject: myResponseObject
);
```

When your code calls SendAsync against the mocked client with the matching URL template, it will return myResponseObject.

3. Mock a Collection Response
Use `MockClientCollectionResponse` to return a collection of objects:

```csharp
mockedClient.MockClientCollectionResponse(
    urlTemplate: "/items",
    returnObject: new List<MyResponse> { item1, item2 }
);
```

When your code calls `SendCollectionAsync`, it will return the specified list.

4. Write and Run Your Test
Once the mocked client is set up, inject or pass it into the class under test, call the method being tested, and verify the behavior:

```csharp
[Test]
public async Task MyService_ShouldReturnItems_FromMockedClient()
{
    // Arrange
    var mockedClient = KiotaClientMockExtensions.GetClientMock<MyKiotaClient>();
    mockedClient.MockClientCollectionResponse(
        "/items",
        new List<MyResponse> { new MyResponse() }
    );

    mockedClient.MockClientResponse(
        urlTemplate: "/items/{itemId}",
        returnObject: new MyResponse(),
        (req) => req.PathParameters["itemId"].ToString() == "123-123-123"
    );
    var serviceUnderTest = new MyService(mockedClient);

    // Act
    var result = await serviceUnderTest.GetItemsAsync();

    // Assert
    Assert.NotNull(result);
    Assert.IsNotEmpty(result);
}
```
