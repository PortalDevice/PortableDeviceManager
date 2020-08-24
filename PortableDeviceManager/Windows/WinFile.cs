using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PortableDeviceManager.Exceptions;
using PortableDeviceManager.Interfaces;

namespace PortableDeviceManager.Windows
{
    class WinFile : IFile {
        private string path_;
        private string name_;
        public WinFile(string path, string name) {
            Debug.Assert(!path.EndsWith("\\"));
            path_ = path;
            name_ = name;
            // drive len is 3
            Debug.Assert(path_.Length >= 3);
        }

        public string Name => name_;

        public IFolder Folder {
            get {
                var di = new DirectoryInfo(path_);
                return new WinFolder( di.Parent.FullName, di.Name );
            }
        }

        public string FullPath => path_ + "\\" + name_;

        public bool Exists => File.Exists(FullPath);
        public IDrive Drive => new WinDrive(path_.Substring(0,3));

        public long Size => new FileInfo(FullPath).Length;
        public DateTime LastWriteTime => new FileInfo(FullPath).LastWriteTime;

        public void CopyAsync(string dest_path) {
            var dest = PDManager.Instance.ParseFolder(dest_path) as IFolder2;
            if ( dest != null)
                dest.CopyFile(this, false);
            else 
                throw new PDException("destination path does not exist: " + dest_path);
        }

        public void CopySync(string dest_path) {
            var dest = PDManager.Instance.ParseFolder(dest_path) as IFolder2;
            if ( dest != null)
                dest.CopyFile(this, true);
            else 
                throw new PDException("destination path does not exist: " + dest_path);
        }

        public void DeleteAsync() {
            File.Delete(FullPath);
        }

        public void DeleteSync() {
            File.Delete(FullPath);
        }


    }
}
