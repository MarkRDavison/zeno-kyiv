namespace mark.davison.common.client.web.tests.CQRS;

public class QueryDispatcherTests
{
    private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
    private readonly Mock<IQueryHandler<ExampleQueryRequest, ExampleQueryResponse>> _handler;
    private readonly Mock<IServiceProvider> _serviceProvider;
    private readonly Mock<IServiceScope> _serviceScope;
    private readonly QueryDispatcher queryDispatcher;

    public QueryDispatcherTests()
    {
        _serviceScopeFactory = new(MockBehavior.Strict);
        _handler = new(MockBehavior.Strict);
        _serviceProvider = new(MockBehavior.Strict);
        _serviceScope = new(MockBehavior.Strict);
        queryDispatcher = new(_serviceScopeFactory.Object);
        _serviceScopeFactory.Setup(_ => _.CreateScope()).Returns(() => _serviceScope.Object);
        _serviceScope.Setup(_ => _.ServiceProvider).Returns(() => _serviceProvider.Object);
        _serviceScope.Setup(_ => _.Dispose());
    }

    [Test]
    public async Task Dispatch_RetrievesRequiredServices()
    {
        _serviceProvider
            .Setup(_ => _
                .GetService(typeof(IQueryHandler<ExampleQueryRequest, ExampleQueryResponse>)))
            .Returns(_handler.Object)
            .Verifiable();

        _handler
            .Setup(_ => _
                .Handle(
                    It.IsAny<ExampleQueryRequest>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExampleQueryResponse())
            .Verifiable();

        var response = await queryDispatcher.Dispatch<ExampleQueryRequest, ExampleQueryResponse>(new ExampleQueryRequest(), CancellationToken.None);

        await Assert.That(response).IsNotNull();

        _handler
            .Verify(_ => _
                .Handle(
                    It.IsAny<ExampleQueryRequest>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

        _serviceProvider.VerifyAll();
    }

    [Test]
    public async Task Dispatch_RetrievesRequiredServices_ForConstructedQuery()
    {
        _serviceProvider
            .Setup(_ => _
                .GetService(typeof(IQueryHandler<ExampleQueryRequest, ExampleQueryResponse>)))
            .Returns(_handler.Object)
            .Verifiable();

        _handler
            .Setup(_ => _
                .Handle(
                    It.IsAny<ExampleQueryRequest>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExampleQueryResponse())
            .Verifiable();

        var response = await queryDispatcher.Dispatch<ExampleQueryRequest, ExampleQueryResponse>(CancellationToken.None);

        await Assert.That(response).IsNotNull();

        _handler
            .Verify(_ => _
                .Handle(
                    It.IsAny<ExampleQueryRequest>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

        _serviceProvider.VerifyAll();
    }
}