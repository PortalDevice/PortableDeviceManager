using System;
using System.Diagnostics;
using System.Threading.Tasks;
using PortableDeviceManager.Exceptions;
using PortableDeviceManager.Interfaces;
using PortableDeviceManager.Util;
using PortableDeviceManager.Windows;
using Shell32;

namespace PortableDeviceManager.Portable
{
    internal class PortableFile : IFile
    {
        private FolderItem2 fi_;
        private PortableDrive drive_;
        public PortableFile(PortableDrive drive, FolderItem2 fi) {
            drive_ = drive;
            fi_ = fi;
            //Debug.Assert(!fi.IsFolder);
        }

        // for android_folder.copy
        internal FolderItem2 RawFolderItem() {
            return fi_;
        }

        public string Name => fi_.Name;

        public IFolder Folder => new PortableFolder(drive_, (fi_.Parent as Folder2).Self);

        public string FullPath => drive_.ParsePortablePath(fi_);

        public bool Exists {
            get {
                try {
                    if (Drive.IsAvailable()) {
                        // if this throws, drive exists, but file does not
                        PDManager.Instance.ParseFile(FullPath);
                        return true;
                    }
                } catch {
                }
                return false;
            }
        }

        public IDrive Drive => drive_;

        public long Size {
            get {
                return PortableUtil.PortableFile_size(fi_);
            }
        }

        public DateTime LastWriteTime {
            get {
                try {
                    var dt = (DateTime)fi_.ExtendedProperty("write");
                    return dt;
                } catch {
                }
                try {
                    // this will return something like "5/11/2017 08:29"
                    var date_str = (fi_.Parent as Folder).GetDetailsOf(fi_, 3).ToLower();
                    var dt_backup = DateTime.Parse(date_str);
                    return dt_backup;
                } catch {
                    return DateTime.MinValue;
                }
            }
        }

        public void CopyAsync(string dest_path) {
            var dest = PDManager.Instance.ParseFolder(dest_path) as IFolder2;
            if ( dest != null)
                dest.CopyFile(this, false);
            else 
                throw new PDException("destination path does not exist: " + dest_path);
        }

        
        public void DeleteAsync() {
            Task.Run( () => WinUtil.DeleteSyncPortableFile(fi_));
        }

        public void CopySync(string dest_path) {
            var dest = PDManager.Instance.ParseFolder(dest_path) as IFolder2;
            if ( dest != null)
                dest.CopyFile(this, true);
            else 
                throw new PDException("destination path does not exist: " + dest_path);
        }

        public void DeleteSync() {
            WinUtil.DeleteSyncPortableFile(fi_);
        }
    }
}
