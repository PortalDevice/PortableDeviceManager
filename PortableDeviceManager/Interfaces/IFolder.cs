using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortableDeviceManager.Interfaces
{
    public interface IFolder {
        // guaranteed to NOT THROW
        string Name { get; }

        // guaranteed to NOT THROW
        bool Exists { get; }

        string FullPath { get; }

        IDrive Drive { get; }

        // can return null if this is a folder from the drive
        IFolder Parent { get; }

        IEnumerable<IFile> Files { get; }
        IEnumerable<IFolder> ChildFolders { get; }

        // throws if there's an error
        void DeleteAsync();

        // throws if there's an error
        void DeleteSync();
    }

    // this is not exposed - so that users only use IFile.copy() instead
    internal interface IFolder2 : IFolder {
        // this is the only way to make sure a file gets copied where it should, no matter where the destination is
        // (since we could copy a file from android to sd card or whereever)
        void CopyFile(IFile file, bool synchronous);
        
    }
}
