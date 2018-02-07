using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileHelper
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = @"D:\";
            string path2 = @"D:\tableName.txt";
            bool ifContinue = true;
            do
            {
                Console.WriteLine("选择方法：");
                string method = Console.ReadLine();
                switch (method)
                {
                    case "get1": DirectoryHelper.GetFilesName(path); break;
                    case "get2": DirectoryHelper.GetFilesName2(path); break;
                    case "read1": ReadFileHelper.Read(path2); break;
                    case "read2": ReadFileHelper.Read2(path2); break;
                    case "read3": ReadFileHelper.Read3(path2); break;
                    case "write1":WriteFileHelper.Write(path);break;
                    case "write2":WriteFileHelper.Write(path);break;
                    case "write3":WriteFileHelper.Write(path);break;
                    case "结束": ifContinue = false; break;
                    default: break;
                }
            } while (ifContinue);
            Console.Read();
        }
    }
    /// <summary>
    /// 获取文件路径
    /// </summary>
    static class DirectoryHelper
    {
        public static void GetFilesName(string path)
        {
            var files = Directory.GetFiles(path, "*.txt");
            foreach (var file in files)
            {
                Console.WriteLine(file);
            }
        }
        /// <summary>
        /// 此方法较好，获取了目录下每个文件的所有信息
        /// </summary>
        public static void GetFilesName2(string path)
        {
            DirectoryInfo folder = new DirectoryInfo(path);
            foreach (FileInfo file in folder.GetFiles("*.txt"))
            {
                Console.WriteLine(file.FullName);
            }
        }
    }
    /// <summary>
    /// 读取文件
    /// </summary>
    static class ReadFileHelper
    {
        public static byte[] byData = new byte[100];
        public static char[] charData = new char[1000];
        public static void Read(string path)
        {
            try
            {
                FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read);
                file.Seek(0, SeekOrigin.Begin);
                for (int i = 0; i < Math.Ceiling(file.Length / 100.0); i++)
                {
                    file.Read(byData, 0, 100);
                    //将已编码的字节序列转换为一组字符
                    Decoder d = Encoding.Default.GetDecoder();
                    d.GetChars(byData, 0, byData.Length, charData, 0);
                    Console.Write(charData);
                }
                file.Close();
            }
            catch (IOException e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        public static void Read2(string path)
        {
            StreamReader sr = new StreamReader(path, Encoding.Default);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                Console.WriteLine(line.ToString());
            }
        }
        public static void Read3(string path)
        {
            String[] files = File.ReadAllLines(path, Encoding.Default);
            foreach (var file in files)
            {
                Console.WriteLine(file.ToString());
            }
        }
    }
    /// <summary>
    /// 写文件
    /// </summary>
    static class WriteFileHelper
    {
        public static void Write(string path)
        {
            FileStream fs = new FileStream(path, FileMode.Create);
            byte[] data = System.Text.Encoding.Default.GetBytes("Hello World!");
            fs.Write(data, 0, data.Length);
            fs.Flush();
            fs.Close();
            Console.WriteLine("OK");
        }
        public static void Write2(string path)
        {
            FileStream fs = new FileStream(path, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);
            sw.Write("Hello World!");
            sw.Flush();
            sw.Close();
            fs.Close();
            Console.WriteLine("OK");
        }
        public static void Write3(string path)
        {
            File.WriteAllText(path, "Hello World!", Encoding.Unicode);
            Console.WriteLine("OK");
        }
    }
}
