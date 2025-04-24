using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using NSubstitute;

namespace Gainsway.Kiota.Testing;

/// <summary>
/// Provides extension methods for mocking Kiota-generated client classes and their responses.
/// </summary>
public static class KiotaClientMockExtensions
{
    /// <summary>
    /// Creates a predicate expression to match a <see cref="RequestInformation"/> object
    /// based on its URL template ending with the specified value.
    /// </summary>
    /// <param name="urlTemplate">The URL template to match.</param>
    /// <returns>An expression that evaluates to true if the URL template matches.</returns>
    private static Expression<Predicate<RequestInformation>> RequestInformationUrlTemplatePredicate(
        string urlTemplate
    ) => req => req.UrlTemplate!.EndsWith(urlTemplate);

    /// <summary>
    /// Creates a mock of the client class.
    /// This is a generic method that can be used to create a mock of any Kiota generated client class.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T GetClientMock<T>()
        where T : class
    {
        IRequestAdapter _clientAdapter;
        _clientAdapter = Substitute.For<IRequestAdapter>();

        return (T)Activator.CreateInstance(typeof(T), _clientAdapter);
    }

    /// <summary>
    /// Retrieves the <see cref="IRequestAdapter"/> instance from a mocked Kiota client.
    /// </summary>
    /// <typeparam name="T">The type of the mocked Kiota client.</typeparam>
    /// <param name="mockedClient">The mocked client instance.</param>
    /// <returns>The <see cref="IRequestAdapter"/> instance associated with the mocked client.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the RequestAdapter property is not found on the mocked client.
    /// </exception>
    private static IRequestAdapter GetRequestAdapter<T>(T mockedClient)
    {
        return mockedClient!
                .GetType()
                ?.GetProperty("RequestAdapter", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(mockedClient) as IRequestAdapter
            ?? throw new InvalidOperationException(
                "RequestAdapter property not found on mocked client."
            );
    }

    /// <summary>
    /// Mocks a single object response for a Kiota client request.
    /// </summary>
    /// <typeparam name="T">The type of the Kiota client request builder.</typeparam>
    /// <typeparam name="R">The type of the response object.</typeparam>
    /// <param name="mockedClient">The mocked client request builder instance.</param>
    /// <param name="urlTemplate">The URL template to match the request.</param>
    /// <param name="returnObject">The object to return as the response.</param>
    /// <param name="requestInfoPredicate">
    /// An optional predicate to further filter the request information.
    /// </param>

    public static void MockClientResponse<T, R>(
        this T mockedClient,
        string urlTemplate,
        R returnObject,
        Expression<Predicate<RequestInformation>>? requestInfoPredicate = null
    )
        where T : BaseRequestBuilder
        where R : IParsable
    {
        var requestAdapter = GetRequestAdapter(mockedClient);

        var requestInformationUrlTemplatePredicate = RequestInformationUrlTemplatePredicate(
            urlTemplate
        );
        var requestInformationPredicate =
            requestInfoPredicate != null
                ? requestInfoPredicate.And(requestInformationUrlTemplatePredicate)
                : requestInformationUrlTemplatePredicate;

        requestAdapter
            ?.SendAsync(
                Arg.Is(requestInformationPredicate),
                Arg.Any<ParsableFactory<R>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(returnObject);
    }

    /// <summary>
    /// Mocks a collection response for a Kiota client request.
    /// </summary>
    /// <typeparam name="T">The type of the Kiota client request builder.</typeparam>
    /// <typeparam name="R">The type of the response objects in the collection.</typeparam>
    /// <param name="mockedClient">The mocked client request builder instance.</param>
    /// <param name="urlTemplate">The URL template to match the request.</param>
    /// <param name="returnObject">The collection of objects to return as the response.</param>
    /// <param name="requestInfoPredicate">
    /// An optional predicate to further filter the request information.
    /// </param>
    public static void MockClientCollectionResponse<T, R>(
        this T mockedClient,
        string urlTemplate,
        IEnumerable<R> returnObject,
        Expression<Predicate<RequestInformation>>? requestInfoPredicate = null
    )
        where T : BaseRequestBuilder
        where R : IParsable
    {
        var requestAdapter = GetRequestAdapter(mockedClient);

        var requestInformationUrlTemplatePredicate = RequestInformationUrlTemplatePredicate(
            urlTemplate
        );
        var requestInformationPredicate =
            requestInfoPredicate != null
                ? requestInfoPredicate.And(requestInformationUrlTemplatePredicate)
                : requestInformationUrlTemplatePredicate;

        requestAdapter
            ?.SendCollectionAsync(
                Arg.Is(requestInformationPredicate),
                Arg.Any<ParsableFactory<R>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(returnObject);
    }
}
