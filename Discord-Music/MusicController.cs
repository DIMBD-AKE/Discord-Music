using System.Diagnostics;
using DSharpPlus.Entities;
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
    
    public async Task<Result<MusicPlayData>> Skip(ulong guildId)
    {
        if (!_guildMusic.ContainsKey(guildId) || !_guildMusic[guildId].IsPlaying)
            return Result.Fail(Language.Get("not_playing_music"));
        
        await _guildMusic[guildId].CancellationToken.CancelAsync();

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

            await PlayAudio(guildId, data.Path, token);
            await ContinuouslyPlay(guildId);
        }
        else
        {
            _guildMusic[guildId].CancellationToken = null;
            _guildMusic[guildId].CurrentPlay = null;
        }
    }
    
    async Task PlayAudio(ulong guildId, string path, CancellationTokenSource cts)
    {
        var ffmpegProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i {path} -ac 2 -f s16le -ar 48000 pipe:1",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                RedirectStandardInput = true,
                CreateNoWindow = true
            }
        };
        
        ffmpegProcess.Start();

        var transmitStream = _guildMusic[guildId].Connection.GetTransmitSink();

        try
        {
            await ffmpegProcess.StandardOutput.BaseStream.CopyToAsync(transmitStream, cancellationToken: cts.Token);
        }
        finally
        {
            await transmitStream.FlushAsync();

            ffmpegProcess.Kill();
            
            ffmpegProcess.Dispose();
            
            Thread.Sleep(1);
            
            File.Delete(path);
        }
    }
}