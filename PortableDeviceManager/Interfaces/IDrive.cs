using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortableDeviceManager.Interfaces
{
    public enum EnumDriveType {
        PORTABLE,
        // if this, we're not sure if it's phone or tablet or whatever
        ANDROID_UNKNOWN, 
        // it's an android phone
        ANDROID_PHONE, 
        // it's an android tablet
        ANDROID_TABLET, 

        IOS_UNKNOWN,
        IPHONE,
        IPAD,

        // SD Card
        // FIXME can i know if it's read-only?
        SD_CARD, 
        // external hard drive
        // FIXME can i know if it's read-only?
        EXTERNAL_HDD,

        // it's the Windows HDD 
        INTERNAL_HDD,

        // FIXME this is to be treated read-only!!!
        CD_ROM,
    }

    public static class DriveTypeOS {
        public static bool IsAndroid(this EnumDriveType dt) {
            return dt == EnumDriveType.ANDROID_UNKNOWN 
                || dt == EnumDriveType.ANDROID_PHONE 
                || dt == EnumDriveType.ANDROID_TABLET;
        }

        public static bool IsPortable(this EnumDriveType dt) {
            return dt == EnumDriveType.ANDROID_UNKNOWN 
                || dt == EnumDriveType.ANDROID_PHONE 
                || dt == EnumDriveType.ANDROID_TABLET 
                || dt == EnumDriveType.PORTABLE
                || dt == EnumDriveType.IPHONE 
                || dt == EnumDriveType.IPAD 
                || dt == EnumDriveType.IOS_UNKNOWN;
        }

        public static bool IsIOS(this EnumDriveType dt) {
            return dt == EnumDriveType.IPHONE 
                || dt == EnumDriveType.IPAD 
                || dt == EnumDriveType.IOS_UNKNOWN;
        }
        public static bool IsInternalHDD(this EnumDriveType dt) {
            return dt == EnumDriveType.INTERNAL_HDD;
        }

        public static bool IsExternalHDD(this EnumDriveType dt)
        {
            return dt == EnumDriveType.EXTERNAL_HDD;
        }
    };

    public interface IDrive {
        // returns true if the drive is connected
        // note: not as a property, since this could actually take time to find out - we don't want to break debugging
        bool IsConnected();

        // returns true if the drive is available - note that the drive can be connected via USB, but locked (thus, not available)
        bool IsAvailable();

        EnumDriveType Type { get; }

        // this is the drive path, such as "c:\" - however, for non-conventional drives, it can be a really weird path
        string RootName { get; }

        // the drive's Unique ID - it is the same between program runs
        string UniqueId { get; }

        // a friendly name for the drive
        string FriendlyName { get; }

        IEnumerable<IFolder> Folders { get; }
        IEnumerable<IFile> Files { get; }

        // throws on failure
        IFile ParseFile(string path);
        IFolder ParseFolder(string path);

        // returns null on failure
        IFile TryParseFile(string path);
        IFolder TryParseFolder(string path);

        // creates the full path to the folder
        IFolder CreateFolder(string folder);
    }

}
