using System.Net.WebSockets;
using Websocket.Client;

namespace ConverseAI.Realtime;

public sealed class RealtimeChatbot : IDisposable
{
    private readonly WebsocketClient _ws;
    private readonly List<IDisposable> _subscriptions = [];
    
    public Action<string> OnAudioReceived { get; set; } = _ => { };
    public Action<string> OnAudioTranscriptReceived { get; set; } = _ => { };
    public Action OnFinishedSpeaking { get; set; } = () => { };
    
    public RealtimeChatbot(string apiKey, string name)
    {
        _ws = new WebsocketClient(new Uri("wss://api.openai.com/v1/realtime?model=gpt-4o-realtime-preview-2024-10-01"), () =>
        {
            var client = new ClientWebSocket();
            client.Options.SetRequestHeader("Authorization", $"Bearer {apiKey}");
            client.Options.SetRequestHeader("OpenAI-Beta", "realtime=v1");
            return client;
        });
        
        _ws.Name = name;
        
       _subscriptions.Add(_ws.MessageReceived.Subscribe(message =>
       {
           var json = message.ReadFromJson();
           var type = json.GetProperty("type").GetString();

           switch (type)
           {
               case "response.audio.delta":
                   OnAudioReceived.Invoke(json.GetProperty("delta").GetString()!);
                   break;
               case "response.audio.done":
                   OnFinishedSpeaking();
                   break;
               case "response.audio_transcript.done":
                   OnAudioTranscriptReceived.Invoke(json.GetProperty("transcript").GetString()!);
                   break;
           }
       }));
    }

    public void Dispose()
    {
        _subscriptions.ForEach(s => s.Dispose());
        _ws.Dispose();
    }

    public void CommitAudio()
    {
        _ws.SendMessage(new { type = "input_audio_buffer.commit" });
        SaySomething();
    }
    
    public void SaySomething()
    {
        _ws.SendMessage(new { type = "response.create", response = new { } });
    }
    
    public void SendAudioChunk(string base64EncodedAudio)
    {
        _ws.SendMessage(new { type = "input_audio_buffer.append", audio = base64EncodedAudio });
    }

    public async Task Start()
    {
        await _ws.Start();
    }

    public void UpdateSessionOptions(object options)
    {
        _ws.SendMessage(new { type = "session.update", session = options });
    }
}