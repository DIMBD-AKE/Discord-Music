using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.VoiceNext;
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
    
    /// <summary>
    /// 유저가 채널에 있나?
    /// </summary>
    public static async Task<bool> IsUserInChannel(this InteractionContext ctx)
    {
        var voiceChannel = ctx.Member?.VoiceState?.Channel;
        if (voiceChannel != null) 
            return true;
        
        await ctx.EditResponseAutoAsync(new DiscordWebhookBuilder().WithContent(Language.Get("not_user_in_channel")));
        
        return false;
    }
    
    /// <summary>
    /// 봇이 채널에 있나?
    /// </summary>
    public static async Task<bool> IsBotInChannel(this InteractionContext ctx)
    {
        var voiceNext = ctx.Client.GetVoiceNext();
        if (voiceNext.GetConnection(ctx.Guild) != null)
            return true;
        
        await ctx.EditResponseAutoAsync(new DiscordWebhookBuilder().WithContent(Language.Get("not_bot_in_channel")));
        
        return false;
    }
    
    /// <summary>
    /// 봇이랑 유저가 같은 채널인가?
    /// </summary>
    public static async Task<bool> IsSameChannel(this InteractionContext ctx)
    {
        if (!await ctx.IsUserInChannel())
            return false;

        if (!await ctx.IsBotInChannel())
            return false;
        
        var voiceNext = ctx.Client.GetVoiceNext();
        var vnc = voiceNext.GetConnection(ctx.Guild);

        if (vnc.TargetChannel.Id == ctx.Member?.VoiceState?.Channel?.Id) 
            return true;
        
        await ctx.EditResponseAutoAsync(new DiscordWebhookBuilder().WithContent(Language.Get("not_same_channel")));
        
        return false;
    }
}