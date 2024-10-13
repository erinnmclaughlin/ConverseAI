using System.Net.WebSockets;

namespace ConverseAI;

public sealed class AiChatbot : IDisposable
{
    private readonly ClientWebSocket _ws = new();

    public string? LastItemId { get; private set; }
    
    public ConsoleColor Color { get; }
    public string Name { get; }
    
    public AiChatbot(string apiKey, string name, ConsoleColor? color = null)
    {
        Color = color ?? Console.ForegroundColor;
        Name = name;
        
        _ws.Options.SetRequestHeader("Authorization", $"Bearer {apiKey}");
        _ws.Options.SetRequestHeader("OpenAI-Beta", "realtime=v1");
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        await _ws.ConnectAsync(new Uri("wss://api.openai.com/v1/realtime?model=gpt-4o-realtime-preview-2024-10-01"), cancellationToken);
    }

    public void Dispose()
    {
        _ws.Dispose();
    }

    public async Task SaySomething(CancellationToken cancellationToken = default)
    {
        await _ws.SendAsync(new { type = "response.create", response = new { } }, cancellationToken);
    }

    public async Task SendInstruction(string instruction)
    {
        await _ws.SendAsync(new
        {
            type = "conversation.item.create",
            previous_item_id = LastItemId,
            item = new
            {
                type = "message",
                status = "completed",
                role = "system",
                content = new object[]
                {
                    new
                    {
                        type = "input_text",
                        text = instruction
                    }
                }
            }
        });
    }
    
    public async Task UpdateSessionOptions(object options)
    {
        await _ws.SendAsync(new { type = "session.update", session = options });
    }
    
    public async Task SendAudio(string base64EncodedAudio)
    {
        await _ws.SendAsync(new
        {
            type = "conversation.item.create",
            item = new
            {
                type = "message",
                role = "user",
                content = new object[]
                {
                    new { type = "input_audio", audio = base64EncodedAudio }
                }
            }
        });
    }
    
    public async Task SendAudioChunk(string base64EncodedAudio)
    {
        await _ws.SendAsync(new { type = "input_audio_buffer.append", audio = base64EncodedAudio });
    }

    public async Task CommitAudio()
    {
        await _ws.SendAsync(new { type = "input_audio_buffer.commit" });
    }
    
    public async IAsyncEnumerable<string> EnumerateResponseChunks()
    {
        while (true)
        {
            var message = await _ws.ReceiveNextMessageAsync();

            var type = message.GetProperty("type").GetString() ?? "";

            if (type is "error")
            {
                var errorMessage = message.GetProperty("error").GetProperty("message").GetString();
                
                PrintNameLabel();
                PrintErrorMessage(errorMessage);
                
                yield break;
            }

            if (type is "conversation.item.created")
            {
                LastItemId = message.GetProperty("item").GetProperty("id").GetString();
            }

            if (type is "response.audio.delta")
            {
                yield return message.GetProperty("delta").GetString() ?? "";
            }
            else if (type is "response.audio.done")
            {
                yield break;
            }
            else if (type is "response.audio_transcript.done")
            {
                Console.ForegroundColor = Color;
                Console.WriteLine();
                Console.WriteLine(Name);
                Console.ResetColor();
                Console.WriteLine(message.GetProperty("transcript").GetString());
            }
            else if (type is "response.done")
            {
                var response = message.GetProperty("response");
                
                if (response.GetProperty("status").GetString() is "failed")
                {
                    var error = response.GetProperty("status_details").GetProperty("error");
                    var errorMessage = error.GetProperty("message").GetString();
                    PrintNameLabel();
                    PrintErrorMessage(errorMessage);
                }
            }
        }
    }

    private void PrintErrorMessage(string? errorMessage)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(errorMessage);
        Console.ResetColor();
    }

    private void PrintNameLabel()
    {
        Console.ForegroundColor = Color;
        Console.WriteLine(Name);
        Console.ResetColor();
    }
}