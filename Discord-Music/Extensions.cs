using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using FluentResults;

namespace Discord_Music;

public static class Extensions
{
    public static string? ErrorMessage(this Result result)
    {
        return result.Errors.FirstOrDefault()?.Message;
    }
    
    public static string? ErrorMessage<T>(this Result<T> result)
    {
        return result.Errors.FirstOrDefault()?.Message;
    }

    public static async Task<DiscordMessage> EditResponseAutoAsync(this InteractionContext ctx, DiscordWebhookBuilder builder)
    {
        var message = await ctx.EditResponseAsync(builder);

        Task.Run(async () =>
        {
            await Task.Delay(3000);
            
            await message.DeleteAsync();
        });
        
        return message;
    }
}