
public class FtpSetting
{
    public bool Enabled { get; set; } = false;
    public string Host { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int Port { get; set; } = 21;
    public string RemoteDir { get; set; } = "/";
}
