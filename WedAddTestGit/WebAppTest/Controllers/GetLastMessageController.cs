using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebAppTest.Models;
using Microsoft.Extensions.Configuration;

namespace WebAppTest.Controllers
{
    //показывает сообщения за последние 10 минут
    public class GetLastMessageController : Controller
    {
        private readonly ILogger<GetLastMessageController> _logger;
        private readonly IConfiguration _configuration;

        public GetLastMessageController(ILogger<GetLastMessageController> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {

            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            await using var dataSource = Npgsql.NpgsqlDataSource.Create(connectionString);

            // var sqlCommand = @"INSERT INTO messages(messageNum, messageText, dateTime) values ((@p1), (@p2), (@p3)) ";
            var sqlCommand = @"SELECT messageNum, messageText, dateTime FROM messages where dateTime >= (@p1)";

            var reslist = new List<MessagesModel>();

            //DateTime.Now.AddMinutes(-10);
            await using (var cmd = dataSource.CreateCommand(sqlCommand))
            {

                cmd.Parameters.AddWithValue("p1", NpgsqlTypes.NpgsqlDbType.Timestamp, DateTime.Now.AddMinutes(-10));

                await using (var res = await cmd.ExecuteReaderAsync())
                {
                    while (await res.ReadAsync())
                    {
                        reslist.Add(new MessagesModel
                        {
                            DateTime = res.GetDateTime(2),
                            Id = res.GetInt32(0),
                            Message = res.GetString(1)
                        });
                    }
                }


            }
            return View("Index", reslist);
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

    }
}