using System.Collections.Specialized;

namespace ClipFlow.Server.Models
{

    public class ClipboardData
    {
        public ClipboardType Type { get; set; }
        public string Uuid { get; set; }
        public ulong? DataLength { get; set; }
    }

    public enum ClipboardType
    {
        Text,       // 文本
        File,      // 单个文件
        FileList,   // 多个文件
    }
}