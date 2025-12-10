using mark.davison.common.CQRS;
using mark.davison.common.generators.CQRS;
using mark.davison.common.server.abstractions.CQRS;
using System.Runtime;

namespace mark.davison.common.generators.tests.CQRS;

public sealed class IncrementalCQRSGeneratorTests
{
    [Test]
    public async Task TestCQRSGeneration()
    {
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using mark.davison.common.CQRS;
using mark.davison.common.source.generators.CQRS;
using mark.davison.common.server.abstractions.CQRS;
using mark.davison.common.server.CQRS.Processors;
using mark.davison.common.server.CQRS.Validators;
using mark.davison.common.server.abstractions.Authentication;

namespace mark.davison.tests.api
{
    [UseCQRSServer(typeof(ApiRoot))]
    public class ApiRoot
    {
    }
}

namespace mark.davison.tests.shared
{
    [PostRequest(Path = ""test-command"", AllowAnonymous = true)]
    public sealed class TestCommand : ICommand<TestCommand, TestCommandResponse>
    {

    }

    public sealed class TestCommandResponse : Response
    {

    }

    public sealed class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
    {
        public async Task<TestCommandResponse> Handle(TestCommand command, ICurrentUserContext currentUserContext, CancellationToken cancellation)
        {
            return await Task.FromResult(new TestCommandResponse());
        }
    }

    public sealed class TestCommandProcessor : ICommandProcessor<TestCommand, TestCommandResponse>
    {
        public async Task<TestCommandResponse> ProcessAsync(TestCommand request, ICurrentUserContext currentUserContext, CancellationToken cancellationToken)
        {
            return await Task.FromResult(new TestCommandResponse());
        }
    }

    public sealed class TestCommandValidator : ICommandValidator<TestCommand, TestCommandResponse>
    {
        public async Task<TestCommandResponse> ValidateAsync(TestCommand request, ICurrentUserContext currentUserContext, CancellationToken cancellationToken)
        {
            return await Task.FromResult(new TestCommandResponse());
        }
    }

    [GetRequest(Path = ""test-query"")]
    public sealed class TestQuery : IQuery<TestQuery, TestQueryResponse>
    {

    }

    public sealed class TestQueryResponse : Response
    {

    }

    public sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
    {
        public async Task<TestQueryResponse> Handle(TestQuery Query, ICurrentUserContext currentUserContext, CancellationToken cancellation)
        {
            return await Task.FromResult(new TestQueryResponse());
        }
    }

    public sealed class TestQueryProcessor : IQueryProcessor<TestQuery, TestQueryResponse>
    {
        public async Task<TestQueryResponse> ProcessAsync(TestQuery request, ICurrentUserContext currentUserContext, CancellationToken cancellationToken)
        {
            return await Task.FromResult(new TestQueryResponse());
        }
    }

    public sealed class TestQueryValidator : IQueryValidator<TestQuery, TestQueryResponse>
    {
        public async Task<TestQueryResponse> ValidateAsync(TestQuery request, ICurrentUserContext currentUserContext, CancellationToken cancellationToken)
        {
            return await Task.FromResult(new TestQueryResponse());
        }
    }
}
";

        var result = GeneratorTestHelpers.RunSourceGenerator<IncrementalMetadataCQRSGenerator>(
            source,
            [
                typeof(Object),
                typeof(GCSettings),
                typeof(CQRSType),
                typeof(ICommand<,>),
                typeof(IQuery<,>),
                typeof(ICommandHandler<,>),
                typeof(ICommandProcessor<,>),
                typeof(ICommandValidator<,>),
                typeof(PostRequestAttribute),
                typeof(GetRequestAttribute),
                typeof(SourceGeneratorHelpers)
            ]);

        var expectedHintNameDependencyInjection = "CQRSServerDependecyInjectionExtensions.g.cs";
        var expectedHintNameEndpointRoute = "GenerateCQRSEndpointRouteExtensions.g.cs";

        await Assert.That(result.Results).HasSingleItem();
        await Assert.That(result.Results.Single().GeneratedSources.Where(_ => _.HintName == expectedHintNameDependencyInjection)).HasSingleItem();
        await Assert.That(result.Results.Single().GeneratedSources.Where(_ => _.HintName == expectedHintNameEndpointRoute)).HasSingleItem();

        var di = result.Results
            .First()
            .GeneratedSources
            .First(_ => _.HintName == expectedHintNameDependencyInjection);

        var sourceStringDi = di.SourceText.ToString();

        var er = result.Results
            .First()
            .GeneratedSources
            .First(_ => _.HintName == expectedHintNameEndpointRoute);

        var sourceStringEr = er.SourceText.ToString();

        await Assert.That(sourceStringDi).IsNotNullOrEmpty();
        await Assert.That(sourceStringEr).IsNotNullOrEmpty();

        await Assert.That(sourceStringDi).Contains("namespace mark.davison.tests.api");
        await Assert.That(sourceStringDi).Contains("public static class CQRSDependecyInjectionExtensions");
        await Assert.That(sourceStringDi).Contains("services.AddScoped<mark.davison.common.CQRS.IQueryDispatcher, mark.davison.common.server.CQRS.QueryDispatcher>();");
        await Assert.That(sourceStringDi).Contains("services.AddScoped<mark.davison.common.CQRS.ICommandDispatcher, mark.davison.common.server.CQRS.CommandDispatcher>();");
        await Assert.That(sourceStringDi).Contains("services.AddScoped<ICommandProcessor<global::mark.davison.tests.shared.TestCommand,global::mark.davison.tests.shared.TestCommandResponse>,global::mark.davison.tests.shared.TestCommandProcessor>();");
        await Assert.That(sourceStringDi).Contains("services.AddScoped<IQueryProcessor<global::mark.davison.tests.shared.TestQuery,global::mark.davison.tests.shared.TestQueryResponse>,global::mark.davison.tests.shared.TestQueryProcessor>();");

        await Assert.That(sourceStringEr).Contains("namespace mark.davison.tests.api");
        await Assert.That(sourceStringEr).Contains("public static class GenerateEndpointRouteExtensions");
        await Assert.That(sourceStringEr).Contains("public static IEndpointRouteBuilder MapCQRSEndpoints(this IEndpointRouteBuilder endpoints)");

        await Assert.That(sourceStringEr).Contains("endpoints.MapPost(");
        await Assert.That(sourceStringEr).Contains("\"/api/test-command\"");
        await Assert.That(sourceStringEr).Contains("var request = await WebUtilities.GetRequestFromBody<global::mark.davison.tests.shared.TestCommand,global::mark.davison.tests.shared.TestCommandResponse>(context.Request);");
        await Assert.That(sourceStringEr).Contains("return await dispatcher.Dispatch<global::mark.davison.tests.shared.TestCommand,global::mark.davison.tests.shared.TestCommandResponse>(request, cancellationToken);");

        await Assert.That(sourceStringEr).Contains("endpoints.MapGet(");
        await Assert.That(sourceStringEr).Contains("\"/api/test-query\"");
        await Assert.That(sourceStringEr).Contains("var request = WebUtilities.GetRequestFromQuery<global::mark.davison.tests.shared.TestQuery,global::mark.davison.tests.shared.TestQueryResponse>(context.Request);");
        await Assert.That(sourceStringEr).Contains("return await dispatcher.Dispatch<global::mark.davison.tests.shared.TestQuery,global::mark.davison.tests.shared.TestQueryResponse>(request, cancellationToken);");
    }
}