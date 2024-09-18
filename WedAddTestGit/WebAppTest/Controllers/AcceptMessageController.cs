using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebAppTest.Models;
using Microsoft.Extensions.Configuration;

namespace WebAppTest.Controllers
{
    //принимает сообщения от клиента
    public class AcceptMessageController : Controller
    {
        private readonly ILogger<AcceptMessageController> _logger;
        private readonly EventOnReceiveNewMessageModel _eventOnReceiveNewMessageModel;
        private readonly IConfiguration _configuration;

        public AcceptMessageController(ILogger<AcceptMessageController> logger, 
            EventOnReceiveNewMessageModel eventOnReceiveNewMessageModel,
            IConfiguration configuration)
        {
            _logger = logger;
            _eventOnReceiveNewMessageModel = eventOnReceiveNewMessageModel;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> Send(MessagesModel messagesModel)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            
            await using var dataSource = Npgsql.NpgsqlDataSource.Create(connectionString);

            var sqlCommand = @"INSERT INTO messages(messageNum, messageText, dateTime) values ((@p1), (@p2), (@p3)) ";

            //проверка на 128 символов в сообщении
            if (messagesModel.Message.Length > 128)
            {
                messagesModel.Message = messagesModel.Message.Substring(0, 128);
                _logger.LogInformation("Too much symbols. Cut.");
            }
            
            

            _logger.LogInformation("Save message to database");
            await using (var cmd = dataSource.CreateCommand(sqlCommand))
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlTypes.NpgsqlDbType.Numeric, messagesModel.Id);
                cmd.Parameters.AddWithValue("p2", NpgsqlTypes.NpgsqlDbType.Text, messagesModel.Message);
                cmd.Parameters.AddWithValue("p3", NpgsqlTypes.NpgsqlDbType.Timestamp, DateTime.Now);
                await cmd.ExecuteNonQueryAsync();
            }

            _logger.LogInformation("Send event about message receive");
            _eventOnReceiveNewMessageModel?.MessageReceive(messagesModel);
            return View("Index");
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