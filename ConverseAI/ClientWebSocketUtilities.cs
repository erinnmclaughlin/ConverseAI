using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace ConverseAI;

public static class ClientWebSocketUtilities
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);
    
    public static async Task<JsonElement> ReceiveNextMessageAsync(this ClientWebSocket ws, CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();
        var buffer = new byte[1024];

        while (true)
        {
            var result = await ws.ReceiveAsync(buffer, default);

            if (result.MessageType is WebSocketMessageType.Close)
            {
                return new JsonElement();
            }
            
            sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            
            if (result.EndOfMessage)
            {
                var message = sb.ToString();
                return JsonSerializer.Deserialize<JsonElement>(message, JsonSerializerOptions);
            }
        }
    }
    
    public static async Task SendAsync(this ClientWebSocket ws, object obj, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(obj, JsonSerializerOptions);
        var message = Encoding.UTF8.GetBytes(json);
        var segment = new ArraySegment<byte>(message);
        await ws.SendAsync(segment, WebSocketMessageType.Text, true, cancellationToken);
    }
}