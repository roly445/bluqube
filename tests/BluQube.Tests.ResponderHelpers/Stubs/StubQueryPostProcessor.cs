using BluQube.Queries;
using BluQube.Tests.RequesterHelpers.Stubs;

namespace BluQube.Tests.ResponderHelpers.Stubs;

public class StubQueryPostProcessor : IQueryProcessor<StubQueryPost, StubPostQueryResult>
{
    public ValueTask<QueryResult<StubPostQueryResult>> Handle(StubQueryPost request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(QueryResult<StubPostQueryResult>.Succeeded(new StubPostQueryResult("stub POST result")));
    }
}