namespace mark.davison.common.server.tests.CQRS;

public sealed class CommandDispatcherTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
    private readonly Mock<ICommandHandler<ExampleCommandRequest, ExampleCommandResponse>> _handler;
    private readonly Mock<ICurrentUserContext> _currentUserContext;
    private readonly Mock<IServiceProvider> _serviceProvider;
    private readonly CommandDispatcher commandDispatcher;
    private readonly HttpContext _httpContext;

    public CommandDispatcherTests()
    {
        _httpContextAccessor = new(MockBehavior.Strict);
        _handler = new(MockBehavior.Strict);
        _currentUserContext = new(MockBehavior.Strict);
        _serviceProvider = new(MockBehavior.Strict);
        commandDispatcher = new(_httpContextAccessor.Object);
        _httpContext = new DefaultHttpContext();
        _httpContext.RequestServices = _serviceProvider.Object;
        _httpContextAccessor.Setup(_ => _.HttpContext).Returns(() => _httpContext);
    }

    [Test]
    public async Task Dispatch_RetrievesRequiredServices()
    {
        _serviceProvider
            .Setup(_ => _
                .GetService(typeof(ICommandHandler<ExampleCommandRequest, ExampleCommandResponse>)))
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
                    It.IsAny<ExampleCommandRequest>(),
                    It.IsAny<ICurrentUserContext>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExampleCommandResponse())
            .Verifiable();

        var response = await commandDispatcher.Dispatch<ExampleCommandRequest, ExampleCommandResponse>(new ExampleCommandRequest(), CancellationToken.None);

        await Assert.That(response).IsNotNull();

        _handler
            .Verify(_ => _
                .Handle(
                    It.IsAny<ExampleCommandRequest>(),
                    It.IsAny<ICurrentUserContext>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

        _serviceProvider.VerifyAll();
    }
}
