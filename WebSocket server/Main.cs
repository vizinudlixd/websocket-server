using System;
using static WebSocket_Server.WebsocketServer;

namespace WebSocket_Server
{
    class Start
    {
        static async Task Main(string[] args)
        {
            string url = "http://localhost:6969/";
            WebsocketServer server = new (url);

            try
            {
                await server.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Server error: {ex.Message}");
            }
        }
    }
}