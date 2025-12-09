namespace mark.davison.common.server.tests.CQRS;


public sealed class QueryDispatcherTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
    private readonly Mock<IQueryHandler<ExampleQueryRequest, ExampleQueryResponse>> _handler;
    private readonly Mock<ICurrentUserContext> _currentUserContext;
    private readonly Mock<IServiceProvider> _serviceProvider;
    private readonly QueryDispatcher queryDispatcher;
    private readonly HttpContext _httpContext;

    public QueryDispatcherTests()
    {
        _httpContextAccessor = new(MockBehavior.Strict);
        _handler = new(MockBehavior.Strict);
        _currentUserContext = new(MockBehavior.Strict);
        _serviceProvider = new(MockBehavior.Strict);
        queryDispatcher = new(_httpContextAccessor.Object);
        _httpContext = new DefaultHttpContext();
        _httpContext.RequestServices = _serviceProvider.Object;
        _httpContextAccessor.Setup(_ => _.HttpContext).Returns(() => _httpContext);
    }

    [Test]
    public async Task Dispatch_RetrievesRequiredServices()
    {
        _serviceProvider
            .Setup(_ => _
                .GetService(typeof(IQueryHandler<ExampleQueryRequest, ExampleQueryResponse>)))
            .Returns(_handler.Object)
            .Verifiable();

        _serviceProvider
            .Setup(_ => _
                .GetService(typeof(ICurrentUserContext)))
            .Returns(_currentUserContext.Object)
            .Verifiable();

        _handler
            .Setup(_ => _
                .Handle(
                    It.IsAny<ExampleQueryRequest>(),
                    It.IsAny<ICurrentUserContext>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExampleQueryResponse())
            .Verifiable();

        var response = await queryDispatcher.Dispatch<ExampleQueryRequest, ExampleQueryResponse>(new ExampleQueryRequest(), CancellationToken.None);

        await Assert.That(response).IsNotNull();

        _handler
            .Verify(_ => _
                .Handle(
                    It.IsAny<ExampleQueryRequest>(),
                    It.IsAny<ICurrentUserContext>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

        _serviceProvider.VerifyAll();
    }
}
