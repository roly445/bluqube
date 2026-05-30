using BluQube.Queries;
using BluQube.Tests.RequesterHelpers.Stubs;

namespace BluQube.Tests.ResponderHelpers.Stubs;

public class StubQueryProcessor : IQueryProcessor<StubQuery, StubQueryResult>
{
    public ValueTask<QueryResult<StubQueryResult>> Handle(StubQuery request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(QueryResult<StubQueryResult>.Succeeded(new StubQueryResult("stub result")));
    }
}