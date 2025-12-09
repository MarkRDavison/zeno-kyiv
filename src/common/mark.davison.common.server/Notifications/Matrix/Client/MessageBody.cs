namespace mark.davison.common.server.Notifications.Matrix.Client;

public class MessageBody
{
    public MessageBody(TextMessageBody textMessageBody)
    {
        Msgtype = "m.text";
        Body = textMessageBody.Body;
    }

    public string Msgtype { get; }
    public string? Body { get; }
}