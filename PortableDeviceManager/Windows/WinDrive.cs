using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PortableDeviceManager.Exceptions;
using PortableDeviceManager.Interfaces;

namespace PortableDeviceManager.Windows
{
    /* note: the main reason we have win drives is so that you can copy from a windows drive to an android drive
     */
    class WinDrive : IDrive {

        private string root_;
        private bool valid_ = true;

        public WinDrive(DriveInfo di) {
            try {
                root_ = di.RootDirectory.FullName;
            } catch (PDException e) {
                // "bad drive " + di + " : " + e;
                valid_ = false;
            }
        }

        public WinDrive(string root) {
            root_ = root;
        }

        public bool IsConnected() {
            return true;
        }

        public bool IsAvailable() {
            return true;
        }

        public EnumDriveType Type {
            get { return EnumDriveType.INTERNAL_HDD; }
        }

        public string RootName {
            get { return root_; }
        }

        public string UniqueId {
            get { return root_; }
        } 
        public string FriendlyName {
            get { return root_; }
        }

        public IEnumerable<IFolder> Folders {
            get { return new DirectoryInfo(root_).EnumerateDirectories().Select(f => new WinFolder(root_, f.Name)); }
        }
        public IEnumerable<IFile> Files {
            get { return new DirectoryInfo(root_).EnumerateFiles().Select(f => new WinFile(root_, f.Name)); }
        }

        public IFile ParseFile(string path) {
            var f = TryParseFile(path);
            if ( f == null)
                throw new PDException("invalid path " + path);
            return f;
        }

        public IFolder ParseFolder(string path) {
            var f = TryParseFolder(path);
            if ( f == null)
                throw new PDException("invalid path " + path);
            return f;
        }

        public IFile TryParseFile(string path) {
            path = path.Replace("/", "\\");
            var contains_drive_prefix = path.StartsWith(root_, StringComparison.CurrentCultureIgnoreCase);
            var full = contains_drive_prefix ? path : root_ + path;
            if (File.Exists(full)) {
                var fi = new FileInfo(full);
                return new WinFile(fi.DirectoryName, fi.Name);
            }
            return null;
        }

        public IFolder TryParseFolder(string path) {
            path = path.Replace("/", "\\");
            var contains_drive_prefix = path.StartsWith(root_, StringComparison.CurrentCultureIgnoreCase);
            var full = contains_drive_prefix ? path : root_ + path;
            if (Directory.Exists(full)) {
                var fi = new DirectoryInfo(full);
                return new WinFolder(fi.Parent.FullName, fi.Name);
            }
            return null;
        }

        public IFolder CreateFolder(string path) {
            path = path.Replace("/", "\\");
            if (path.EndsWith("\\"))
                path = path.Substring(0, path.Length - 1);
            var contains_drive_prefix = path.StartsWith(root_, StringComparison.CurrentCultureIgnoreCase);
            if (!contains_drive_prefix)
                path = root_ + path;
            Directory.CreateDirectory(path);

            return ParseFolder(path);
        }
    }
}
