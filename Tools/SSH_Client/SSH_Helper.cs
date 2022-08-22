using Renci.SshNet;

namespace Tools
{

    /// <summary>
    /// 默认登录ssh 账号 使用root ，不然可能权限会不够
    /// </summary>
    public class SSH_Helper
    {
        /// <summary>
        /// 指定文件目录上传
        /// </summary>
        /// <param name="testPath"></param>
        /// <param name="tarPath"></param>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="keyPath"></param>
        public void SFTP(string testPath, string tarPath, string ip, int port, string keyPath)
        {
            Console.WriteLine("SFTP Helper");
            
            PrivateKeyFile keyFiles = new PrivateKeyFile(keyPath);
            
            var sftp = new SftpClient(ip, port, "root", keyFiles); //创建ssh连接对象
            sftp.Connect();  //  连接
            
            var ssh = new SshClient(ip, port, "root", keyFiles); //创建ssh连接对象
            ssh.Connect();  //  连接
            
            ClearDirectory(sftp, ssh, testPath, tarPath, string.Empty);
            
            GetUploadDirectory(sftp, ssh, testPath, tarPath, string.Empty);
            
            sftp.Disconnect();     //断开连接及文件流
            ssh.Disconnect();

        }
        
        /// <summary>
        /// 清空指定目录下所有文件信息
        /// </summary>
        /// <param name="sftp"></param>
        /// <param name="ssh"></param>
        /// <param name="path"></param>
        /// <param name="tarPath"></param>
        /// <param name="subPath"></param>
        public void ClearDirectory(SftpClient sftp, SshClient ssh, string path, string tarPath,  string subPath)
        {
            
            var commed = $"rm -rf {tarPath}";
            var cmd = ssh.RunCommand(commed); //执行指令
            
            if (!string.IsNullOrEmpty(cmd.Error))
            {
                Console.WriteLine("Error:\t" + cmd.Error + "\n");   //有错误信息则打印
                return;
            }
            else
            {
                Console.WriteLine(cmd.Result);  //打印结果
                Console.WriteLine("目标路径下所有文件清空");  //打印结果
            }
        }
        
        public void GetUploadDirectory(SftpClient sftp, SshClient ssh, string path, string tarPath,  string subPath)
        {
            //获取当前路径下的模板文件，指定文件进行处理
            DirectoryInfo root = new DirectoryInfo(path);
            FileInfo[] files = root.GetFiles();

            foreach (FileInfo fi in files)
            {
                SetFileUpload(sftp, ssh, fi.Name, fi.FullName, tarPath, subPath);
            }

            DirectoryInfo[] dir = root.GetDirectories();

            string sinPath = String.Empty;
            
            foreach (DirectoryInfo di in dir)
            {
                if (!subPath.IsNullOrEmpty())
                {
                    sinPath = subPath + "/" + di.Name;
                }
                else
                {
                    sinPath = di.Name;
                }
                
                GetUploadDirectory(sftp, ssh, di.FullName, tarPath, sinPath);
            }
        }
        
        public void SetFileUpload(SftpClient sftp, SshClient ssh, string name, string filePath, string tarPath, string subPath)
        {
            string tarFilePath = String.Empty;
            string tarDirPath = String.Empty;
            if (subPath.IsNullOrEmpty())
            {
                tarDirPath = $"{tarPath}";
                tarFilePath = $"{tarPath}/{name}";
            }
            else
            {
                tarDirPath = $"{tarPath}/{subPath}";
                tarFilePath = $"{tarPath}/{subPath}/{name}";
            }

            var commed = $"mkdir -p {tarDirPath}";
            var cmd = ssh.RunCommand(commed); //执行指令
            
            if (!string.IsNullOrEmpty(cmd.Error))
            {
                Console.WriteLine("Error:\t" + cmd.Error + "\n");   //有错误信息则打印
                return;
            }
            else
            {
                Console.WriteLine(cmd.Result);  //打印结果
                Console.WriteLine("mkdir:" + tarDirPath);  //打印结果
            }

            sftp.UploadFile(File.Open(filePath, FileMode.Open), tarFilePath, CL);
            Console.WriteLine("UploadFile:" + filePath + "~ to ~ " +  tarFilePath);  //打印结果
            
            var commed2 = $"chmod +x {tarFilePath}";
            var cmd2 = ssh.RunCommand(commed2); //执行指令
            
            if (!string.IsNullOrEmpty(cmd2.Error))
            {
                Console.WriteLine("Error:\t" + cmd2.Error + "\n");   //有错误信息则打印
                return;
            }
            else
            {
                Console.WriteLine("chmod:" + name);  //打印结果
            }
            
        }

        public void CL(ulong ui)
        {
            Console.WriteLine("CL:" + ui);
        }


        /// <summary>
        /// 实现CMD功能
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="keyPath"></param>
        public void CMD(string ip, int port, string keyPath)
        {
            Console.WriteLine("CMD Helper");
            
            PrivateKeyFile keyFiles = new PrivateKeyFile(keyPath);
            
            var ssh = new SshClient(ip, port, "root", keyFiles); //创建ssh连接对象
            ssh.Connect();  //  连接
            
            while (true) // 循环写入
            {
                Console.Write(">> ");
                var commed = Console.ReadLine();    // 读取指令
 
                if (string.IsNullOrEmpty(commed)) continue;     //指令为空则continue
                if (string.Equals(commed,"exit")) break;   //指令为“exit”退出循环
 
                var cmd = ssh.RunCommand(commed); //执行指令
 
                //var cmd = ssh.RunCommand(commed).Executed();  这种方式默认返回result,不能打印错误信息
 
                if (!string.IsNullOrEmpty(cmd.Error))
                {
                    Console.WriteLine("Error:\t" + cmd.Error + "\n");   //有错误信息则打印
                }
                else
                {
                    Console.WriteLine(cmd.Result);  //打印结果
                }
            }
            ssh.Disconnect();     //断开连接及文件流
        }
        

    }
}