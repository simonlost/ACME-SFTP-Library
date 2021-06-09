using Synlait.SFTP.Library;
using System;
using Microsoft.Extensions.Options;
using Xunit;

namespace SML_SFTP_Library_TESTS
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            SFTPConfiguration sftpConfig = new SFTPConfiguration();
            sftpConfig.FilePath = "/public/ANZTest2";
            sftpConfig.Host = "13.70.87.150";
            sftpConfig.SFTPPort = "22";
            sftpConfig.Port = 22;
            sftpConfig.Username = "sftpuser";
            sftpConfig.Password = "YVmt2Kuk";

            SFTPConnection conn = new SFTPConnection(sftpConfig);
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
