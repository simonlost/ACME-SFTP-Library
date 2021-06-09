namespace Acme.SFTP.Library
{
    public interface ISFTPConfig
    {
        string Host { get; set; }
        int Port { get; set; }
        string Username { get; set; }
        string Password { get; set; }
        string FilePath { get; set; }
        string SFTPPort { get; set; }
        string PrivateKeyPath { get; set; }
        string PassPhrase { get; set; }
    }
}