using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using Serilog;

namespace Acme.SFTP.Library
{
    public class SFTPConnection : ISFTPConnection
    {
        public ISFTPConfig _config { get; set; }
        public PrivateKeyFile[] privateKey;

        public SFTPConnection(IOptions<SFTPConfiguration> config)
        {
            _config = config.Value;
            _config.Port = int.Parse(_config.SFTPPort);
        }

        protected SFTPConnection()
        {
        }

        public SFTPConnection(ISFTPConfig config)
        {
            _config = (SFTPConfiguration) config;
        }

        private ConnectionInfo GetConnectionInfo()
        {
            ConnectionInfo conInfo;
            var methods = new List<AuthenticationMethod>();
            methods.Add(new PasswordAuthenticationMethod(_config.Username, _config.Password));
            if (!string.IsNullOrWhiteSpace(_config.PrivateKeyPath)) {
                var keyFile = new PrivateKeyFile(_config.PrivateKeyPath, _config.PassPhrase);
                privateKey = new[] { keyFile };
                methods.Add(new PrivateKeyAuthenticationMethod(_config.Username, privateKey));
            }
            conInfo = new ConnectionInfo(_config.Host, _config.Port, _config.Username, methods.ToArray());
            return conInfo;
        }

        public int Send(string filename)
        {
            // Upload File
            try
            {
                if (string.IsNullOrEmpty(filename))
                {
                    filename = "";
                    throw new NullReferenceException("Filename supplied is null or empty");
                }
                _config.Port = int.Parse(_config.SFTPPort);
                using (var sftp = new SftpClient(GetConnectionInfo()))
                {
                    sftp.Connect();
                    if (!string.IsNullOrEmpty(_config.FilePath))
                        sftp.ChangeDirectory(_config.FilePath);
                    using (var uplfileStream = System.IO.File.OpenRead(filename))
                    {
                        sftp.UploadFile(uplfileStream, filename, true);
                    }

                    sftp.Disconnect();
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Log.Error(ex, $"Error attempting to send file: {filename}");
                throw;
            }
        }

        public int Send(MemoryStream csvStream, string filename)
        {
            try
            {
                if (string.IsNullOrEmpty(filename))
                {
                    filename = "";
                    throw new NullReferenceException("Filename supplied is null or empty");
                }

                if (csvStream == null)
                {
                    throw new NullReferenceException("file stream is null");
                }

                using (csvStream)
                {
                    _config.Port = int.Parse(_config.SFTPPort);
                    using (var sftp = new SftpClient(GetConnectionInfo()))
                    {
                        sftp.Connect();
                        if (!string.IsNullOrEmpty(_config.FilePath))
                            sftp.ChangeDirectory(_config.FilePath);
                        using (csvStream)
                        {
                            sftp.UploadFile(csvStream, filename, true);
                        }

                        sftp.Disconnect();
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Log.Error(ex, $"Error attempting to send file: {filename}");
                throw;
            }
        }

        /// <summary>
        /// Get list of files in directory. Checks files to ensure they are not in the process of being downloaded. Only returns with certain that all files are stable in size
        /// </summary>
        /// <param name="filePath">Optional variable</param>
        /// <returns></returns>
        public List<SftpFile> ListFilesInDirectory(string filePath)
        {
            string directoryPath = _config.FilePath;
            if (!string.IsNullOrEmpty(filePath))
            {
                directoryPath = directoryPath + "/" + filePath;
            }

            List<SftpFile> fileList = new List<SftpFile>();
            List<SftpFile> currentFileList = new List<SftpFile>();
            List<SftpFile> current2FileList = new List<SftpFile>();
            try
            {
                bool allFilesAreStable = false;
                int iterationCheck = 0;
                while (!allFilesAreStable)
                {
                    allFilesAreStable = true;
                    currentFileList = GetDirectoryFileList(directoryPath);
                    currentFileList = currentFileList.Where(o => o.IsDirectory == false).ToList();
                    Task.Delay(1000).Wait();
                    current2FileList = GetDirectoryFileList(directoryPath);
                    current2FileList = current2FileList.Where(o => o.IsDirectory == false).ToList();
                    // check to ensure that all files are stable and not in the process of being loaded in...

                    foreach (var file in currentFileList)
                    {
                        foreach (var file2 in current2FileList)
                        {
                            if (file.FullName == file2.FullName)
                            {
                                if (file.Attributes.Size != file2.Attributes.Size)
                                {
                                    allFilesAreStable = false;
                                }
                            }
                        }
                    }

                    iterationCheck++;
                    if (iterationCheck > 99)
                        break;
                }

                if (iterationCheck > 99)
                {
                    return new List<SftpFile>();
                }

                fileList = currentFileList;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Log.Error(ex, "No folder matches the run id provided");
                throw;
            }

            return fileList;
        }

        private List<SftpFile> GetDirectoryFileList(string directoryPath)
        {
            try
            {
                if (string.IsNullOrEmpty(directoryPath))
                {
                    directoryPath = "";
                    throw new NullReferenceException("DirectoryPath supplied is null or empty");
                }

                List<SftpFile> fileList = new List<SftpFile>();
                _config.Port = int.Parse(_config.SFTPPort);
                using (var sftp = new SftpClient(GetConnectionInfo()))
                {
                    sftp.Connect();
                    fileList = GetDirectoryFileList(directoryPath, sftp);
                    sftp.Disconnect();
                }

                return fileList;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Log.Error(ex, "Error retrieving directory file list");
                throw;
            }
        }

        private List<SftpFile> GetDirectoryFileList(string directoryPath, SftpClient sftp)
        {
            List<SftpFile> fileList = new List<SftpFile>();
            fileList = sftp.ListDirectory(directoryPath).ToList();
            fileList.Remove(fileList.FirstOrDefault(c => c.Name == "."));
            fileList.Remove(fileList.FirstOrDefault(c => c.Name == ".."));
            fileList.Remove(fileList.FirstOrDefault(c => c.IsDirectory));
            return fileList;
        }


        public bool DownloadDirectory(string source, string destination)
        {
            try
            {
                if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(destination))
                {
                    throw new NullReferenceException("One or more of fsource or destination is null or empty");
                }
                _config.Port = int.Parse(_config.SFTPPort);
                using (var sftp = new SftpClient(GetConnectionInfo()))
                {
                    sftp.Connect();
                    //if (!string.IsNullOrEmpty(_config.FilePath))
                    //    sftp.ChangeDirectory(_config.FilePath + "\\" + source);
                    var files = sftp.ListDirectory(_config.FilePath + "/" + source);
                    foreach (var file in files)
                    {
                        if (!file.IsDirectory && !file.IsSymbolicLink)
                        {
                            DownloadFile(sftp, file, destination);
                        }
                        else if (file.IsSymbolicLink)
                        {
                            // TODO:
                            Console.WriteLine("Ignoring symbolic link {0}", file.FullName);
                        }
                        else if (file.Name != "." && file.Name != "..")
                        {
                            Console.WriteLine("We are ignoring internal folders {0}", file.FullName);
                        }
                    }

                    sftp.Disconnect();
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Log.Error(ex, "Error attempting to download all files from a directory into another directory");
                return false;
            }
        }

        /// <summary>
        /// Used for getting content of the Metadata file
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public string[] DownLoadFile(string fileName, string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                {
                    fileName = "";
                    throw new NullReferenceException("fileName supplied is null or empty");
                }

                string[] fileLines;
                string fileLoadPath = _config.FilePath + "/";
                if (!string.IsNullOrEmpty(filePath))
                {
                    fileLoadPath = fileLoadPath + filePath + "/";
                }

                fileLoadPath = fileLoadPath + fileName;
                _config.Port = int.Parse(_config.SFTPPort);
                using (var sftp = new SftpClient(GetConnectionInfo()))
                {
                    sftp.Connect();

                    fileLines = sftp.ReadAllLines(fileLoadPath);
                    sftp.Disconnect();
                }

                return fileLines;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Log.Error(ex, $"Error during process of downloading file {fileName}");
                throw;
            }
        }


        private void DownloadFile(SftpClient client, SftpFile file, string directory)
        {
            Console.WriteLine("Downloading {0}", file.FullName);
            using (Stream fileStream = File.OpenWrite(Path.Combine(directory, file.Name)))
            {
                client.DownloadFile(file.FullName, fileStream);
            }
        }

        public void MoveFile(string file, string source, string destination)
        {
            try
            {
                if (string.IsNullOrEmpty(file) || string.IsNullOrEmpty(source) || string.IsNullOrEmpty(destination))
                {
                    throw new NullReferenceException("One or more of file, source or destination is null or empty");
                }
                _config.Port = int.Parse(_config.SFTPPort);
                using (var sftp = new SftpClient(GetConnectionInfo()))
                {
                    List<SftpFile> fileList = new List<SftpFile>();

                    sftp.Connect();
                    fileList = GetDirectoryFileList(source, sftp);
                    SftpFile fileToMove = fileList.First(o => o.Name == file);
                    //fileToMove.MoveTo(source + "/" + destination + "/" + file);
                    fileToMove.MoveTo(destination + "/" + file);
                    sftp.Disconnect();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Log.Error(ex, "Error attempting to move file");
                throw;
            }
        }

        public void Move(string originalFile, string newFile, string source, string destination)
        {
            var loggerprefix = "SFTPConnection:Move";
            try
            {
                if (string.IsNullOrEmpty(originalFile) || string.IsNullOrEmpty(newFile) || string.IsNullOrEmpty(source) || string.IsNullOrEmpty(destination))
                {
                    throw new NullReferenceException("One or more of source or destination is null or empty");
                }
                _config.Port = int.Parse(_config.SFTPPort);
                using (var sftp = new SftpClient(GetConnectionInfo()))
                {
                    List<SftpFile> fileList = new List<SftpFile>();

                    sftp.Connect();
                    fileList = GetDirectoryFileList(source, sftp);
                    SftpFile fileToMove = fileList.First(o => o.Name == originalFile);
                    //fileToMove.MoveTo(source + "/" + destination + "/" + file);
                    fileToMove.MoveTo(destination + "/" + newFile);
                    sftp.Disconnect();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Log.Error(ex, $"{loggerprefix} Error attempting to move file");
                throw;
            }
        }
    }
}