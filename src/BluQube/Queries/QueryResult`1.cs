using System.Text.Json.Serialization;
using BluQube.Constants;
using MaybeMonad;

namespace BluQube.Queries;

public class QueryResult<T>
{
    private readonly Maybe<T> _data;

    private QueryResult(Maybe<T> data, QueryResultStatus status)
    {
        this._data = data;
        this.Status = status;
    }

    public QueryResultStatus Status { get; }

    public bool IsSucceeded => this.Status == QueryResultStatus.Succeeded;

    public bool IsFailed => this.Status == QueryResultStatus.Failed;

    public bool IsUnauthorized => this.Status == QueryResultStatus.Unauthorized;

    public bool IsNotFound => this.Status == QueryResultStatus.NotFound;

    public bool IsEmpty => this.Status == QueryResultStatus.Empty;

    public T Data
    {
        get
        {
            if (this.Status != QueryResultStatus.Succeeded)
            {
                throw new System.InvalidOperationException("Data is only available when the status is Succeeded");
            }

            return this._data.Value;
        }
    }

    public static QueryResult<T> Failed()
    {
        return new QueryResult<T>(Maybe<T>.Nothing, QueryResultStatus.Failed);
    }

    public static QueryResult<T> Succeeded(T data)
    {
        return new QueryResult<T>(Maybe.From(data), QueryResultStatus.Succeeded);
    }

    public static QueryResult<T> Unauthorized()
    {
        return new QueryResult<T>(Maybe<T>.Nothing, QueryResultStatus.Unauthorized);
    }

    public static QueryResult<T> NotFound()
    {
        return new QueryResult<T>(Maybe<T>.Nothing, QueryResultStatus.NotFound);
    }

    public static QueryResult<T> Empty()
    {
        return new QueryResult<T>(Maybe<T>.Nothing, QueryResultStatus.Empty);
    }
}