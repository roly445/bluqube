using BluQube.Queries;
using BluQube.Tests.RequesterHelpers.Stubs;

namespace BluQube.Tests.ResponderHelpers.Stubs;

public class StubQueryGetProcessor : IQueryProcessor<StubQueryGet, StubGetQueryResult>
{
    public ValueTask<QueryResult<StubGetQueryResult>> Handle(StubQueryGet request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(QueryResult<StubGetQueryResult>.Succeeded(new StubGetQueryResult("stub GET result")));
    }
}