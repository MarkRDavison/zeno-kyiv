namespace mark.davison.common.client.web.tests.CQRS;

public sealed class CommandDispatcherTests
{
    private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
    private readonly Mock<ICommandHandler<ExampleCommandRequest, ExampleCommandResponse>> _handler;
    private readonly Mock<IServiceProvider> _serviceProvider;
    private readonly Mock<IServiceScope> _serviceScope;
    private readonly CommandDispatcher commandDispatcher;

    public CommandDispatcherTests()
    {
        _serviceScopeFactory = new(MockBehavior.Strict);
        _handler = new(MockBehavior.Strict);
        _serviceProvider = new(MockBehavior.Strict);
        _serviceScope = new(MockBehavior.Strict);
        commandDispatcher = new(_serviceScopeFactory.Object);
        _serviceScopeFactory.Setup(_ => _.CreateScope()).Returns(() => _serviceScope.Object);
        _serviceScope.Setup(_ => _.ServiceProvider).Returns(() => _serviceProvider.Object);
        _serviceScope.Setup(_ => _.Dispose());
    }

    [Test]
    public async Task Dispatch_RetrievesRequiredServices()
    {
        _serviceProvider
            .Setup(_ => _
                .GetService(typeof(ICommandHandler<ExampleCommandRequest, ExampleCommandResponse>)))
            .Returns(_handler.Object)
            .Verifiable();

        _handler
            .Setup(_ => _
                .Handle(
                    It.IsAny<ExampleCommandRequest>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExampleCommandResponse())
            .Verifiable();

        var response = await commandDispatcher.Dispatch<ExampleCommandRequest, ExampleCommandResponse>(new ExampleCommandRequest(), CancellationToken.None);

        await Assert.That(response).IsNotNull();

        _handler
            .Verify(_ => _
                .Handle(
                    It.IsAny<ExampleCommandRequest>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

        _serviceProvider.VerifyAll();
    }

    [Test]
    public async Task Dispatch_RetrievesRequiredServices_ForConstructedCommand()
    {
        _serviceProvider
            .Setup(_ => _
                .GetService(typeof(ICommandHandler<ExampleCommandRequest, ExampleCommandResponse>)))
            .Returns(_handler.Object)
            .Verifiable();

        _handler
            .Setup(_ => _
                .Handle(
                    It.IsAny<ExampleCommandRequest>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExampleCommandResponse())
            .Verifiable();

        var response = await commandDispatcher.Dispatch<ExampleCommandRequest, ExampleCommandResponse>(CancellationToken.None);

        await Assert.That(response).IsNotNull();

        _handler
            .Verify(_ => _
                .Handle(
                    It.IsAny<ExampleCommandRequest>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

        _serviceProvider.VerifyAll();
    }
}
