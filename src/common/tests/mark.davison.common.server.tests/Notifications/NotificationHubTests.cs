namespace mark.davison.common.server.tests.Notifications;

public class NotificationHubTests
{
    private readonly Mock<INotificationService> _service1;
    private readonly Mock<INotificationService> _service2;
    private readonly Mock<INotificationService> _disabledService;

    private readonly Mock<NotificationSettings> _service1Settings;
    private readonly Mock<NotificationSettings> _service2Settings;
    private readonly Mock<NotificationSettings> _disabledServiceSettings;

    private readonly NotificationHub _hub;

    public NotificationHubTests()
    {
        _service1 = new();
        _service2 = new();
        _disabledService = new();

        _service1Settings = new();
        _service2Settings = new();
        _disabledServiceSettings = new();

        _service1Settings.Setup(_ => _.ENABLED).Returns(true);
        _service2Settings.Setup(_ => _.ENABLED).Returns(true);
        _disabledServiceSettings.Setup(_ => _.ENABLED).Returns(false);

        _service1.Setup(_ => _.Settings).Returns(_service1Settings.Object);
        _service2.Setup(_ => _.Settings).Returns(_service2Settings.Object);
        _disabledService.Setup(_ => _.Settings).Returns(_disabledServiceSettings.Object);

        _hub = new([_service1.Object, _service2.Object, _disabledService.Object]);
    }

    [Test]
    public async Task SendNotification_WillNotSendOnDisabledServices()
    {
        _service1
            .Setup(_ => _.SendNotification(It.IsAny<NotificationMessage>()))
            .ReturnsAsync(new Response());
        _service2
            .Setup(_ => _.SendNotification(It.IsAny<NotificationMessage>()))
            .ReturnsAsync(new Response());

        await _hub.SendNotification(new() { Message = "Message" });

        _disabledService
            .Verify(_ => _.SendNotification(It.IsAny<NotificationMessage>()), Times.Never);
    }

    [Test]
    public async Task SendNotification_WithAllSuccess_ReturnsSuccess()
    {
        _service1
            .Setup(_ => _.SendNotification(It.IsAny<NotificationMessage>()))
            .ReturnsAsync(new Response());
        _service2
            .Setup(_ => _.SendNotification(It.IsAny<NotificationMessage>()))
            .ReturnsAsync(new Response());

        var response = await _hub.SendNotification(new() { Message = "Message" });

        await Assert.That(response.Success).IsTrue();
    }

    [Test]
    public async Task SendNotification_WithErrors_ReturnsError()
    {
        var error1 = "error 1";
        var error2 = "error 2";
        _service1
            .Setup(_ => _.SendNotification(It.IsAny<NotificationMessage>()))
            .ReturnsAsync(new Response() { Errors = [error1] });
        _service2
            .Setup(_ => _.SendNotification(It.IsAny<NotificationMessage>()))
            .ReturnsAsync(new Response() { Errors = [error2] });

        var response = await _hub.SendNotification(new() { Message = "Message" });

        await Assert.That(response.Success).IsFalse();
        await Assert.That(response.Errors).Contains(error1);
        await Assert.That(response.Errors).Contains(error2);
    }

    [Test]
    public async Task SendNotification_WithWarnings_ReturnsSuccessWithWarning()
    {
        var warning1 = "warning 1";
        var warning2 = "warning 2";
        _service1
            .Setup(_ => _.SendNotification(It.IsAny<NotificationMessage>()))
            .ReturnsAsync(new Response() { Warnings = [warning1] });
        _service2
            .Setup(_ => _.SendNotification(It.IsAny<NotificationMessage>()))
            .ReturnsAsync(new Response() { Warnings = [warning2] });

        var response = await _hub.SendNotification(new() { Message = "Message" });

        await Assert.That(response.Success).IsTrue();
        await Assert.That(response.Warnings).Contains(warning1);
        await Assert.That(response.Warnings).Contains(warning2);
    }
}
