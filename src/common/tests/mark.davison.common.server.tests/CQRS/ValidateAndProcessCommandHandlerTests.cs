namespace mark.davison.common.server.tests.CQRS;

public class ValidateAndProcessCommandHandlerTests
{
    private class TestHandler : ValidateAndProcessCommandHandler<ExampleCommandRequest, ExampleCommandResponse>
    {
        public TestHandler(
            ICommandProcessor<ExampleCommandRequest, ExampleCommandResponse> processor
        ) : base(
            processor
        )
        {
        }

        public TestHandler(
            ICommandProcessor<ExampleCommandRequest, ExampleCommandResponse> processor,
            ICommandValidator<ExampleCommandRequest, ExampleCommandResponse> validator
        ) : base(
            processor,
            validator
        )
        {
        }
    }

    private readonly Mock<ICommandProcessor<ExampleCommandRequest, ExampleCommandResponse>> _processor;
    private readonly Mock<ICommandValidator<ExampleCommandRequest, ExampleCommandResponse>> _validator;
    private readonly Mock<ICurrentUserContext> _userContext;

    public ValidateAndProcessCommandHandlerTests()
    {
        _processor = new();
        _validator = new();
        _userContext = new();
    }

    [Test]
    public async Task Handle_WithValidator_InvokesValidator()
    {
        var handler = new TestHandler(_processor.Object, _validator.Object);

        _validator
            .Setup(_ => _.ValidateAsync(
                It.IsAny<ExampleCommandRequest>(),
                It.IsAny<ICurrentUserContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExampleCommandResponse())
            .Verifiable();

        await handler.Handle(new(), _userContext.Object, CancellationToken.None);

        _validator.VerifyAll();
    }

    [Test]
    public async Task Handle_WithoutValidator_DoesNotInvokeValidator()
    {
        var handler = new TestHandler(_processor.Object);

        _validator
            .Setup(_ => _.ValidateAsync(
                It.IsAny<ExampleCommandRequest>(),
                It.IsAny<ICurrentUserContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExampleCommandResponse())
            .Verifiable();

        await handler.Handle(new(), _userContext.Object, CancellationToken.None);

        _validator
            .Verify(_ =>
                _.ValidateAsync(
                    It.IsAny<ExampleCommandRequest>(),
                    It.IsAny<ICurrentUserContext>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
    }

    [Test]
    public async Task Handle_WithoutValidator_InvokesProcessor()
    {
        var handler = new TestHandler(_processor.Object);

        _processor
            .Setup(_ => _.ProcessAsync(
                It.IsAny<ExampleCommandRequest>(),
                It.IsAny<ICurrentUserContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExampleCommandResponse())
            .Verifiable();

        await handler.Handle(new(), _userContext.Object, CancellationToken.None);

        _processor.VerifyAll();
    }

    [Test]
    public async Task Handle_WithValidator_InvokesProcessor_IfValidationPasses()
    {
        var handler = new TestHandler(_processor.Object, _validator.Object);

        _validator
            .Setup(_ => _.ValidateAsync(
                It.IsAny<ExampleCommandRequest>(),
                It.IsAny<ICurrentUserContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExampleCommandResponse());

        _processor
            .Setup(_ => _.ProcessAsync(
                It.IsAny<ExampleCommandRequest>(),
                It.IsAny<ICurrentUserContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExampleCommandResponse())
            .Verifiable();

        await handler.Handle(new(), _userContext.Object, CancellationToken.None);

        _processor.VerifyAll();
    }

    [Test]
    public async Task Handle_WithValidator_DoesNotInvokeProcessor_IfValidationFails()
    {
        var handler = new TestHandler(_processor.Object, _validator.Object);

        _validator
            .Setup(_ => _.ValidateAsync(
                It.IsAny<ExampleCommandRequest>(),
                It.IsAny<ICurrentUserContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExampleCommandResponse { Errors = new() { "ERROR " } });

        _processor
            .Setup(_ => _.ProcessAsync(
                It.IsAny<ExampleCommandRequest>(),
                It.IsAny<ICurrentUserContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExampleCommandResponse())
            .Verifiable();

        await handler.Handle(new(), _userContext.Object, CancellationToken.None);

        _processor
            .Verify(_ =>
                _.ProcessAsync(
                    It.IsAny<ExampleCommandRequest>(),
                    It.IsAny<ICurrentUserContext>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
    }
}
