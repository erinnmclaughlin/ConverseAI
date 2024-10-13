using ConverseAI.Realtime;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
var apiKey = configuration["ApiKey"] ?? throw new Exception("Missing OpenAI API key");

var conversation = new RealtimeConversation(apiKey);

Console.WriteLine("Starting conversation...");
await conversation.StartAsync();

Console.WriteLine("Waiting 15 seconds...");
await Task.Delay(TimeSpan.FromSeconds(15));
conversation.Stop();

Console.WriteLine($"Conversation finished! Output files can be found at {conversation.OutputDirectory}");
