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
using PortableDeviceManager.Monitor;
using PortableDeviceManager.Portable;
using PortableDeviceManager.Util;
using PortableDeviceManager.Windows;
using Shell32;

namespace PortableDeviceManager
{
    /* the root - the one that contains all external drives 
     */
    public class PDManager
    {

        public static PDManager Instance { get; } = new PDManager();

        private bool AutoCloseWinDialogs_ = true;

        // note: not all devices register as USB hubs, some only register as controller devices
        private DeviceMonitor monitor_usbhub_devices_ = new DeviceMonitor ();
        private DeviceMonitor monitor_controller_devices_ = new DeviceMonitor ();
        
        private Dictionary<string,string> vidpid_to_unique_id_ = new Dictionary<string, string>();

        private PDManager() {
            var existing_devices = DeviceFinder.FindObjects("Win32_USBHub");
            foreach (var device in existing_devices) {
                if (device.ContainsKey("PNPDeviceID")) {
                    var device_id = device["PNPDeviceID"];
                    string VidPid = "", UniqueId = "";
                    if (USBUtil.pnp_device_id_to_vidpid_and_unique_id(device_id, ref VidPid, ref UniqueId)) {
                        lock(this)
                            vidpid_to_unique_id_.Add(VidPid, UniqueId);
                    }
                }
            }
            var existing_controller_devices = DeviceFinder.FindObjects("Win32_USBControllerDevice");
            foreach (var device in existing_controller_devices) {
                if (device.ContainsKey("Dependent")) {
                    var device_id = device["Dependent"];
                    string VidPid = "", UniqueId = "";
                    if (USBUtil.dependent_to_vidpid_and_unique_id(device_id, ref VidPid, ref UniqueId)) {
                        lock(this)
                            if ( !vidpid_to_unique_id_.ContainsKey(VidPid))
                                vidpid_to_unique_id_.Add(VidPid, UniqueId);
                    }
                }
            }

            Refresh();

            monitor_usbhub_devices_.DeviceAddedEvent += DeviceAdded;
            monitor_usbhub_devices_.DeviceDeletedEvent += DeviceRemoved;
            monitor_usbhub_devices_.Monitor("Win32_USBHub");

            monitor_controller_devices_.DeviceAddedEvent += DeviceAddedConroller;
            monitor_controller_devices_.DeviceDeletedEvent += DeviceRemovedConroller;
            monitor_controller_devices_.Monitor("Win32_USBControllerDevice");

            new Thread(Win32Util.check_for_dialogs_thread) {IsBackground = true}.Start();
        }

        // returns all drives, even the internal HDDs - you might need this if you want to copy a file onto an external drive
        public IReadOnlyList<IDrive> Drives {
            get { lock(this) return drives_; }
        }



        private void OnNewDevice(string VidPid, string UniqueId) {
            lock (this) {
                if (vidpid_to_unique_id_.ContainsKey(VidPid))
                    vidpid_to_unique_id_[VidPid] = UniqueId;
                else
                    vidpid_to_unique_id_.Add(VidPid, UniqueId);
            }
            RefreshPortableUniqueIds();
            var already_a_drive = false;
            lock (this) {
                var ad = drives_.FirstOrDefault(d => d.UniqueId == UniqueId) as PortableDrive;
                if (ad != null) {
                    ad.ConnectedViaUSB = true;
                    already_a_drive = true;
                }
            }
            if (!already_a_drive)
                WinUtil.postpone(() => MonitorForDrive(VidPid, 0), 50);            
        }

        private void OnDeletedDevice(string VidPid, string UniqueId) {            
            lock (this) {
                var ad = drives_.FirstOrDefault(d => d.UniqueId == UniqueId) as PortableDrive;
                if (ad != null)
                    ad.ConnectedViaUSB = false;
            }
            Refresh();
        }

        private void DeviceAddedConroller(Dictionary<string, string> properties) {
            if (properties.ContainsKey("Dependent")) {
                var device_id = properties["Dependent"];
                string VidPid = "", UniqueId = "";
                if (USBUtil.dependent_to_vidpid_and_unique_id(device_id, ref VidPid, ref UniqueId)) 
                    OnNewDevice(VidPid, UniqueId);
            } 
        }
        private void DeviceRemovedConroller(Dictionary<string, string> properties) {
            if (properties.ContainsKey("Dependent")) {
                var device_id = properties["Dependent"];
                string VidPid = "", UniqueId = "";
                if (USBUtil.dependent_to_vidpid_and_unique_id(device_id, ref VidPid, ref UniqueId)) 
                    OnDeletedDevice(VidPid, UniqueId);
            } 
        }

        private void DeviceAdded(Dictionary<string, string> properties) {
            if (properties.ContainsKey("PNPDeviceID")) {
                var device_id = properties["PNPDeviceID"];
                string VidPid = "", UniqueId = "";
                if (USBUtil.pnp_device_id_to_vidpid_and_unique_id(device_id, ref VidPid, ref UniqueId)) 
                    OnNewDevice(VidPid, UniqueId);
            } else {
                // added usb device with no PNPDeviceID
                Debug.Assert(false);
            }
        }

        // here, we know the drive was connected, wait a bit until it's actually visible
        private void MonitorForDrive(string vidpid, int idx) {
            const int MAX_RETRIES = 10;
            var drives_now = GetPortableDrives();
            var found = drives_now.FirstOrDefault(d => (d as PortableDrive).VidPid == vidpid);
            if (found != null) 
                Refresh();
            else if (idx < MAX_RETRIES)
                WinUtil.postpone(() => MonitorForDrive(vidpid, idx + 1), 100);
            else {
                // "can't find usb connected drive " + vidpid
                Debug.Assert(false);
            }
        }

        private void DeviceRemoved(Dictionary<string, string> properties) {
            if (properties.ContainsKey("PNPDeviceID")) {
                var device_id = properties["PNPDeviceID"];
                string VidPid = "", UniqueId = "";
                if (USBUtil.pnp_device_id_to_vidpid_and_unique_id(device_id, ref VidPid, ref UniqueId)) 
                    OnDeletedDevice(VidPid, UniqueId);                
            } else {
                // deleted usb device with no PNPDeviceID
                Debug.Assert(false);
            }
        }


        public bool AutoCloseWinDialogs {
            get { return AutoCloseWinDialogs_; }
            set {
                if (AutoCloseWinDialogs_ == value)
                    return;
                AutoCloseWinDialogs_ = value;
            }
        }

        // this includes all drives, even the internal ones
        private List<IDrive> drives_ = new List<IDrive>();

        public void Refresh() {
            List<IDrive> drives_now = new List<IDrive>();
            try {
                drives_now.AddRange(GetWinDrives());
            } catch (PDException e) {
                throw new PDException( "error getting win drives ", e);
            }
            try {
                drives_now.AddRange(GetPortableDrives());
            } catch (PDException e) {
                throw new PDException("error getting android drives ", e);
            }
            var external = drives_now.Where(d => d.Type != EnumDriveType.INTERNAL_HDD).ToList();
            lock (this) {
                drives_ = drives_now;
            }
            RefreshPortableUniqueIds();
        }

        private void RefreshPortableUniqueIds() {
            lock(this)
                foreach ( PortableDrive ad in drives_.OfType<PortableDrive>())
                    if ( vidpid_to_unique_id_.ContainsKey(ad.VidPid))
                        ad.UniqueId = vidpid_to_unique_id_[ad.VidPid];
        }

        // As drive name, use any of: 
        // "{<UniqueId>}:", "<drive-name>:", "[a<android-drive-index>]:", "[i<ios-index>]:", "[p<portable-index>]:", "[d<drive-index>]:"
        public IDrive TryGetDrive(string drive_prefix) {
            drive_prefix = drive_prefix.Replace("/", "\\");
            // case insensitive
            foreach ( var d in drives_)
                if (string.Compare(d.RootName, drive_prefix, StringComparison.CurrentCultureIgnoreCase) == 0 ||
                    string.Compare("{" + d.UniqueId + "}:\\", drive_prefix, StringComparison.CurrentCultureIgnoreCase) == 0)
                    return d;

            if (drive_prefix.StartsWith("[") && drive_prefix.EndsWith("]:\\")) {
                drive_prefix = drive_prefix.Substring(1, drive_prefix.Length - 4);
                if (drive_prefix.StartsWith("d", StringComparison.CurrentCultureIgnoreCase)) {
                    // d<drive-index>
                    drive_prefix = drive_prefix.Substring(1);
                    var idx = 0;
                    if (int.TryParse(drive_prefix, out idx)) {
                        var all = drives_;
                        if (all.Count > idx)
                            return all[idx];
                    }
                }
                else if (drive_prefix.StartsWith("a", StringComparison.CurrentCultureIgnoreCase)) {
                    drive_prefix = drive_prefix.Substring(1);
                    var idx = 0;
                    if (int.TryParse(drive_prefix, out idx)) {
                        var android = drives_.Where(d => d.Type.is_android()).ToList();
                        if (android.Count > idx)
                            return android[idx];
                    }                    
                }
                else if (drive_prefix.StartsWith("i", StringComparison.CurrentCultureIgnoreCase)) {
                    drive_prefix = drive_prefix.Substring(1);
                    var idx = 0;
                    if (int.TryParse(drive_prefix, out idx)) {
                        var ios = drives_.Where(d => d.Type.is_iOS()).ToList();
                        if (ios.Count > idx)
                            return ios[idx];
                    }                    
                }
                else if (drive_prefix.StartsWith("p", StringComparison.CurrentCultureIgnoreCase)) {
                    drive_prefix = drive_prefix.Substring(1);
                    var idx = 0;
                    if (int.TryParse(drive_prefix, out idx)) {
                        var portable = drives_.Where(d => d.Type.is_portable()).ToList();
                        if (portable.Count > idx)
                            return portable[idx];
                    }                    
                }
            }

            return null;
        }
        // throws if drive not found
        public IDrive get_drive(string drive_prefix) {
            // case insensitive
            var d = TryGetDrive(drive_prefix);
            if ( d == null)
                throw new PDException("invalid drive " + drive_prefix);
            return d;
        }

        private void split_into_drive_and_folder_path(string path, out string drive, out string folder_or_file) {
            path = path.Replace("/", "\\");
            var end_of_drive = path.IndexOf(":\\");
            if (end_of_drive >= 0) {
                drive = path.Substring(0, end_of_drive + 2);
                folder_or_file = path.Substring(end_of_drive + 2);
            } else
                drive = folder_or_file = null;
        }

        // returns null on failure
        public IFile TryParseFile(string path) {
            // split into drive + path
            string drive_str, folder_or_file;
            split_into_drive_and_folder_path(path, out drive_str, out folder_or_file);
            if (drive_str == null)
                return null;
            var drive = get_drive(drive_str);
            return drive.TryParseFile(folder_or_file);            
        }

        // returns null on failure
        public IFolder TryParseFolder(string path) {
            string drive_str, folder_or_file;
            split_into_drive_and_folder_path(path, out drive_str, out folder_or_file);
            if ( drive_str == null)
                return null;
            var drive = TryGetDrive(drive_str);
            if (drive == null)
                return null;
            return drive.TryParseFolder(folder_or_file);            
        }

        // throws if anything goes wrong
        public IFile ParseFile(string path) {
            // split into drive + path
            string drive_str, folder_or_file;
            split_into_drive_and_folder_path(path, out drive_str, out folder_or_file);
            if ( drive_str == null)
                throw new PDException("invalid path " + path);
            var drive = TryGetDrive(drive_str);
            if (drive == null)
                return null;
            return drive.ParseFile(folder_or_file);
        }

        // throws if anything goes wrong
        public IFolder ParseFolder(string path) {
            string drive_str, folder_or_file;
            split_into_drive_and_folder_path(path, out drive_str, out folder_or_file);
            if ( drive_str == null)
                throw new PDException("invalid path " + path);
            var drive = get_drive(drive_str);
            return drive.ParseFolder(folder_or_file);
        }

        // creates all folders up to the given path
        public IFolder NewFolder(string path) {
            string drive_str, folder_or_file;
            split_into_drive_and_folder_path(path, out drive_str, out folder_or_file);
            if ( drive_str == null)
                throw new PDException("invalid path " + path);
            var drive = get_drive(drive_str);
            return drive.CreateFolder(folder_or_file);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Portable


        private List<IDrive> GetPortableDrives() {
            var new_drives = PortableUtil. get_portable_connected_device_drives().Select(d => new PortableDrive(d) as IDrive).ToList();
            List<IDrive> old_drives = null;
            lock (this)
                old_drives = drives_.Where(d => d is PortableDrive).ToList();

            // if we already have this drive, reuse that
            List<IDrive> result = new List<IDrive>();
            foreach (var new_ in new_drives) {
                var old = old_drives.FirstOrDefault(od => od.RootName == new_.RootName);
                result.Add(old ?? new_);
            }
            return result;
        }

        // END OF Portable
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Windows

        // for now, I return all drives - don't care about which is External, Removable, whatever

        private List<IDrive> GetWinDrives() {
            return DriveInfo.GetDrives().Select(d => new WinDrive(d) as IDrive).ToList();
        }
        // END OF Windows
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

    }
}
