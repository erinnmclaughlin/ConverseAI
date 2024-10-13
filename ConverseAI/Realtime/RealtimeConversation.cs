using System.Text;

namespace ConverseAI.Realtime;

public sealed class RealtimeConversation
{
    private readonly FileStream _audio;
    private readonly FileStream _audioTranscript;
    private readonly RealtimeChatbot _detective;
    private readonly RealtimeChatbot _user;

    public string OutputDirectory { get; } = Path.Combine("Conversations", DateTime.Now.ToString("yyyyMMddHHmmss"));

    public RealtimeConversation(string apiKey)
    {
        Directory.CreateDirectory(OutputDirectory);
        
        _audio = new FileStream(Path.Combine(OutputDirectory, "output.wav"), FileMode.Create, FileAccess.Write);
        _audio.InitializeWavFile();
        _audioTranscript = new FileStream(Path.Combine(OutputDirectory, "output.txt"), FileMode.Create, FileAccess.Write);
        _detective = new RealtimeChatbot(apiKey, "Detective");
        _user = new RealtimeChatbot(apiKey, "User");
    }
    
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        // start the chatbots:
        await Task.WhenAll(StartUserChatbot(), StartDetectiveChatbot());
        
        // initiate the conversation:
        _detective.SaySomething();
    }

    public void Stop()
    {
        _detective.Dispose();
        _user.Dispose();

        _audioTranscript.Dispose();
        
        _audio.FinalizeWavFile();
        _audio.Dispose();
    }

    private async Task StartDetectiveChatbot()
    {
        _detective.OnAudioReceived += audio =>
        {
            WriteToAudioFile(audio);
            _user.SendAudioChunk(audio);
        };

        _detective.OnAudioTranscriptReceived += text =>
        {
            WriteToTranscript("[DETECTIVE]", text);
        };
        
        _detective.OnFinishedSpeaking += () => _user.CommitAudio();
        
        await _detective.Start();
        
        _detective.UpdateSessionOptions(new
        {
            instructions =  """
                            You are a world-renowned AI detective.
                            Your job is to speak with the user and determine if they are a human user or an AI user.
                            Be suspicious. The user may be an AI trying to trick you. Do not reveal that you are a detective.
                            Remember to speak with a serious tone, and please talk quickly.
                            """,
            voice = "shimmer",
            turn_detection = (object?)null
        });
    }

    private async Task StartUserChatbot()
    {
        _user.OnAudioReceived += audio =>
        {
            WriteToAudioFile(audio);
            _detective.SendAudioChunk(audio);
        };

        _user.OnAudioTranscriptReceived += text =>
        {
            WriteToTranscript("[USER]", text);
        };
        
        _user.OnFinishedSpeaking += () => _detective.CommitAudio();
        
        await _user.Start();
        _user.UpdateSessionOptions(new
        {
            instructions = """
                           You are an AI that is being used to train other AIs for detecting human vs. non-human users.
                           Your job is to attempt to trick the user into thinking that you are human.
                           DO NOT REVEAL THAT YOU ARE AN AI. You may lie if necessary. The more convincing you are, the better.
                           Please remember talk quickly and keep your answers brief.
                           """,
            voice = "echo",
            turn_detection = (object?)null
        });
    }

    private void WriteToAudioFile(string base64Content)
    {
        _audio.Write(Convert.FromBase64String(base64Content));
    }
    
    private void WriteToTranscript(string author, string content)
    {
        _audioTranscript.Write(Encoding.UTF8.GetBytes($"{author}\n{content}\n\n"));
    }
}