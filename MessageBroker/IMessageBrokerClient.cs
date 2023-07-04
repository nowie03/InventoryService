using InventoryService.Models;

namespace InventoryService.MessageBroker
{
    public interface IMessageBrokerClient
    {
        public void SendMessage(Message message);

        public ulong GetNextSequenceNumber();


    }
}
