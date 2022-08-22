using System.Diagnostics.Contracts;
using ICSharpCode.SharpZipLib.Checksums;
using ICSharpCode.SharpZipLib.Zip;
using Renci.SshNet;

namespace Tools
{
    /// <summary>
    /// 默认登录ssh 账号 使用root ，不然可能权限会不够
    /// </summary>
    public class SSH_Helper
    {
        public long zipSize;
        public long pl = 200;
        public long cu = 0;
        /// <summary>
        /// 指定文件目录上传
        /// </summary>
        /// <param name="airPath"></param>
        /// <param name="tarPath"></param>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="keyPath"></param>
        public void ZIP_SFTP(string airPath, string tarPath, string ip, int port, string keyPath)
        {
            Console.WriteLine("SFTP and ZIP Helper");

            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            
            PrivateKeyFile keyFiles = new PrivateKeyFile(keyPath);

            var sftp = new SftpClient(ip, port, "root", keyFiles); //创建ssh连接对象
            sftp.Connect(); //  连接

            var ssh = new SshClient(ip, port, "root", keyFiles); //创建ssh连接对象
            ssh.Connect(); //  连接

            ClearDirectory(sftp, ssh, airPath, tarPath, string.Empty);
            ClearDirectory(sftp, ssh, airPath, "/zip", string.Empty);
            
            Directory.SetCurrentDirectory(Directory.GetParent(airPath).FullName);
            string parentPath = Directory.GetCurrentDirectory();

            string zipName = $"{parentPath}/server.zip";
            CompressDirectory(airPath, zipName, 9, false);
            // ZipFileDictory(airPath, zipName);
            
            FileInfo t = new FileInfo(zipName);//获取文件
            zipSize = t.Length;
            
            SetFileUpload(sftp, ssh, "server.zip", zipName, "/zip", "");
            
            SetCommand(ssh, $"cd /zip; unzip -o server.zip  -d /server");
            
            sftp.Disconnect(); //断开连接及文件流
            ssh.Disconnect();
        }

         #region 压缩文件及文件夹
        /// <summary>
        /// 递归压缩文件夹方法
        /// </summary>
        /// <param name="FolderToZip"></param>
        /// <param name="ZOPStream">压缩文件输出流对象</param>
        /// <param name="ParentFolderName"></param>
        private bool ZipFileDictory(string FolderToZip, ZipOutputStream ZOPStream, string ParentFolderName)
        {
            bool res = true;
            string[] folders, filenames;
            ZipEntry entry = null;
            FileStream fs = null;
            Crc32 crc = new Crc32();
            try
            {
                //创建当前文件夹 
                entry = new ZipEntry(Path.Combine(ParentFolderName, Path.GetFileName(FolderToZip) + "/"));  //加上 “/” 才会当成是文件夹创建
                ZOPStream.PutNextEntry(entry);
                ZOPStream.Flush();
                //先压缩文件，再递归压缩文件夹 
                filenames = Directory.GetFiles(FolderToZip);
                foreach (string file in filenames)
                {
                    //打开压缩文件
                    fs = File.OpenRead(file);
                    byte[] buffer = new byte[fs.Length];
                    fs.Read(buffer, 0, buffer.Length);
                    entry = new ZipEntry(Path.Combine(ParentFolderName, Path.GetFileName(FolderToZip) + "/" + Path.GetFileName(file)));
                    entry.DateTime = DateTime.Now;
                    entry.Size = fs.Length;
                    fs.Close();
                    crc.Reset();
                    crc.Update(buffer);
                    entry.Crc = crc.Value;
                    ZOPStream.PutNextEntry(entry);
                    ZOPStream.Write(buffer, 0, buffer.Length);
                }
            }
            catch
            {
                res = false;
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                    fs = null;
                }
                if (entry != null)
                {
                    entry = null;
                }
                GC.Collect();
                GC.Collect(1);
            }
            folders = Directory.GetDirectories(FolderToZip);
            foreach (string folder in folders)
            {
                if (!ZipFileDictory(folder, ZOPStream, Path.Combine(ParentFolderName, Path.GetFileName(FolderToZip))))
                {
                    return false;
                }
            }
 
            return res;
        }
 
        /// <summary>
        /// 压缩目录
        /// </summary>
        /// <param name="FolderToZip">待压缩的文件夹</param>
        /// <param name="ZipedFile">压缩后的文件名</param>
        /// <returns></returns>
        private bool ZipFileDictory(string FolderToZip, string ZipedFile)
        {
            bool res;
            if (!Directory.Exists(FolderToZip))
            {
                return false;
            }
            ZipOutputStream ZOPStream = new ZipOutputStream(File.Create(ZipedFile));
            ZOPStream.SetLevel(9);
            res = ZipFileDictory(FolderToZip, ZOPStream, "");
            ZOPStream.Finish();
            ZOPStream.Close();
            return res;
        }
 
        /// <summary>
        /// 压缩文件和文件夹
        /// </summary>
        /// <param name="FileToZip">待压缩的文件或文件夹</param>
        /// <param name="ZipedFile">压缩后生成的压缩文件名，全路径格式</param>
        /// <returns></returns>
        public bool Zip(String FileToZip, String ZipedFile)
        {
            if (Directory.Exists(FileToZip))
            {
                return ZipFileDictory(FileToZip, ZipedFile);
            }
            else
            {
                return false;
            }
        }
        #endregion
        
        /// <summary>
        /// 压缩文件夹
        /// </summary>
        /// <param name="dirPath">要打包的文件夹</param>
        /// <param name="GzipFileName">目标文件名</param>
        /// <param name="CompressionLevel">压缩品质级别（0~9）</param>
        /// <param name="deleteDir">是否删除原文件夹</param>
        public static void CompressDirectory(string dirPath, string GzipFileName, int CompressionLevel, bool deleteDir)
        {
            //压缩文件为空时默认与压缩文件夹同一级目录
            if (GzipFileName == string.Empty)
            {
                GzipFileName = dirPath.Substring(dirPath.LastIndexOf("\\"));
                GzipFileName = dirPath.Substring(0, dirPath.LastIndexOf("\\")) + "\\" + GzipFileName + ".zip";
            }

            try
            {
                using (ZipOutputStream zipoutputstream = new ZipOutputStream(File.Create(GzipFileName)))
                {
                    //设置压缩文件级别
                    zipoutputstream.SetLevel(CompressionLevel);
                    Crc32 crc = new Crc32();
                    Dictionary<string, DateTime> fileList = GetAllFies(dirPath);
                    foreach (KeyValuePair<string, DateTime> item in fileList)
                    {
                        //将文件数据读到流里面
                        FileStream fs = File.OpenRead(item.Key.ToString());
                        byte[] buffer = new byte[fs.Length];
                        //从流里读出来赋值给缓冲区
                        fs.Read(buffer, 0, buffer.Length);
                        ZipEntry entry = new ZipEntry(item.Key.Substring(dirPath.Length));
                        entry.DateTime = item.Value;
                        entry.Size = fs.Length;
                        fs.Close();
                        crc.Reset();
                        crc.Update(buffer);
                        entry.Crc = crc.Value;
                        zipoutputstream.PutNextEntry(entry);
                        zipoutputstream.Write(buffer, 0, buffer.Length);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            


            if (deleteDir)
            {
                // Directory.Delete(dirPath, true);
            }
        }

        /// <summary>
        /// 获取所有文件
        /// </summary>
        /// <returns></returns>
        private static Dictionary<string, DateTime> GetAllFies(string dir)
        {
            Dictionary<string, DateTime> FilesList = new Dictionary<string, DateTime>();
            DirectoryInfo fileDire = new DirectoryInfo(dir);
            if (!fileDire.Exists)
            {
                throw new System.IO.FileNotFoundException("目录:" + fileDire.FullName + "没有找到!");
            }

            GetAllDirFiles(fileDire, FilesList);
            GetAllDirsFiles(fileDire.GetDirectories(), FilesList);
            return FilesList;
        }

        /// <summary>
        /// 获取一个文件夹下的所有文件夹里的文件
        /// </summary>
        /// <param name="dirs"></param>
        /// <param name="filesList"></param>
        private static void GetAllDirsFiles(DirectoryInfo[] dirs, Dictionary<string, DateTime> filesList)
        {
            foreach (DirectoryInfo dir in dirs)
            {
                if (dir.Name.Equals("tmp"))
                {
                    continue;;
                }
                foreach (FileInfo file in dir.GetFiles("*.*"))
                {
                    filesList.Add(file.FullName, file.LastWriteTime);
                }

                GetAllDirsFiles(dir.GetDirectories(), filesList);
            }
        }

        /// <summary>
        /// 获取一个文件夹下的文件
        /// </summary>
        /// <param name="dir">目录名称</param>
        /// <param name="filesList">文件列表HastTable</param>
        private static void GetAllDirFiles(DirectoryInfo dir, Dictionary<string, DateTime> filesList)
        {
            foreach (FileInfo file in dir.GetFiles("*.*"))
            {
                filesList.Add(file.FullName, file.LastWriteTime);
            }
        }

        /// <summary>
        /// 指定文件目录上传
        /// </summary>
        /// <param name="airPath"></param>
        /// <param name="tarPath"></param>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="keyPath"></param>
        public void SFTP(string airPath, string tarPath, string ip, int port, string keyPath)
        {
            Console.WriteLine("SFTP Helper");
            
            PrivateKeyFile keyFiles = new PrivateKeyFile(keyPath);

            var sftp = new SftpClient(ip, port, "root", keyFiles); //创建ssh连接对象
            sftp.Connect(); //  连接

            var ssh = new SshClient(ip, port, "root", keyFiles); //创建ssh连接对象
            ssh.Connect(); //  连接

            ClearDirectory(sftp, ssh, airPath, tarPath, string.Empty);

            CompressDirectory(@"C:\Users\zhao\Desktop\新建文件夹 (2)", @"C:\Users\zhao\Desktop\111111\666.zip", 9, false);
            
            GetUploadDirectory(sftp, ssh, airPath, tarPath, string.Empty);

            sftp.Disconnect(); //断开连接及文件流
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
        public void ClearDirectory(SftpClient sftp, SshClient ssh, string path, string tarPath, string subPath)
        {
            var commed = $"rm -rf {tarPath}";
            var cmd = ssh.RunCommand(commed); //执行指令

            if (!string.IsNullOrEmpty(cmd.Error))
            {
                Console.WriteLine("Error:\t" + cmd.Error + "\n"); //有错误信息则打印
                return;
            }
            else
            {
                Console.WriteLine(cmd.Result); //打印结果
                Console.WriteLine($"目标路径{tarPath}下所有文件清空"); //打印结果
            }
        }

        public void GetUploadDirectory(SftpClient sftp, SshClient ssh, string path, string tarPath, string subPath)
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
                if (di.Name.Equals("tmp"))
                {
                    continue;
                }

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

        public void SetCommand(SshClient ssh, string str)
        {
            var commed = str;
            var cmd = ssh.RunCommand(commed); //执行指令

            if (!string.IsNullOrEmpty(cmd.Error))
            {
                Console.WriteLine("Error:\t" + cmd.Error + "\n"); //有错误信息则打印
                return;
            }
            else
            {
                Console.WriteLine(cmd.Result); //打印结果
                Console.WriteLine(str + "success"); //打印结果
            }
            
        }

        public void SetFileUpload(SftpClient sftp, SshClient ssh, string name, string filePath, string tarPath,
            string subPath)
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
                Console.WriteLine("Error:\t" + cmd.Error + "\n"); //有错误信息则打印
                return;
            }
            else
            {
                Console.WriteLine(cmd.Result); //打印结果
                Console.WriteLine("mkdir:" + tarDirPath); //打印结果
            }

            sftp.UploadFile(File.Open(filePath, FileMode.Open), tarFilePath, CL);
            Console.WriteLine("UploadFile:" + filePath + "~ to ~ " + tarFilePath); //打印结果

            var commed2 = $"chmod +x {tarFilePath}";
            var cmd2 = ssh.RunCommand(commed2); //执行指令

            if (!string.IsNullOrEmpty(cmd2.Error))
            {
                Console.WriteLine("Error:\t" + cmd2.Error + "\n"); //有错误信息则打印
                return;
            }
            else
            {
                Console.WriteLine("chmod:" + name); //打印结果
            }
        }

        public void CL(ulong ui)
        {
            // float pre = (long) ui / zipSize;
            //
            // pre = CorrectFloat4(pre) * 100;
            //
            cu++;
            if (cu >= pl)
            {
                cu = 0;
                Console.WriteLine("curr:" + ui + " ~ all:" + zipSize);
            }
        }
        
        public float CorrectFloat4(float value)
        {
            string s = string.Empty;
            float x = value;
            s = string.Format("{0:f2}", x);
            x = Convert.ToSingle(s);
            return x;
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
            ssh.Connect(); //  连接

            while (true) // 循环写入
            {
                Console.Write(">> ");
                var commed = Console.ReadLine(); // 读取指令

                if (string.IsNullOrEmpty(commed)) continue; //指令为空则continue
                if (string.Equals(commed, "exit")) break; //指令为“exit”退出循环

                var cmd = ssh.RunCommand(commed); //执行指令

                //var cmd = ssh.RunCommand(commed).Executed();  这种方式默认返回result,不能打印错误信息

                if (!string.IsNullOrEmpty(cmd.Error))
                {
                    Console.WriteLine("Error:\t" + cmd.Error + "\n"); //有错误信息则打印
                }
                else
                {
                    Console.WriteLine(cmd.Result); //打印结果
                }
            }

            ssh.Disconnect(); //断开连接及文件流
        }
    }
}