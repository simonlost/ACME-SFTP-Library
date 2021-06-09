using System.Collections.Generic;
using System.IO;
using Renci.SshNet.Sftp;

namespace Acme.SFTP.Library
{
    public interface ISFTPConnection
    {
        ISFTPConfig _config { get; set; }
        int Send(string filename);
        int Send(MemoryStream csvStream, string fileName);
        List<SftpFile> ListFilesInDirectory(string filePath);
        bool DownloadDirectory(string source, string destination);
        string[] DownLoadFile(string fileName, string filePath);
        void MoveFile(string file, string source, string destination);
        void Move(string originalFile, string newFile, string source, string destination);
    }
}