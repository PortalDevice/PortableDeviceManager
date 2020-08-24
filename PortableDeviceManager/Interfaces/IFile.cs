using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortableDeviceManager.Interfaces {
    public interface IFile {
        // guaranteed to NOT THROW
        string Name { get; }

        IFolder Folder { get; }
        string FullPath { get; }

        // guaranteed to NOT THROW
        bool Exists { get; }

        IDrive Drive { get; }

        long Size { get; }
        DateTime LastWriteTime { get; }

        // note: dest_path can be to another external drive
        // throws if there's an error
        //
        // note: move can be implemented via copy() + delete()
        //
        // note: overwrites if destination exists
        void CopyAsync(string dest_path);
        // throws if there's an error
        void DeleteAsync();

        void CopySync(string dest_path);
        void DeleteSync();

    }

}

