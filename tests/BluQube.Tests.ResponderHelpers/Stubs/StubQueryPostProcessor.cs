using BluQube.Queries;
using BluQube.Tests.RequesterHelpers.Stubs;

namespace BluQube.Tests.ResponderHelpers.Stubs;

public class StubQueryPostProcessor : IQueryProcessor<StubQueryPost, StubPostQueryResult>
{
    public Task<QueryResult<StubPostQueryResult>> Handle(StubQueryPost request, CancellationToken cancellationToken)
    {
        return Task.FromResult(QueryResult<StubPostQueryResult>.Succeeded(new StubPostQueryResult("stub POST result")));
    }
}