////////////////////////////////////////////////////////////////////////////////
///  
/// 说明：此程序为控制台测试程序，用于测试PortableDeviceManager(PDManager)控制
/// 作者：李光强
/// 时间：2020/8/25/（七夕日）
/// 
////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PortableDeviceManager;
using PortableDeviceManager.Bulk;
using PortableDeviceManager.Interfaces;
using PortableDeviceManager.Monitor;
using PortableDeviceManager.Util;

namespace ConsoleDemo
{
    class Program
    {
        /// <summary>
        /// 移动设备列表
        /// </summary>
        static List<IDrive> portable_drives;

        /// <summary>
        /// 照片存储目录
        /// </summary>
        static string imageFolder;

        /// <summary>
        /// 照片格式（后缀名)
        /// </summary>
        static string imageFormat;

        /// <summary>
        /// 控制是否仅仅显示照片
        /// </summary>
        static bool listOnlyImage = false;

        #region 主函数
        static void Main(string[] args)
        {
            GetSetting();

            DisplayMenu();
            
        }

        /// <summary>
        /// 从app.config中读取参数
        /// </summary>
        static void GetSetting()
        {
            imageFolder = ConfigurationManager.AppSettings["ImageFolder"];
            imageFormat = ConfigurationManager.AppSettings["ImageFormat"];
        }

        /// <summary>
        /// 显示菜单
        /// </summary>
        static private void DisplayMenu()
        {
            Console.WriteLine("\n");
            Console.WriteLine("=======================================\n");
            Console.WriteLine("Portable Device Sample Console Application");
            Console.WriteLine("  Copyright by Li Guangqiang, 2020/8/25/ \n");
            Console.WriteLine("=======================================\n");
            Console.WriteLine("0.  Detect Device");
            Console.WriteLine("1.  Traverse Portable Drive and Display folder and its file number");
            Console.WriteLine("2.  Traverse Portable Drive and Display folders and its files");
            Console.WriteLine("3.  List Files in a inputed folder");
            Console.WriteLine("4.  List All Images");
            
            Console.WriteLine("x.  Exit");
            Console.Write("> ");

            DoMenu();
        }

        /// <summary>
        /// 处理菜单选择
        /// </summary>
        static void DoMenu()
        {
            string selectionIndex = Console.ReadLine();

            switch (selectionIndex)
            {
                case "0":                   //查询连接的设备                    
                    SearchPortableDrives();
                    break;
                case "1":                   //遍历文件数量
                    TraversePortableDrive(true);
                    break;
                case "2":                   //遍历文件
                    TraversePortableDrive(false);
                    break;
                case "3":                   //显示指定目录中的所有文件
                    EnumerateFileInFolder();
                    break;
                case "4":
                    EnumeratePhotos();
                    break;
                case "x":
                case "X":
                    Process.GetCurrentProcess().Kill();
                    break;
            }            
        }
        #endregion

        #region 测试函数
        /// <summary>
        /// 查询所有连接的设备
        /// </summary>
        static void SearchPortableDrives()
        {
            Console.WriteLine("Enumerating Portable Drives:");
            try
            {
                portable_drives = PDManager.Instance.Drives.Where(d => d.Type.IsPortable()).ToList();
            }
            catch (Exception e) { }
            int i = 0;
            foreach (var pd in portable_drives)
            {
                Console.WriteLine("Index:"+(i++)+"\n \tDrive Unique ID: " + pd.UniqueId + ", friendly name=" + pd.FriendlyName
                + ", type=" + pd.Type + ", available=" + pd.IsAvailable()
                +", RootName="+pd.RootName+"\n");
                //pd.
            }
            if (portable_drives.Count < 1)
                Console.WriteLine("No Portable Drives connected");

            DisplayMenu();
        }
        
        /// <summary>
        /// 遍历移动设备中的文件夹和文件
        /// </summary>
        /// <param name="dump_file_count_only">仅遍历文件数</param>
        static void TraversePortableDrive(bool dump_file_count_only)
        {
            Console.WriteLine("\r\n----------------------------------------");
            Console.WriteLine("Traverse the portable drive.");

            if (CheckDrivers()){
                short index = InputDriveIndex();
                TraverseDrive(portable_drives[index], dump_file_count_only);
            }
            DisplayMenu();
        }

        /// <summary>
        /// 列举指定目录中的全部文件
        /// </summary>
        static void EnumerateFileInFolder()
        {
            Console.WriteLine("\r\n----------------------------------------");
            Console.WriteLine("Enumerating all files from the inputed folder.");

            if (CheckDrivers() == false) return;
            short index = InputDriveIndex();
            IDrive drive = portable_drives[index];

            Console.WriteLine("Please input a folder name:");
            string name = Console.ReadLine();

            IFolder dest=null;
            //迭代查找目录
            foreach (IFolder folder in drive.Folders)
            {
                dest = FindFolder(folder, name);
                if (dest != null) break;
            }

            if (dest == null)
            {
                Console.WriteLine("The folder [" + name + "] is not found in the drive [" + drive.UniqueId + "].");
                
            }
            else
            {
                Console.WriteLine("\n ---------------------------");
                Console.WriteLine("Files in the " + name + " folder ["+dest.Files.Count()+"] : ");
                DumpFiles(dest.Files,1);
            }

            DisplayMenu();

        }

        /// <summary>
        /// 列举所有相机中的图片
        /// </summary>
        static void EnumeratePhotos()
        {
            Console.WriteLine("\r\n----------------------------------------");
            
            if (CheckDrivers() == false) return;
            short index = InputDriveIndex();
            IDrive drive = portable_drives[index];

            Console.WriteLine("Please enter the name of the folder containing the picture ["+ imageFolder+"]:");
            string str = Console.ReadLine();
            if (str != "")
                imageFolder = str;

            IFolder dest = null;
            //迭代查找目录
            foreach (IFolder folder in drive.Folders)
            {
                dest = FindFolder(folder, imageFolder);
                if (dest != null) break;
            }

            if (dest != null)
            {
                //设置仅仅显示照片
                listOnlyImage = true;
                TraverseFolder(dest, false, 0);
                listOnlyImage = false;
            }
            else
            {
                Console.WriteLine("\n ## The folder ["+imageFolder+"] not found !");                
            }
                        
            DisplayMenu();
        }

        /// <summary>
        /// 遍历图库
        /// </summary>
        static void EnumerateAlbums()
        {
            if (CheckDrivers() == false) return;

            Console.WriteLine("Enumerating albums on a portable drive.");
            Int16 index = InputDriveIndex();
            IDrive drive = portable_drives[index];

            var folders = drive.ParseFolder("/*/DCIM");
            
                foreach (var folder in folders.ChildFolders)
                    Console.WriteLine(folder.Name + " - " + folder.Files.Count() + " files");
            
        }

        #endregion

        #region 其它函数
        /// <summary>
        /// 检查是否有连接的移动设备
        /// </summary>
        /// <returns></returns>
        static bool CheckDrivers()
        {
            if (portable_drives == null)
                SearchPortableDrives();

            if (portable_drives.Count == 0)
            {
                Console.WriteLine("No portable device connected!");
                return false;
            }
            return true;
        }

        static Int16 InputDriveIndex()
        {
            string s;
            Int16 index;
            Console.Write("\r\nInput the Portable Driver index[0-" + (portable_drives.Count - 1) + ", x-Exit] : ");
            while (true)
            {
                s = Console.ReadLine();
                if (Int16.TryParse(s, out index) == false)
                {
                    if (s == "x")  //返回主菜单
                    {
                        DisplayMenu();
                    }
                    Console.WriteLine("#The inputed char is not a valide number. Please input a number [0-" + (portable_drives.Count - 1) + "] : "); 
                }
                else if (index >= 0 && index < portable_drives.Count)
                    break;
                else
                    Console.WriteLine("#Your inputed number is not valide. Please input a number [0-" + (portable_drives.Count - 1) + "] : ");
            }
            return index;
        }
        
        /// <summary>
        /// 创建临时文件夹
        /// </summary>
        /// <returns></returns>
        static string CreateTempPath()
        {
            var temp_dir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\";
            if (Directory.Exists(temp_dir + "Temp"))
                temp_dir += "Temp\\";
            temp_dir += "external_drive_temp\\temp-" + DateTime.Now.Ticks;

            Directory.CreateDirectory(temp_dir);
            return temp_dir;
        }
    static string FilesCountSuffix(IEnumerable<IFile> files)
        {
            var count = files.Count();
            var suffix = count > 0 ? " - " + count + " files" : "";
            return suffix;
        }       

        /// <summary>
        /// 遍历移动设备文件数
        /// </summary>
        /// <param name="d">设备</param>
        /// <param name="dump_file_count_only">是否仅显示文件数</param>
        static void TraverseDrive(IDrive d, bool dump_file_count_only)
        {           
            //首先查询当前目录下的文件数量
            var suffix = dump_file_count_only ? FilesCountSuffix(d.Files) : "";
            Console.WriteLine("Drive ID : " + d.UniqueId + suffix +"\tName : "+d.FriendlyName);
            if (!dump_file_count_only)
                DumpFiles(d.Files, 0);

            //然后迭代查询所有子目录的文件数
            foreach (var folder in d.Folders)
                TraverseFolder(folder, dump_file_count_only, 1);
        }

        /// <summary>
        /// 遍历文件夹
        /// </summary>
        /// <param name="folder">要遍历的文件夹</param>
        /// <param name="dump_file_count_only">是否仅计算文件数</param>
        /// <param name="level"></param>
        static void TraverseFolder(IFolder folder, bool dump_file_count_only, int level)
        {
            var suffix = dump_file_count_only ? FilesCountSuffix(folder.Files) : "";
            Console.WriteLine(new string(' ', level * 2) + "["+folder.Name +"]"+ suffix);
            if (!dump_file_count_only)
                DumpFiles(folder.Files, level);

            //迭代遍历所有子目录中的文件数量
            foreach (var child in folder.ChildFolders)
                TraverseFolder(child, dump_file_count_only, level + 1);
        }

        /// <summary>
        /// 遍历文件
        /// </summary>
        /// <param name="files">文件列表</param>
        /// <param name="indent">缩进量</param>
        static void DumpFiles(IEnumerable<IFile> files, int indent)
        {
            foreach (var f in files)
            {
                if ((!listOnlyImage || f.Name.ToUpper().Contains(imageFormat.ToUpper())))
                        Console.WriteLine("\t" + new string(' ', indent * 2) + f.Name + ", size=" + f.Size
                            + ", modified=" + f.LastWriteTime);                

            }
        }

        /// <summary>
        /// 迭代查询指定的文件名称
        /// </summary>
        /// <param name="folder">目标目录及子目录</param>
        /// <param name="name">要查询的目录名称</param>
        /// <returns></returns>
        static IFolder FindFolder(IFolder folder,string name)
        {
            System.Diagnostics.Debug.WriteLine("##Folder name:" + folder.Name);
            if (folder.Name == name) 
                return folder;

            IFolder fi=null;
            //然后迭代查询所有子目录的文件数
            foreach (var f in folder.ChildFolders)
            {
                fi = FindFolder(f, name);
                if (fi != null) return fi;
            }

            return fi;
        }

        // 1.2.4+ we have callbacks - called after each file is copied
        static void ExampleBulkCopyAllCameraPhotosTHdd()
        {
            Console.WriteLine("Copying all photos you took on your first Android device");
            var camera = PDManager.Instance.TryParseFolder("[a0]:/*/DCIM/107RICOH");
            if (camera != null)
            {
                DateTime start = DateTime.Now;
                var temp = CreateTempPath();
                Console.WriteLine("Copying to " + temp);
                Bulk.BulkCopySync(camera.Files.ToList(), temp, (f, i, c) => {
                    Console.WriteLine(f + " to " + temp + "(" + (i + 1) + " of " + c + ")");
                });
                Console.WriteLine("Copying to " + temp + " - complete, took " + (int)(DateTime.Now - start).TotalMilliseconds + " ms");
            }
            else
                Console.WriteLine("No Android Drive Connected");
        }

        /* Note: this shows progress (that is, after each copied file). However, it will be slower than the bulk copy
         * (bulk copying can do some optimizations, but for now, you don't kwnow the progress)
         */
        static void ExampleCopyAllCameraPhotosToHddWithProgress()
        {
            Console.WriteLine("Copying all photos you took on your first Android device");
            var camera = PDManager.Instance.TryParseFolder("[a0]:/*/DCIM/107RICOH");
            if (camera != null)
            {
                var temp = CreateTempPath();
                DateTime start = DateTime.Now;
                var files = camera.Files.ToList();
                var idx = 0;
                foreach (var file in files)
                {
                    Console.Write(file.FullPath + " to " + temp + "(" + ++idx + " of " + files.Count + ")");
                    file.CopySync(temp);
                    Console.WriteLine(" ...done");
                }
                Console.WriteLine("Copying to " + temp + " - complete, took " + (int)(DateTime.Now - start).TotalMilliseconds + " ms");
            }
            else
                Console.WriteLine("No Android Drive Connected");
        }

        static void ExampleCopyLatestPhotoToHdd()
        {
            Console.WriteLine("Copying latest photo from your Android device to HDD");
            var camera = PDManager.Instance.TryParseFolder("[a0]:/*/DCIM/107RICOH");
            if (camera != null)
            {
                var temp = CreateTempPath();
                var files = camera.Files.OrderBy(f => f.LastWriteTime).ToList();
                if (files.Count > 0)
                {
                    var latest_file = files.Last();
                    Console.WriteLine("Copying " + latest_file.FullPath + " to " + temp);
                    latest_file.CopySync(temp);
                    Console.WriteLine("Copying " + latest_file.FullPath + " to " + temp + " - complete");
                }
                else
                    Console.WriteLine("You have no Photos");
            }
            else
                Console.WriteLine("No Android Drive Connected");
        }

        static void ExampleFindBiggestPhotoInSize()
        {
            Console.WriteLine("Copying latest photo from your Android device to HDD");
            var camera = PDManager.Instance.TryParseFolder("[a0]:/*/DCIM/107RICOH");
            if (camera != null)
            {
                var files = camera.Files.ToList();
                if (files.Count > 0)
                {
                    var max_size = files.Max(f => f.Size);
                    var biggest_file = files.First(f => f.Size == max_size);
                    Console.WriteLine("Your biggest photo is " + biggest_file.FullPath + ", size=" + biggest_file.Size);
                }
                else
                    Console.WriteLine("You have no Photos");
            }
            else
                Console.WriteLine("No Android Drive Connected");
        }

        static long FileSize(IFile f)
        {
            return f?.Size ?? 0;
        }
        static IFile GetBiggestFile(List<IFile> files)
        {
            if (files.Count < 1)
                return null;
            var max_size = files.Max(f => f.Size);
            var biggest_file = files.First(f => f.Size == max_size);
            return biggest_file;
        }

        static IFile GetBiggestFile(IFolder folder)
        {
            var files = folder.Files.ToList();
            if (files.Count < 1)
                return null;
            var max_size = files.Max(f => f.Size);
            var biggest_file = files.First(f => f.Size == max_size);
            return biggest_file;
        }

        static IFile GetBiggestFileRecursive(IFolder folder)
        {
            var biggest = GetBiggestFile(folder);
            foreach (var child in folder.ChildFolders)
            {
                var child_biggest = GetBiggestFileRecursive(child);
                if (FileSize(biggest) < FileSize(child_biggest))
                    biggest = child_biggest;
            }
            return biggest;
        }

        static IFile GetBiggestFile(IDrive d)
        {
            IFile biggest = GetBiggestFile(d.Files.ToList());
            foreach (var child in d.Folders)
            {
                var child_biggest = GetBiggestFileRecursive(child);
                if (FileSize(biggest) < FileSize(child_biggest))
                    biggest = child_biggest;
            }
            return biggest;
        }


        static void ExampleFindBiggestFileOnFirstPortableDevice()
        {
            Console.WriteLine("Findind biggest file on First Portable Device");
            var portable_drives = PDManager.Instance.Drives.Where(d => d.Type.IsPortable()).ToList();
            if (portable_drives.Count > 0)
            {
                if (portable_drives[4].IsAvailable())
                {
                    var biggest = GetBiggestFile(portable_drives[4]);
                    if (biggest != null)
                        Console.WriteLine("Biggest file on device " + biggest.FullPath + ", " + biggest.Size);
                    else
                        Console.WriteLine("You have no files on device");
                }
                else Console.WriteLine("First Portable device is not available");
            }
            else
                Console.WriteLine("No Portable Drives connected");
        }

        #endregion


    }
}
