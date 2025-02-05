﻿using System.Diagnostics;
using System.Text;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.VoiceNext;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace Discord_Music;

public class MusicSlashCommands : ApplicationCommandModule
{
    private readonly YoutubeClient _youtube = new YoutubeClient();
    
    [SlashCommand("재생", "유튜브에서 노래를 찾아 재생합니다.")]
    public async Task PlayCommand(InteractionContext ctx, [Option("노래", "링크 or 이름 찾기")] string content)
    {
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

        if (!await ctx.IsUserInChannel())
            return;
        
        // YouTube 비디오 정보 가져오기
        var video = await GetVideo(content);
        var streamManifest = await _youtube.Videos.Streams.GetManifestAsync(video.Id);
        var audioStreamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
        
        // 임시로 저장해둠
        var tempStreamFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Temp/{Guid.NewGuid()}");

        await _youtube.Videos.Streams.DownloadAsync(audioStreamInfo, tempStreamFile);
        
        // 음성 채널 연결
        var voiceNext = ctx.Client.GetVoiceNext();
        var vnc = voiceNext.GetConnection(ctx.Guild);

        // 이미 연결되어 있는지 확인
        if (vnc == null)
        {
            var voiceChannel = ctx.Member?.VoiceState?.Channel;
            vnc = await voiceNext.ConnectAsync(voiceChannel);
        }
        else if (!await ctx.IsSameChannel())
        {
            return;
        }
        
        await ctx.EditResponseAutoAsync(new DiscordWebhookBuilder().WithContent(Language.Get("play", video.Title)));
        
        await MusicController.Instance.Play(ctx.Guild.Id, vnc, video.Title, tempStreamFile);
    }
    
    [SlashCommand("스킵", "재생중인 노래를 넘깁니다.")]
    public async Task SkipCommand(InteractionContext ctx)
    {
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

        if (!await ctx.IsSameChannel())
            return;
        
        var skipResult = await MusicController.Instance.Skip(ctx.Guild.Id);

        if (skipResult.IsSuccess)
        {
            var musicName = skipResult.Value.MusicName;
            await ctx.EditResponseAutoAsync(new DiscordWebhookBuilder().WithContent(Language.Get("skip", musicName)));
        }
        else
        {
            await ctx.EditResponseAutoAsync(new DiscordWebhookBuilder().WithContent(skipResult.ErrorMessage()));
        }
    }
    
    [SlashCommand("대기열", "대기중인 노래 목록")]
    public async Task QueueCommand(InteractionContext ctx)
    {
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

        if (!await ctx.IsBotInChannel())
            return;

        var queueResult = MusicController.Instance.GetQueue(ctx.Guild.Id);

        if (!queueResult.IsSuccess)
        {
            await ctx.EditResponseAutoAsync(new DiscordWebhookBuilder().WithContent(queueResult.ErrorMessage()));
            return;
        }
        
        var embed = new DiscordEmbedBuilder()
            .WithTitle(Language.Get("queue"))
            .WithColor(DiscordColor.Azure);

        var descriptionBuilder = new StringBuilder();
        for (var i = 0; i < Math.Min(queueResult.Value.Count, 10); i++)
        {
            if (i == 0)
            {
                descriptionBuilder.AppendLine(Language.Get("queue_now", queueResult.Value[i]));
                descriptionBuilder.AppendLine();
            }
            else
                descriptionBuilder.AppendLine($":number_{i}: {queueResult.Value[i]}");
        }

        embed.WithDescription(descriptionBuilder.ToString());

        await ctx.EditResponseAutoAsync(new DiscordWebhookBuilder().AddEmbed(embed), 10000);
    }
    
    [SlashCommand("납치", "채널을 이동합니다.")]
    public async Task TakeCommand(InteractionContext ctx)
    {
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

        if (!await ctx.IsUserInChannel())
            return;

        if (!await ctx.IsBotInChannel())
            return;
        
        var channel = await MusicController.Instance.ChangeChannel(ctx.Guild.Id, ctx);
        
        await ctx.EditResponseAutoAsync(new DiscordWebhookBuilder().WithContent(Language.Get("bot_take", channel.Name)));
    }

    [SlashCommand("대말", "대신 말해드립니다.")]
    public async Task SayCommand(InteractionContext ctx, [Option("할말", "내용")] string content)
    {
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(true));

        await ctx.EditResponseAutoAsync(new DiscordWebhookBuilder().WithContent(Language.Get("say")));
        
        await ctx.Channel.SendMessageAsync(content);
    }

    async Task<Video> GetVideo(string content)
    {
        if (content.Contains("https"))
            return await _youtube.Videos.GetAsync(content);

        await foreach (var result in _youtube.Search.GetVideosAsync(content))
            return await _youtube.Videos.GetAsync(result.Url);

        return null;
    }
}