namespace mark.davison.common.client.abstractions.CQRS;

public interface ICQRSDispatcher : ICommandDispatcher, IQueryDispatcher, IActionDispatcher
{

}
