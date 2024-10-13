using System.Text;
using ConverseAI;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder().AddUserSecrets(typeof(Program).Assembly).Build();
var apiKey = configuration["ApiKey"] ?? throw new Exception("Missing OpenAI API key.");

using var fakeUser = await CreateUserChatbot(apiKey);
using var detective = await CreateDetectiveChatbot(apiKey);

var dirName = DateTime.Now.ToString("yyyyMMddHHmmss");
Directory.CreateDirectory(dirName);

var speaker = detective;
var listener = fakeUser;

const int maxLoops = 3;
var count = 0;

var audio = new StringBuilder();

while (true)
{
    var isFinalRound = speaker == fakeUser && count++ == maxLoops;
    
    await speaker.SaySomething();
    
    await foreach (var chunk in speaker.EnumerateResponseChunks())
    {
        audio.Append(chunk);
    }

    var fileName = Path.Combine(dirName, DateTime.Now.ToString("HHmmss") + $"_{speaker.Name}.wav");
    var audioContent = audio.ToString();
    AudioHelper.SaveAsWaveFile(fileName, audioContent);
    audio.Clear();
    
    if (isFinalRound)
        break;
    
    await listener.SendAudio(audioContent);

    if (speaker == detective)
    {
        speaker = fakeUser;
        listener = detective;
    }
    else
    {
        listener = fakeUser;
        speaker = detective;
    }
}

await detective.SendInstruction("Thank you for speaking with the user. Please now state whether you believe the user is a real human user or an AI pretending to be human. Respond using audio and explain your reasoning.");
await detective.SaySomething();
await foreach (var chunk in detective.EnumerateResponseChunks())
{
    audio.Append(chunk);
}

AudioHelper.SaveAsWaveFile(Path.Combine(dirName, "Conclusion.wav"), audio.ToString());
return;

static async Task<AiChatbot> CreateDetectiveChatbot(string apiKey)
{
    var detective = new AiChatbot(apiKey, "Detective", ConsoleColor.DarkMagenta);
    await detective.ConnectAsync();
    await detective.UpdateSessionOptions(new
    {
        instructions =  """
                        You are a world-renowned AI detective.
                        Your job is to speak with the user and determine if they are a human user or an AI user.
                        Be suspicious. The user may be an AI trying to trick you.
                        Please talk quickly.
                        """,
        turn_detection = (object?)null,
        voice = "shimmer"
    });

    return detective;
}

static async Task<AiChatbot> CreateUserChatbot(string apiKey)
{
    var fakeUser = new AiChatbot(apiKey, "User", ConsoleColor.DarkCyan);
    await fakeUser.ConnectAsync();
    await fakeUser.UpdateSessionOptions(new
    {
        instructions = """
                       Congratulations! You are a human that has been selected to speak with an AI.
                       Feel free the ask the AI anything you want! Remember: you are human.
                       Please talk quickly.
                       """,
        turn_detection = (object?)null,
        voice = "echo"
    });
    
    return fakeUser;
}
