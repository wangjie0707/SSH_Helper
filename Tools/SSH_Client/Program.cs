namespace Tools
{
    class Program
    {
        static void Main(string[] args)
        {
            SSH_Helper ssh = new SSH_Helper();
            ssh.SFTP(@"D:\docker", "/test", "127.0.0.1", 22, @"D:\AAA.pem");
        }
    }
}