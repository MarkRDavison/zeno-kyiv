namespace mark.davison.common.client.web.tests.CQRS;

public sealed class CQRSDispatcherTests
{

    private readonly Mock<IActionDispatcher> _actionDispatcher = new(MockBehavior.Strict);
    private readonly Mock<ICommandDispatcher> _commandDispatcher = new(MockBehavior.Strict);
    private readonly Mock<IQueryDispatcher> _queryDispatcher = new(MockBehavior.Strict);
    private readonly ICQRSDispatcher _dispatcher;

    public CQRSDispatcherTests()
    {
        _dispatcher = new CQRSDispatcher(_commandDispatcher.Object, _queryDispatcher.Object, _actionDispatcher.Object);
    }

    [Test]
    public async Task DispatchCommand_InvokesCorrectDispatcher()
    {
        _commandDispatcher
            .Setup(_ => _
                .Dispatch<ExampleCommandRequest, ExampleCommandResponse>(
                    It.IsAny<ExampleCommandRequest>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExampleCommandResponse())
            .Verifiable();

        await _dispatcher.Dispatch<ExampleCommandRequest, ExampleCommandResponse>(new ExampleCommandRequest(), CancellationToken.None);

        _commandDispatcher
            .Verify(_ => _
                .Dispatch<ExampleCommandRequest, ExampleCommandResponse>(
                    It.IsAny<ExampleCommandRequest>(),
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task DispatchCommand_InvokesCorrectDispatcher_ForConstructedCommand()
    {
        _commandDispatcher
            .Setup(_ => _
                .Dispatch<ExampleCommandRequest, ExampleCommandResponse>(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExampleCommandResponse())
            .Verifiable();

        await _dispatcher.Dispatch<ExampleCommandRequest, ExampleCommandResponse>(CancellationToken.None);

        _commandDispatcher
            .Verify(_ => _
                .Dispatch<ExampleCommandRequest, ExampleCommandResponse>(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task DispatchQuery_InvokesCorrectDispatcher()
    {
        _queryDispatcher
            .Setup(_ => _
                .Dispatch<ExampleQueryRequest, ExampleQueryResponse>(
                    It.IsAny<ExampleQueryRequest>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExampleQueryResponse())
            .Verifiable();

        await _dispatcher.Dispatch<ExampleQueryRequest, ExampleQueryResponse>(new ExampleQueryRequest(), CancellationToken.None);

        _queryDispatcher
            .Verify(_ => _
                .Dispatch<ExampleQueryRequest, ExampleQueryResponse>(
                    It.IsAny<ExampleQueryRequest>(),
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task DispatchQuery_InvokesCorrectDispatcher_ForConstructedQuery()
    {
        _queryDispatcher
            .Setup(_ => _
                .Dispatch<ExampleQueryRequest, ExampleQueryResponse>(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExampleQueryResponse())
            .Verifiable();

        await _dispatcher.Dispatch<ExampleQueryRequest, ExampleQueryResponse>(CancellationToken.None);

        _queryDispatcher
            .Verify(_ => _
                .Dispatch<ExampleQueryRequest, ExampleQueryResponse>(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task DispatchAction_InvokesCorrectDispatcher()
    {
        _actionDispatcher
            .Setup(_ => _
                .Dispatch<ExampleAction>(
                    It.IsAny<ExampleAction>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        await _dispatcher.Dispatch<ExampleAction>(new ExampleAction(), CancellationToken.None);

        _actionDispatcher
            .Verify(_ => _
                .Dispatch<ExampleAction>(
                    It.IsAny<ExampleAction>(),
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task DispatchAction_InvokesCorrectDispatcher_ForConstructedAction()
    {
        _actionDispatcher
            .Setup(_ => _
                .Dispatch<ExampleAction>(
                    It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        await _dispatcher.Dispatch<ExampleAction>(CancellationToken.None);

        _actionDispatcher
            .Verify(_ => _
                .Dispatch<ExampleAction>(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task DispatchResponseAction_InvokesCorrectDispatcher()
    {
        _actionDispatcher
            .Setup(_ => _
                .Dispatch<ExampleResponseActionRequest, ExampleResponseActionResponse>(
                    It.IsAny<ExampleResponseActionRequest>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new ExampleResponseActionResponse()))
            .Verifiable();

        await _dispatcher.Dispatch<ExampleResponseActionRequest, ExampleResponseActionResponse>(new ExampleResponseActionRequest(), CancellationToken.None);

        _actionDispatcher
            .Verify(_ => _
                .Dispatch<ExampleResponseActionRequest, ExampleResponseActionResponse>(
                    It.IsAny<ExampleResponseActionRequest>(),
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task DispatchResponseAction_InvokesCorrectDispatcher_ForConstructedAction()
    {
        _actionDispatcher
            .Setup(_ => _
                .Dispatch<ExampleResponseActionRequest, ExampleResponseActionResponse>(
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new ExampleResponseActionResponse()))
            .Verifiable();

        await _dispatcher.Dispatch<ExampleResponseActionRequest, ExampleResponseActionResponse>(CancellationToken.None);

        _actionDispatcher
            .Verify(_ => _
                .Dispatch<ExampleResponseActionRequest, ExampleResponseActionResponse>(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

}
