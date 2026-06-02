namespace BluQube.Mediation;

/// <summary>
/// Defines a behavior that can run before or after a BluQube request handler.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface IBluQubePipelineBehavior<TRequest, TResponse>
{
    /// <summary>
    /// Handles the request or delegates to the next pipeline step.
    /// </summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="next">The next pipeline step.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The response.</returns>
    ValueTask<TResponse> Handle(
        TRequest request,
        BluQubeRequestHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken);
}