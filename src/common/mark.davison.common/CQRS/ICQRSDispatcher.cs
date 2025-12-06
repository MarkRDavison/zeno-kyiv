namespace mark.davison.common.CQRS;

public interface ICQRSDispatcher : ICommandDispatcher, IQueryDispatcher, IActionDispatcher
{

}
