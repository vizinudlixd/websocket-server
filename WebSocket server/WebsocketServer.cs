using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.WebSockets;
using System.Text;

namespace WebSocket_Server
{
    class WebsocketServer
    {
        private readonly HttpListener Listener;

        // JWT
        private readonly string SecretKey = "eyJhbGciOiJIUzI1NiJ9.eyJSb2xlIjoiQWRtaW4iLCJJc3N1ZXIiOiJBZG1pbiIsIlVzZXJuYW1lIjoiRXNwQ2xpZW50In0.JzR-spFYwcFWLr_oiBCVagx7BgjnbAsT71jeHcC_-XA";

        public WebsocketServer(string prefix)
        {
            this.Listener = new();
            this.Listener.Prefixes.Add(prefix);
        }


        private static async Task<string> ReceiveJwtToken(WebSocket webSocket)
        {
            byte[] buffer = new byte[1024];
            WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            // Decode the received message to retrieve the JWT token
            string jwtToken = Encoding.UTF8.GetString(buffer, 0, result.Count);
            return jwtToken;
        }


        /// <summary>
        /// HttpListener indítása és végtelen loop ami fogadja a klienseket
        /// </summary>
        public async Task Start()
        {
            try
            {
                Listener.Start();
                await Console.Out.WriteLineAsync("Server started.");

                while (true)
                {
                    HttpListenerContext context = await Listener.GetContextAsync();
                    if (context.Request.IsWebSocketRequest)
                    {
                        ProcessWebSocketRequest(context);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                PrintDetailedException(ex);
            }
        }


        /// <summary>
        /// Hitelesítés JSON Web Tokennel
        /// </summary>
        private async Task<bool> AuthenticateUser(HttpListenerContext context)
        {
            // Retrieve the JWT token from the request headers
            string authHeader = context.Request.Headers["Authorization"];
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                string token = authHeader.Substring("Bearer ".Length).Trim();

                // Validate the JWT token
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(SecretKey);
                try
                {
                    tokenHandler.ValidateToken(token, new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = false,
                        ValidateAudience = false
                    }, out _);

                    return true; // Sikeres auth
                }
                catch (Exception)
                {
                    return false; // Sikertelen auth
                }
            }

            return false; // Sikertelen auth ha null
        }


        /// <summary>
        /// Feldolgozza a kérelmet, hitelesíti a felhasználót és ha sikeres továbbítja az üzenetet
        /// </summary>
        private async void ProcessWebSocketRequest(HttpListenerContext context)
        {
            WebSocketContext webSocketContext = null;

            try
            {
                webSocketContext = await context.AcceptWebSocketAsync(subProtocol: null);
                Console.WriteLine("WebSocket connection established.");

                await EchoMessages(webSocketContext.WebSocket);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                context.Response.Close();
                PrintDetailedException(ex);
            }
            finally
            {
                if (webSocketContext != null)
                    webSocketContext.WebSocket.Dispose();
            }
        }

        /// <summary>
        /// Visszaküldi az üzenetet a küldőnek
        /// </summary>
        private async Task EchoMessages(WebSocket webSocket)
        {
            try
            {
                byte[] buffer = new byte[1024];

                while (webSocket.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        await Console.Out.WriteLineAsync($"Echoing '{Encoding.UTF8.GetString(buffer, 0, result.Count)}'");
                        await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await Console.Out.WriteLineAsync($"Closing Websocket (WebSocketMessageType.Close)");
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    }
                    else
                    {
                        await Console.Out.WriteLineAsync($"Else");
                    }
                }
            }
            catch (Exception ex)
            {
                PrintDetailedException(ex);
            }
        }

        /// <summary>
        /// Hibák részletes kiírása (Message, stacktrace, source)
        /// </summary>
        /// <param name="ex"></param>
        private async void PrintDetailedException(Exception ex) 
            => await Console.Out.WriteLineAsync($"Error occured:\nMessage{ex.Message}\nStacktrace: {ex.StackTrace}\nSource: {ex.Source}");
    }
}