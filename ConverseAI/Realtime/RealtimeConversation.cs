using System.Text;
using ConverseAI.Realtime.Chatbots;

namespace ConverseAI.Realtime;

public sealed class RealtimeConversation(string apiKey)
{
    private readonly StringBuilder _audioContent = new();
    private readonly StringBuilder _audioTranscript = new();
    private readonly RealtimeChatbot _detective = new DetectiveChatbot(apiKey);
    private readonly RealtimeChatbot _user = new UserChatbot(apiKey);

    public string OutputDirectory { get; } = Path.Combine("Conversations", DateTime.Now.ToString("yyyyMMddHHmmss"));
    
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        // set up callbacks:
        SetUpDetectiveChatbot();
        SetUpUserChatbot();
        
        // connect to the server:
        await _user.Start();
        await _detective.Start();
        
        // initiate the conversation:
        _detective.SaySomething();
    }

    public void Stop()
    {
        _detective.Dispose();
        _user.Dispose();

        Directory.CreateDirectory(OutputDirectory);
        FileUtilities.SaveAsTextFile(Path.Combine(OutputDirectory, "Transcript.txt"), _audioTranscript.ToString());
        FileUtilities.SaveAsWaveFile(Path.Combine(OutputDirectory, "Conversation.wav"), _audioContent.ToString());
    }

    private void SetUpDetectiveChatbot()
    {
        _detective.OnAudioReceived += audio =>
        {
            _audioContent.Append(audio);
            _user.SendAudioChunk(audio);
        };

        _detective.OnAudioTranscriptReceived += text =>
        {
            _audioTranscript.AppendLine("[DETECTIVE]");
            _audioTranscript.AppendLine(text + Environment.NewLine);
        };
        
        _detective.OnFinishedSpeaking += () => _user.CommitAudio();
    }

    private void SetUpUserChatbot()
    {
        _user.OnAudioReceived += audio =>
        {
            _audioContent.Append(audio);
            _detective.SendAudioChunk(audio);
        };

        _user.OnAudioTranscriptReceived += text =>
        {
            _audioTranscript.AppendLine("[USER]");
            _audioTranscript.AppendLine(text + Environment.NewLine);
        };
        
        _user.OnFinishedSpeaking += () => _detective.CommitAudio();
    }
}