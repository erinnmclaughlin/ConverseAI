namespace ConverseAI.Realtime.Chatbots;

public class DetectiveChatbot(string apiKey) : RealtimeChatbot(apiKey, "Detective")
{
    public override async Task Start()
    {
        await base.Start();
        UpdateSessionOptions(new
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
}