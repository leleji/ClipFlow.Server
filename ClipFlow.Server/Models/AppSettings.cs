namespace ClipFlow.Server.Models
{
    public class AppSettings
    {
        public List<TokenSetting> TokenSettings { get; set; }

        public int FileCacheMinutes { get; set; } = 60; // 默认缓存1小时
    }

    public class TokenSetting
    {
        public string Token { get; set; }
       
        public double MaxFileSize { get; set; } = 0; // 最大多少mb，默认不限制
    }
} 