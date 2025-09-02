namespace FileExplorerApplicationV1.Models
{
    public class SftpSetting
    {
        public bool Enabled { get; set; } = true;
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 22;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string RemoteDir { get; set; } = "/";
    }
}