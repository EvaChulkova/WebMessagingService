using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net.WebSockets;
using WebAppTest.Models;

namespace WebAppTest.Controllers
{
    //отображение сообщений в реальном времени
    public class OnlineMessageController : Controller
    {
        private readonly ILogger<OnlineMessageController> _logger;
        private readonly EventOnReceiveNewMessageModel _eventOnReceiveNewMessageModel;

        public OnlineMessageController(ILogger<OnlineMessageController> logger, 
            EventOnReceiveNewMessageModel eventOnReceiveNewMessageModel)
        {
            _logger = logger;
            _eventOnReceiveNewMessageModel = eventOnReceiveNewMessageModel;
            _eventOnReceiveNewMessageModel.OnMessageReceive += MessageRecive;
        }

        [HttpGet("/ws")]
        public async Task Get()
        {
            _logger.LogInformation("begin connect websocket");
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                await Echo(webSocket,CancellationToken.None);
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }

        private MessagesModel? _message;
        private object _lockObj=new object();

        private Task MessageRecive(object message, EventArgs args)
        {
           
            lock (_lockObj)
            {
                _message = (MessagesModel)message;
            }

            _logger.LogInformation("Receive message");
            return Task.CompletedTask;
        }

        private async Task Echo(WebSocket webSocket,CancellationToken token)
        {
            byte[] buffer;
            while (!token.IsCancellationRequested)
            {
                if (_message != null)
                {
                    //получить последнее сообщение
                    lock (_lockObj)
                    {
                        buffer = System.Text.Encoding.UTF8.GetBytes(DateTime.Now.ToString("G") + _message.Id + " " + _message.Message);
                        //отправка сообщения
                        _message = null;
                    }
                    _logger.LogInformation("Send message to wedsocket");
                    await webSocket.SendAsync(
                        new ArraySegment<byte>(buffer, 0, buffer.Length), WebSocketMessageType.Text, true
                        ,
                        CancellationToken.None);
                }
            }
            _logger.LogInformation("Close connection to wedsocket");
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure,"",
                CancellationToken.None);
        }


        public IActionResult Index()
        {
            return View();
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

    }
}