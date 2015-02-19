# remotelens-scp

A simple SSH/SCP upload file utility written in .NET that allow you to upload multiple files to a remote server. This program
requires .NET 4.5.1 to be installed on the local computer to work. It uses the amazing [SSH.NET LIBRARY](http://sshnet.codeplex.com/).

![image](https://cloud.githubusercontent.com/assets/869/6272458/0e56b0ca-b868-11e4-9b65-bf881aeba469.png)

### How to upload a single file using username/password

```ps
.\remotelens-scp.exe --host sftp.google.com --username google --password 123 --upload-files test.txt --upload-destination /home/google/test.txt
```

### How to upload multiple files using private key (OPENSSH FORMAT)

```ps
.\remotelens-scp.exe --host sftp.google.com --username google --upload-files test.txt,test2.txt --upload-destination /home/google --ppk %LOCALAPPDATA%\.ssh\github_rsa
```

### License

BSD.
