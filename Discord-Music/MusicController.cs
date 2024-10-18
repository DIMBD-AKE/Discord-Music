using System.Diagnostics;
using System.Runtime.InteropServices;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.VoiceNext;
using FluentResults;

namespace Discord_Music;

public class MusicController
{
    public class MusicPlayData
    {
        public MusicPlayData(string path, string musicName)
        {
            Path = path;
            MusicName = musicName;
        }

        public string Path { get; set; }
        public string MusicName { get; set; }
    }

    class GuildMusic
    {
        public ulong GuildId { get; set; }
        public Queue<MusicPlayData> MusicQueue { get; set; } = new();
        public CancellationTokenSource CancellationToken { get; set; } = null;
        public VoiceNextConnection Connection { get; set; }
        public MusicPlayData CurrentPlay { get; set; }
        
        public bool IsPlaying => CancellationToken != null && CurrentPlay != null;
    }
    
    private static MusicController _instance;
    public static MusicController Instance => _instance ??= new MusicController();
    
    Dictionary<ulong, GuildMusic> _guildMusic = new();
    
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    
    public async Task<Result<MusicPlayData>> Skip(ulong guildId)
    {
        if (!_guildMusic.ContainsKey(guildId) || !_guildMusic[guildId].IsPlaying)
            return Result.Fail(Language.Get("not_playing_music"));
        
        await _guildMusic[guildId].CancellationToken.CancelAsync();

        await _guildMusic[guildId].Connection.GetTransmitSink().FlushAsync();

        var temp = _guildMusic[guildId].CurrentPlay;
        _guildMusic[guildId].CurrentPlay = null;
        _guildMusic[guildId].CancellationToken = null;
        
        return Result.Ok(temp);
    }

    public async Task Play(ulong guildId, VoiceNextConnection vnc, string audioName, string path)
    {
        _guildMusic.TryAdd(guildId, new GuildMusic()
        {
            GuildId = guildId, 
            MusicQueue = new(),
            Connection = vnc,
        });
        
        _guildMusic[guildId].MusicQueue.Enqueue(new MusicPlayData(path, audioName));

        if (!_guildMusic[guildId].IsPlaying)
            await ContinuouslyPlay(guildId);
    }

    async Task ContinuouslyPlay(ulong guildId)
    {
        if (_guildMusic[guildId].MusicQueue.Count > 0)
        {
            var data = _guildMusic[guildId].MusicQueue.Dequeue();
            _guildMusic[guildId].CurrentPlay = data;

            var token = new CancellationTokenSource();
            _guildMusic[guildId].CancellationToken = token;

            try
            {
                await PlayAudio(guildId, data.Path, token);
            }
            finally
            {
                await ContinuouslyPlay(guildId);
            }
        }
        else
        {
            _guildMusic[guildId].CancellationToken = null;
            _guildMusic[guildId].CurrentPlay = null;
        }
    }
    
    async Task PlayAudio(ulong guildId, string path, CancellationTokenSource cts)
    {
        var process = new Process();

        try
        {
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.Start();

            var buffer = new byte[1024];
            int read;

            using (var ffmpegOutput = process.StandardOutput.BaseStream)
            {
                while ((read = await ffmpegOutput.ReadAsync(buffer, 0, buffer.Length)) > 0 &&
                       !cts.IsCancellationRequested)
                {
                    await _semaphore.WaitAsync();
                    
                    var stream = _guildMusic[guildId].Connection.GetTransmitSink();
                    await stream.WriteAsync(buffer, 0, read, cts.Token);

                    _semaphore.Release();
                }
            }
        }
        finally
        {
            if (_semaphore.CurrentCount == 0)
                _semaphore.Release();

            await process.WaitForExitAsync();
            
            process.Kill();

            process.Dispose();
        }
    }

    public async Task<DiscordChannel> ChangeChannel(ulong guildId, InteractionContext ctx)
    {
        await _semaphore.WaitAsync();

        if (_guildMusic[guildId].Connection != null)
        {
            await _guildMusic[guildId].Connection.GetTransmitSink().FlushAsync();
            _guildMusic[guildId].Connection.Disconnect();
        }
        
        var voiceChannel = ctx.Member?.VoiceState?.Channel;
        
        var voiceNext = ctx.Client.GetVoiceNext();
        
        _guildMusic[guildId].Connection = await voiceNext.ConnectAsync(voiceChannel);

        _semaphore.Release();
        
        return voiceChannel;
    }

    public Result<List<string>> GetQueue(ulong guildId)
    {
        if (!_guildMusic.ContainsKey(guildId) || !_guildMusic[guildId].IsPlaying)
            return Result.Fail(Language.Get("not_playing_music"));

        var queue = new List<string>();
        queue.Add(_guildMusic[guildId].CurrentPlay.MusicName);
        
        foreach (var play in _guildMusic[guildId].MusicQueue)
            queue.Add(play.MusicName);

        return Result.Ok(queue);
    }
}