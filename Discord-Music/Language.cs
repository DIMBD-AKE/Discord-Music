using DSharpPlus.Entities;
using Newtonsoft.Json;

namespace Discord_Music;

public class Language
{
    private static Language _instance;
    public static Language Instance => _instance ??= new Language();
    
    private Dictionary<string, string> _translations;

    public Language()
    {
        _translations = new Dictionary<string, string>();
        
        LoadLanguageFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Languages/ko-KR.json"));
    }

    // JSON 파일에서 언어 데이터를 로드
    void LoadLanguageFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Language file not found: {filePath}");
        }

        // JSON 파일을 읽고 Dictionary로 파싱
        var jsonContent = File.ReadAllText(filePath);
        _translations = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent);
    }

    public static string Get(string key)
    {
        if (!Instance._translations.ContainsKey(key))
            return $"Missing translation for key: {key}";

        return Instance._translations[key];
    }
    
    public static string Get(string key, params string[] args)
    {
        if (!Instance._translations.ContainsKey(key))
            return $"Missing translation for key: {key}";

        var content = Instance._translations[key];
        
        for (var i = 1; i <= args.Length; i++)
            content = content.Replace($"@{i}", args[i - 1]);

        return content;
    }

    public static DiscordEmbed BuildLuckEmbed()
    {
        int GetLuckMessageIndex()
        {
            var today = DateTime.Today;
            var seed = today.Year * 10000 + today.Month * 100 + today.Day;
            var rnd = new Random(seed);
            var chance = rnd.Next(0, 101);

            if (chance <= 3)
                return 0;  // 0%
            else if (chance <= 7)
                return 1;  // 10%
            else if (chance <= 12)
                return 2;  // 20%
            else if (chance <= 22)
                return 3;  // 30%
            else if (chance <= 37)
                return 4;  // 40%
            else if (chance <= 57)
                return 5;  // 50%
            else if (chance <= 72)
                return 6;  // 60%
            else if (chance <= 82)
                return 7;  // 70%
            else if (chance <= 89)
                return 8;  // 80%
            else if (chance <= 95)
                return 9;  // 90%
            else
                return 10; // 100%
        }
        
        var luckGauges = new string[]
        {
            "0% [□□□□□□□□□□]",  // 0%
            "10% [■□□□□□□□□□]",  // 10%
            "20% [■■□□□□□□□□]",  // 20%
            "30% [■■■□□□□□□□]",  // 30%
            "40% [■■■■□□□□□□]",  // 40%
            "50% [■■■■■□□□□□]",  // 50%
            "60% [■■■■■■□□□□]",  // 60%
            "70% [■■■■■■■□□□]",  // 70%
            "80% [■■■■■■■■□□]",  // 80%
            "90% [■■■■■■■■■□]",  // 90%
            "100% [■■■■■■■■■■]"   // 100%
        };

        var index = GetLuckMessageIndex();

        var embed = new DiscordEmbedBuilder
        {
            Title = $"🌟 오늘의 표게이지 🌟",
            Description = luckGauges[index], // 게이지만 표시
            Color = DiscordColor.Gold // 금색으로 강조
        };

        return embed.Build();
    }
}