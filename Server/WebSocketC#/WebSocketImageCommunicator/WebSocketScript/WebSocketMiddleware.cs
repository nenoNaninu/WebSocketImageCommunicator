using Microsoft.AspNetCore.Http;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WebSocketImageCommunicator.WebSocketScript
{
    public class WebSocketMiddleware
    {
        private readonly RequestDelegate _next;
        private WebSocketHandler _webSocketHandler { get; set; }

        private int _width;
        private int _height;

        public WebSocketMiddleware(RequestDelegate next, WebSocketHandler webSocketHandler)
        {
            _next = next;
            _webSocketHandler = webSocketHandler;
        }

        public async Task Invoke(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest) return;

            var socket = await context.WebSockets.AcceptWebSocketAsync();
            await _webSocketHandler.OnConnected(socket);

            await Receive(socket, async (result, buffer) =>
            {
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    await _webSocketHandler.ReceiveAsync(socket, result, buffer);
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _webSocketHandler.OnDisconnected(socket);
                }
                else if (result.MessageType == WebSocketMessageType.Binary)
                {
                    await _webSocketHandler.ReceiveAsync(socket, result, Encoding.UTF8.GetBytes("バイナリが飛んできたらしい"));
                }
            });
        }

        public async Task Receive(WebSocket webSocket, Action<WebSocketReceiveResult, byte[]> handleMessage)
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var buffer = new byte[1980 * 1080 * 4];
                var segment = new ArraySegment<byte>(buffer);

                var result = await webSocket.ReceiveAsync(segment, CancellationToken.None);

                int count = result.Count;

                while (!result.EndOfMessage)
                {
                    if (count >= buffer.Length)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.InvalidPayloadData,
                            "That's too long", CancellationToken.None);
                        return;
                    }

                    segment = new ArraySegment<byte>(buffer, count, buffer.Length - count);
                    result = await webSocket.ReceiveAsync(segment, CancellationToken.None);
                    count += result.Count;
                }

                //画像が来たらさらに後ろのサーバに吐き出させる。
                if (result.MessageType == WebSocketMessageType.Binary)
                {
                    var content = new ByteArrayContent(buffer, 0, count);
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    content.Headers.Add("width", _width.ToString());
                    content.Headers.Add("height", _height.ToString());
                    HttpClient client = new HttpClient();
                    await client.PostAsync("http://localhost:5000/detect/", content);
                }

                if (result.EndOfMessage && result.MessageType == WebSocketMessageType.Text)
                {
                    if (Encoding.UTF8.GetString(buffer, 0, result.Count) == "ImageMetaData")
                    {
                        buffer = new byte[1980 * 1080 * 4];
                        segment = new ArraySegment<byte>(buffer);

                        result = await webSocket.ReceiveAsync(segment, CancellationToken.None);

                        if (result.MessageType == WebSocketMessageType.Text && result.EndOfMessage)
                        {
                            var metadata =
                                JsonConvert.DeserializeObject<ImageData>(
                                    Encoding.UTF8.GetString(buffer, 0, result.Count));
                            _height = metadata.Height;
                            _width = metadata.Width;
                        }
                    }
                    else
                    {
                        handleMessage(result, buffer);
                    }
                }
            }
        }
    }
}
