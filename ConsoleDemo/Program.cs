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
        static List<IDrive> portable_drives;
        static string CreateTempPath()
        {
            var temp_dir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\";
            if (Directory.Exists(temp_dir + "Temp"))
                temp_dir += "Temp\\";
            temp_dir += "external_drive_temp\\temp-" + DateTime.Now.Ticks;

            Directory.CreateDirectory(temp_dir);
            return temp_dir;
        }

        static void ExampleShowAllPortableDrives()
        {
            Console.WriteLine("Enumerating Portable Drives:");
            try
            {
                portable_drives = PDManager.Instance.Drives.Where(d => d.Type.is_portable()).ToList();
            }
            catch (Exception e) { }

            foreach (var pd in portable_drives)
            {
                Console.WriteLine("Drive Unique ID: " + pd.UniqueId + ", friendly name=" + pd.FriendlyName
                + ", type=" + pd.Type + ", available=" + pd.IsAvailable()
                +", RootName="+pd.RootName);
                //pd.
            }
                if (portable_drives.Count < 1)
                Console.WriteLine("No Portable Drives connected");
        }

        static string FilesCountSuffix(IEnumerable<IFile> files)
        {
            var count = files.Count();
            var suffix = count > 0 ? " - " + count + " files" : "";
            return suffix;
        }

        static void TraverseFolder(IFolder folder, bool dump_file_count_only, int level)
        {
            var suffix = dump_file_count_only ? FilesCountSuffix(folder.Files) : "";
            Console.WriteLine(new string(' ', level * 2) + folder.Name + suffix);
            if (!dump_file_count_only)
                DumpFiles(folder.Files, level);

            foreach (var child in folder.ChildFolders)
                TraverseFolder(child, dump_file_count_only, level + 1);
        }

        static void DumpFiles(IEnumerable<IFile> files, int indent)
        {
            foreach (var f in files)
                Console.WriteLine(new string(' ', indent * 2) + f.Name + ", size=" + f.Size 
                    + ", modified=" + f.LastWriteTime);
        }

        static void TraverseDrive(IDrive d, bool dump_file_count_only)
        {
            Debug.Assert(d.Type.is_portable());

            var suffix = dump_file_count_only ? FilesCountSuffix(d.Files) : "";
            Console.WriteLine("Drive " + d.UniqueId + suffix);
            if (!dump_file_count_only)
                DumpFiles(d.Files, 0);
            foreach (var folder in d.Folders)
                TraverseFolder(folder, dump_file_count_only, 1);
        }

        static void ExampleTraverseFirstPortableDrive(bool dump_file_count_only)
        {
            Console.WriteLine("Traversing First Portable Drive");
            var portable_drives = PDManager.Instance.Drives.Where(d => d.Type.is_portable()).ToList();
            if (portable_drives.Count > 0)
                TraverseDrive(portable_drives[0], dump_file_count_only);
            else
                Console.WriteLine("No Portable Drives connected");
        }

        static void ExampleEnumerateAllAndroidAlbums()
        {
            Console.WriteLine("Enumerating all albums on First Android Drive");
            if (PDManager.Instance.Drives.Any(d => d.Type.is_android()))
            {
                foreach (var folder in PDManager.Instance.ParseFolder("[a0]:/*/dcim").ChildFolders)
                    Console.WriteLine(folder.Name + " - " + folder.Files.Count() + " files");
            }
            else
                Console.WriteLine("No Android Drive Connected");
        }

        static void example_enumerate_all_camera_pics()
        {
            Console.WriteLine("Enumerating all photos from Camera folder");
            var camera = PDManager.Instance.TryParseFolder("[a0]:/*/DCIM/107RICOH");
            if (camera != null)
                foreach (var f in camera.Files)
                    Console.WriteLine(f.Name + ", " + f.Size);
            else
                Console.WriteLine("No Android Drive Connected");
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
            var portable_drives = PDManager.Instance.Drives.Where(d => d.Type.is_portable()).ToList();
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

        static void ExampleWaitForFirstConnectedDevice()
        {
            Console.WriteLine("Waiting for you to plug the first portable device");
            while (true)
            {
                var portable_drives = PDManager.Instance.Drives.Where(d => d.Type.is_portable());
                if (portable_drives.Any())
                    break;
            }
            Console.WriteLine("Waiting for you to make the device availble");
            while (true)
            {
                var d = PDManager.Instance.GetDriveByUniqueId("[p0]:/");
                if (d != null && d.IsAvailable())
                    break;
            }
            ExampleShowAllPortableDrives();
        }

        static void Main(string[] args)
        {

            ExampleShowAllPortableDrives();
            //ExampleWaitForFirstConnectedDevice();

            //bool dump_file_count_only = true;
            //ExampleTraverseFirstPortableDrive(dump_file_count_only);

            //example_enumerate_all_camera_pics();
            //ExampleEnumerateAllAndroidAlbums();
            //ExampleBulkCopyAllCameraPhotosTHdd();
            //ExampleCopyAllCameraPhotosToHddWithProgress();

            //ExampleCopyLatestPhotoToHdd();
            // ExampleFindBiggestPhotoInSize();
            //ExampleFindBiggestFileOnFirstPortableDevice();

            Console.WriteLine();
            Console.WriteLine("Press any key...");
            Console.ReadKey();
        }
    }
}
