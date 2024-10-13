using System.Net.WebSockets;
using Websocket.Client;

namespace ConverseAI.Realtime.Chatbots;

public abstract class RealtimeChatbot : IDisposable
{
    private readonly WebsocketClient _ws;
    private readonly List<IDisposable> _subscriptions = [];
    
    public Action<string> OnAudioReceived { get; set; } = _ => { };
    public Action<string> OnAudioTranscriptReceived { get; set; } = _ => { };
    public Action OnFinishedSpeaking { get; set; } = () => { };
    
    protected RealtimeChatbot(string apiKey, string name)
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

           if (type is "response.audio.delta")
           {
               var audio = json.GetProperty("delta").GetString()!;
               OnAudioReceived.Invoke(audio);
           }
           else if (type is "response.audio.done")
           {
               OnFinishedSpeaking();
           }
           else if (type is "response.audio_transcript.done")
           {
               var transcript = json.GetProperty("transcript").GetString()!;
               OnAudioTranscriptReceived.Invoke(transcript);
           }
       }));
    }

    public void Dispose()
    {
        foreach (var subscription in _subscriptions)
            subscription.Dispose();
        
        _ws.Dispose();
        GC.SuppressFinalize(this);
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

    public virtual async Task Start()
    {
        if (!_ws.IsStarted)
            await _ws.Start();
    }

    protected void UpdateSessionOptions(object options)
    {
        _ws.SendMessage(new { type = "session.update", session = options });
    }
}