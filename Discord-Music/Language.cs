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
}