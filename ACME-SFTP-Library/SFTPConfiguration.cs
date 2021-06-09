namespace Acme.SFTP.Library
{
    public class SFTPConfiguration : ISFTPConfig
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string FilePath { get; set; }
        public string SFTPPort { get; set; }
        public string PrivateKeyPath { get; set; }
        public string PassPhrase { get; set; }
    }
}