using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using DSharpPlus.VoiceNext;

namespace Discord_Music;

class Program
{
    static async Task Main(string[] args)
    {
        var discord = new DiscordClient(new DiscordConfiguration
        {
            Token = Config.Get("TOKEN"),
            TokenType = TokenType.Bot,
            
        });

        discord.Ready += OnClientReady;

        discord.UseVoiceNext();

        // Slash Commands 설정
        var slash = discord.UseSlashCommands();
        slash.RegisterCommands<MusicSlashCommands>();
        
        // 임시 폴더 초기화
        if (Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Temp")))
            Directory.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Temp"), true);
        Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Temp"));

        await discord.ConnectAsync();
        await Task.Delay(-1);
    }

    private static Task OnClientReady(DiscordClient sender, ReadyEventArgs e)
    {
        Console.WriteLine("Bot is connected!");
        
        return Task.CompletedTask;
    }
}