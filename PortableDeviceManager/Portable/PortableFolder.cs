using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using PortableDeviceManager.Interfaces;
using PortableDeviceManager.Util;
using PortableDeviceManager.Windows;
using Shell32;

namespace PortableDeviceManager.Portable
{
    // https://blog.dotnetframework.org/2014/12/10/read-extended-properties-of-a-file-in-c/ -> this gets properties of a folder

    internal class PortableFolder : IFolder2 {

        private FolderItem fi_;
        private PortableDrive drive_;

        private bool enumerated_children_ = false;
        private List<IFolder> folders_ = new List<IFolder>();
        private List<IFile> files_ = new List<IFile>();

        public PortableFolder(PortableDrive drive,FolderItem fi) {
            drive_ = drive;
            fi_ = fi;
            Debug.Assert(fi.IsFolder);
        }

        public string Name => fi_.Name;

        public bool Exists {
            get {
                try {
                    if (Drive.IsAvailable()) {
                        // if this throws, drive exists, but folder does not
                        PDManager.Instance.ParseFolder(FullPath);
                        return true;
                    }
                } catch {
                }
                return false;
            }
        }

        // for Bulk copy
        public FolderItem RawFolderItem() {
            return fi_;
        }

        public string FullPath {
            get {
                return drive_.ParsePortablePath(fi_);
            }
        }
        public IDrive Drive {
            get { return drive_; }
        }

        public IFolder Parent => new PortableFolder(drive_, (fi_.Parent as Folder2).Self);

        public IEnumerable<IFile> Files {
            get {
                if (!enumerated_children_) {
                    enumerated_children_ = true;
                    PortableUtil.EnumerateChildren(drive_, fi_, folders_, files_);
                }
                return files_;
            }
        }
        public IEnumerable<IFolder> ChildFolders {
            get {
                if (!enumerated_children_) {
                    enumerated_children_ = true;
                    PortableUtil.EnumerateChildren(drive_, fi_, folders_, files_);
                }
                return folders_;
            }
        }


        public void DeleteAsync() {
            var full = FullPath;
            Task.Run( () => WinUtil.DeleteSyncPortableFolder(fi_, full));
        }

        public void DeleteSync() {
            var full = FullPath;
            WinUtil.DeleteSyncPortableFolder(fi_, full);
        }


        public void CopyFile(IFile file, bool synchronous) {
            var copy_options = 4 | 16 | 512 | 1024 ;
            var andoid = file as PortableFile;
            var win = file as WinFile;
            // it can either be android or windows
            Debug.Assert(andoid != null || win != null);
            FolderItem dest_item = null;
            var souce_name = file.Name;
            if (andoid != null) 
                dest_item = andoid.RawFolderItem();
            else if (win != null) {
                var WinFile_name = new FileInfo(win.FullPath);

                var shell_folder = WinUtil.GetShell32Folder(WinFile_name.DirectoryName);
                var shell_file = shell_folder.ParseName(WinFile_name.Name);
                Debug.Assert(shell_file != null);
                dest_item = shell_file;
            }

            // Windows stupidity - if file exists, it will display a stupid "Do you want to replace" dialog,
            // even if we speicifically told it not to (via the copy options)
            //
            // so, if file exists, delete it first
            var existing_name = (fi_.GetFolder as Folder).ParseName(souce_name);
            if ( existing_name != null)
                WinUtil.DeleteSyncPortableFile(existing_name);

            (fi_.GetFolder as Folder).CopyHere(dest_item, copy_options);
            if ( synchronous)
                WinUtil.WaitForPortableCopyComplete(FullPath + "\\" + souce_name, file.Size);
        }


    }
}
