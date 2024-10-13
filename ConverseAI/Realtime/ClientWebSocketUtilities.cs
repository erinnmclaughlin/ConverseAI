using System.Text;
using System.Text.Json;
using Websocket.Client;

namespace ConverseAI.Realtime;

public static class ClientWebSocketUtilities
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);
    
    public static JsonElement ReadFromJson(this ResponseMessage responseMessage)
    {
        return JsonSerializer.Deserialize<JsonElement>(responseMessage.Text ?? "{}", JsonSerializerOptions);
    }
    
    public static void SendMessage(this WebsocketClient ws, object message)
    {
        var json = JsonSerializer.Serialize(message, JsonSerializerOptions);
        var segment = new ArraySegment<byte>(Encoding.UTF8.GetBytes(json));
        ws.SendAsText(segment);
    }
}