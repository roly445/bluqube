namespace BluQube.Mediation;

/// <summary>
/// Represents the next step in a BluQube mediator pipeline.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <param name="request">The request to handle.</param>
/// <param name="cancellationToken">A token to cancel the operation.</param>
/// <returns>The response.</returns>
public delegate ValueTask<TResponse> BluQubeRequestHandlerDelegate<in TRequest, TResponse>(
    TRequest request,
    CancellationToken cancellationToken);