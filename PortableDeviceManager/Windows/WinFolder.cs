using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PortableDeviceManager.Exceptions;
using PortableDeviceManager.Interfaces;
using PortableDeviceManager.Portable;
using Shell32;

namespace PortableDeviceManager.Windows
{
    class WinFolder : IFolder2 {

        private string parent_, name_;
        public WinFolder(string parent_folder, string FolderName) {
            parent_ = parent_folder;
            name_ = FolderName;

            Debug.Assert(!parent_.EndsWith("\\") || ParentIsDrive());
            // drive len is 3
            Debug.Assert(parent_.Length >= 3);
        }
        public string Name {
            get { return name_; }
        }

        public bool Exists => Directory.Exists(FolderName());

        public string FullPath => FolderName();
        public IDrive Drive => new WinDrive(parent_.Substring(0,3));

        private bool ParentIsDrive() {
            return parent_.Length <= 3;
        }

        public IFolder Parent {
            get {
                if (ParentIsDrive())
                    return null;
                var di = new DirectoryInfo(parent_);
                return new WinFolder(di.Parent.FullName, di.Name);
            }
        }

        private string FolderName() {
            return parent_ + (ParentIsDrive() ? "" : "\\") + name_;
        }

        public IEnumerable<IFile> Files {
            get {
                var fn = FolderName();
                return new DirectoryInfo(fn).EnumerateFiles().Select(f => new WinFile(fn, f.Name));
            }
        }

        public IEnumerable<IFolder> ChildFolders {
            get {
                var fn = FolderName();
                return new DirectoryInfo(fn).EnumerateDirectories().Select(f => new WinFolder(fn, f.Name));
            }
        }

        public void DeleteAsync() {
            Task.Run(() => DeleteSync());
        }

        public void DeleteSync() {
            Directory.Delete(FolderName(), true);
        }

        public void CopyFile(IFile file, bool synchronous) {
            var copy_options = 4 | 16 | 512 | 1024;
            var andoid = file as PortableFile;
            var win = file as WinFile;
            // it can either be android or windows
            //Debug.Assert(andoid != null || win != null);

            var fn = FolderName();
            var dest_path = fn + "\\" + file.Name;
            if (win != null) {
                if (synchronous)
                    File.Copy(file.FullPath, dest_path, true);
                else
                    Task.Run(() => File.Copy(file.FullPath, dest_path, true));
            } else if (andoid != null) {
                // android file to windows:

                // Windows stupidity - if file exists, it will display a stupid "Do you want to replace" dialog,
                // even if we speicifically told it not to (via the copy options)
                if (File.Exists(dest_path))
                    File.Delete(dest_path);
                var shell_folder = WinUtil.GetShell32Folder(fn);
                shell_folder.CopyHere(andoid.RawFolderItem(), copy_options);
                //logger.Debug("winfolder: CopyHere complete");
                if ( synchronous)
                    WinUtil.WaitForWinCopyComplete(file.Size, dest_path);
            }
        }


    }
}
