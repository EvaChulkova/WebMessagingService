using WebAppTest.Models;

namespace WebAppTest
{
    public class EventOnReceiveNewMessageModel
    {
        public AsyncEvent<EventArgs> OnMessageReceive { get; set; }

        public async Task MessageReceive(MessagesModel messagesModel)
        {
            var eventTmp = OnMessageReceive;
            if (eventTmp != null)
            {
                await eventTmp.InvokeAsync(messagesModel, null);
            }
        }
      
    }
}