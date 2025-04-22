using Dunet;

namespace ApiAggregator.Common;

[Union]
public abstract partial record Result<T> where T : notnull
{
    public partial record Ok(T Value);

    public partial record Er(Problem Problem);
}

public static class ResultExtensions
{
    /// <summary>
    /// Produces a <see cref="StatusCodes.Status200OK"/> response.
    /// </summary>
    public static Task<IResult> ToOk<T>(this Task<Result<T>> result)
        where T : notnull
        => result.MatchAsync(ok => Results.Ok(ok.Value), ToError);

    /// <summary>
    /// Produces a <see cref="StatusCodes.Status201Created"/> response.
    /// </summary>
    /// <param name="createdAtUri">Produces the URI at which the content has been created.</param>
    public static Task<IResult> ToCreated<T>(this Task<Result<T>> result, Func<T, string>? createdAtUri = null)
        where T : notnull
        => result.MatchAsync(ok => Results.Created(uri: createdAtUri?.Invoke(ok.Value), value: ok.Value), ToError);

    /// <summary>
    /// Produces a <see cref="StatusCodes.Status202Accepted"/> response.
    /// </summary>
    /// <param name="acceptedAt">Produces the URI with the location at which the status of requested content can be monitored.</param>
    public static Task<IResult> ToAccepted<T>(this Task<Result<T>> result, Func<T, string>? acceptedAt = null)
        where T : notnull
        => result.MatchAsync(ok => Results.Accepted(acceptedAt?.Invoke(ok.Value), ok.Value), ToError);

    /// <summary>
    /// Produces a <see cref="StatusCodes.Status204NoContent"/> response.
    /// </summary>
    public static Task<IResult> ToNoContent<T>(this Task<Result<T>> result)
        where T : notnull
        => result.MatchAsync(ok => Results.NoContent(), ToError);

    private static IResult ToError<T>(Result<T>.Er er)
        where T : notnull
        => er.Problem switch
        {
            { Status: 400 } => TypedResults.BadRequest(er.Problem),
            { Status: 401 } => TypedResults.Unauthorized(),
            { Status: 403 } => TypedResults.Forbid(),
            { Status: 404 } => TypedResults.NotFound(er.Problem),
            { Status: 409 } => TypedResults.Conflict(er.Problem),
            { Status: 422 } => TypedResults.UnprocessableEntity(er.Problem),
            { Status: 500 } => TypedResults.InternalServerError(er.Problem),
            _ => TypedResults.StatusCode(er.Problem.Status)
        };
}
