using Newtonsoft.Json;

namespace Discord_Music;

public class Config
{
    private static Config _instance;
    public static Config Instance => _instance ??= new Config();
    
    private Dictionary<string, string> _configs;

    public Config()
    {
        _configs = new Dictionary<string, string>();
        
        LoadConfigFile("Config.json");
    }

    // JSON 파일에서 언어 데이터를 로드
    void LoadConfigFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Config file not found: {filePath}");
        }

        // JSON 파일을 읽고 Dictionary로 파싱
        var jsonContent = File.ReadAllText(filePath);
        _configs = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent);
    }

    public static string Get(string key)
    {
        if (!Instance._configs.ContainsKey(key))
            return $"Missing key: {key}";

        return Instance._configs[key];
    }
}