using Synlait.SFTP.Library;
using System;
using Microsoft.Extensions.Options;
using Xunit;
using System.Reflection;
using System.IO;

namespace SML_SFTP_Library_TESTS
{
    public class KeyConnectionTests
    {
        [Fact]
        public void Test1()
        {
            var currentDir = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/anz-ssh-private-key.txt";
            SFTPConfiguration sftpConfig = new SFTPConfiguration();
            sftpConfig.FilePath = "/ANZ";
            sftpConfig.Host = "13.70.87.150";
            sftpConfig.SFTPPort = "22";
            sftpConfig.Port = 22;
            sftpConfig.Username = "anz";
            sftpConfig.Password = "FMfs4US1qk5N";
            sftpConfig.PrivateKeyPath = null;
            sftpConfig.PassPhrase = "N5ETixFx07yE";

            SFTPConnection conn = new SFTPConnection(sftpConfig);
            conn._config.PrivateKeyPath = currentDir;
            try
            {
                var result = conn.ListFilesInDirectory("");
                Assert.NotNull(result);
            }
            catch (Exception e)
            {
                Assert.False(true);
            }

        }
    }
}
