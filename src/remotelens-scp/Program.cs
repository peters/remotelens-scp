using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HumanBytes;
using remotelens_scp.Mono.Options;
using Renci.SshNet;

namespace remotelens_scp
{
    internal class Program
    {
        static OptionSet _opts;

        static int Main(string[] args)
        {
            var arguments = new Arguments
            {
                Port = 22
            };

            var actionType = default(ActionType);

            _opts = new OptionSet
            {
                "Usage: remotelens-scp.exe command [OPTS]",
                "Upload files using SCP",
                "",
                "Commands:",
                {"host=", "Remote SSH server (e.g sftp.domeneshop.no)", _ => arguments.Host = _},
                {
                    "port=", "Port to use when connecting (default: 22)", _ =>
                    {
                        int port;
                        int.TryParse(_, out port);
                        arguments.Port = port <= 0 ? arguments.Port : port;
                    }
                },
                {"username=", "Username to use when connecting", _ => arguments.Username = _},
                {"password=", "Password to use when connecting", _ => arguments.Password = _},
                {"ppk=", "Private key file to use when connecting", _ => arguments.PrivateKeyFile = _},
                "",
                "Options:",
                {"h|?|help", "Display Help and exit", _ => arguments.DisplayHelp = true},
                {
                    "upload-files=", "The local files you wish to upload. (e.g C:\\test.txt,C:\\test2.txt))", _ =>
                    {
                        actionType = ActionType.UploadFile;
                        arguments.UploadFiles = _;
                    }
                },
                {
                    "upload-destination=",
                    "The remote destination location for where to store uploaded files. (e.g /home/user)",
                    _ =>
                    {
                        actionType = ActionType.UploadFile;
                        arguments.UploadDestination = _;
                    }
                }
            };

            _opts.Parse(args);

            if (actionType == ActionType.None || arguments.DisplayHelp)
            {
                _opts.WriteOptionDescriptions(Console.Out);
                return -1;
            }

            if (string.IsNullOrEmpty(arguments.Host))
            {
                Console.WriteLine("Error: Please specify a valid host.");
                return -1;
            }

            if (string.IsNullOrEmpty(arguments.Username))
            {
                Console.WriteLine("Error: Please specify a valid username.");
                return -1;
            }

            if (string.IsNullOrEmpty(arguments.Password) && string.IsNullOrEmpty(arguments.PrivateKeyFile))
            {
                Console.WriteLine("Error: Please specify either a password or a private key file.");
                return -1;
            }

            if (string.IsNullOrEmpty(arguments.Password))
            {
                if (!File.Exists(arguments.PrivateKeyFile))
                {
                    Console.WriteLine("Error: Unable to find private key file: {0}", arguments.PrivateKeyFile);
                    return -1;
                }
                try
                {
                    var bytes = File.ReadAllBytes(arguments.PrivateKeyFile);
                    arguments.PrivateKeyStream = new MemoryStream();
                    arguments.PrivateKeyStream.Write(bytes, 0, bytes.Length);
                    arguments.PrivateKeyStream.Seek(0, SeekOrigin.Begin);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: Unable to read private key file. Reason: {0}", ex.Message);
                    return -1;
                }
            }

            switch (actionType)
            {
                case ActionType.UploadFile:
                    try
                    {
                        return ConnectAndUpload(arguments);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error when attempting to upload. Why: {0}", ex.Message);
                    }
                    break;
            }

            return -1;
        }

        static int ConnectAndUpload(Arguments arguments)
        {
            if (string.IsNullOrEmpty(arguments.UploadDestination))
            {
                Console.WriteLine("Error: Please specify a remote destination.");
                return -1;
            }

            var filesToUpload = new Dictionary<string, decimal>();
            var parsedUploadFiles = arguments.UploadFiles?.Split(',');
            if (parsedUploadFiles == null)
            {
                Console.WriteLine("Error: Unable to parse list of files to upload");
                return -1;
            }

            foreach (var filename in parsedUploadFiles)
            {
                if (string.IsNullOrEmpty(filename) || !File.Exists(filename))
                {
                    Console.WriteLine("Error: Unable to find local file: {0}", filename);
                    return -1;
                }
                if (filesToUpload.ContainsKey(filename))
                {
                    Console.WriteLine("Warning: Skipping duplicate filename: {0}", filename);
                    continue;
                }
                filesToUpload.Add(filename, 0);
            }

            if (!filesToUpload.Any())
            {
                Console.WriteLine("Error: Please specify a filename to upload.");
                return -1;
            }

            using (var scp = WithScpClient(arguments))
            using (arguments.PrivateKeyStream)
            {
                var uploadProgress = new Dictionary<string, decimal>();

                scp.Uploading += (sender, args) =>
                {
                    var percentage = Math.Floor((decimal)args.Uploaded / args.Size * 100);

                    decimal previousPercentage;
                    if (!uploadProgress.TryGetValue(args.Filename, out previousPercentage))
                    {
                        uploadProgress.Add(args.Filename, 0);
                    }
                    else
                    {
                        uploadProgress[args.Filename] = percentage;
                    }

                    if (previousPercentage != percentage || percentage >= 100)
                    {
                        Console.WriteLine("{0}: {1} of {2} - {3:N0}%", args.Filename, args.Uploaded.Bytes(), args.Size.Bytes(), percentage);
                    }
                };

                scp.ErrorOccurred += (sender, args) => { Console.WriteLine("SCP Error: {0}", args.Exception.Message); };

                Console.WriteLine("Connecting to server {0} on port {1}", arguments.Host, arguments.Port);

                scp.Connect();

                if (!scp.IsConnected)
                {
                    Console.WriteLine("Unable to connect to server.");
                    return -1;
                }

                Console.WriteLine("Preparing to upload {0} files.", filesToUpload.Count);
                foreach (var pair in filesToUpload)
                {
                    scp.Upload(new FileInfo(pair.Key), arguments.UploadDestination);
                }

                Console.WriteLine("Finished, disconnecting.");
                scp.Disconnect();
            }

            return 0;
        }

        static ScpClient WithScpClient(Arguments arguments)
        {
            return arguments.PrivateKeyStream == null
                ? new ScpClient(arguments.Host, arguments.Port, arguments.Password)
                : new ScpClient(arguments.Host, arguments.Port, arguments.Username,
                    new PrivateKeyFile(arguments.PrivateKeyStream));
        }
        
        class Arguments
        {
            public string Host { get; set; }
            public int Port { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
            public string PrivateKeyFile { get; set; }
            public MemoryStream PrivateKeyStream { get; set; }
            public string UploadFiles { get; set; }
            public string UploadDestination { get; set; }
            public bool DisplayHelp { get; set; }
        }

        enum ActionType
        {
            None,
            UploadFile
        }
    }
}