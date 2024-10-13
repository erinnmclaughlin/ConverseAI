namespace ConverseAI.Realtime.Chatbots;

public class UserChatbot(string apiKey) : RealtimeChatbot(apiKey, "User")
{
    public override async Task Start()
    {
        await base.Start();
        UpdateSessionOptions(new
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
}