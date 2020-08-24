using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using PortableDeviceManager.Exceptions;

namespace PortableDeviceManager.Monitor
{
    public class DeviceMonitor
    {
        private void DeviceInsertedEvent(object sender, EventArrivedEventArgs e)
        {
            try {
                Dictionary<string,string> properties = new Dictionary<string, string>();
                ManagementBaseObject instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
                foreach (var p in instance.Properties)
                    if ( p.Value != null)
                        properties.Add(p.Name, p.Value.ToString());

                DeviceAddedEvent?.Invoke(properties);
            } catch (PDException ex) {
                throw new PDException( "invalid device inserted", ex);
            }
        }

        private void DeviceRemovedEvent(object sender, EventArrivedEventArgs e)
        {
            try {
                Dictionary<string,string> properties = new Dictionary<string, string>();
                ManagementBaseObject instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
                foreach (var p in instance.Properties)
                    if ( p.Value != null)
                        properties.Add(p.Name, p.Value.ToString());

                DeviceDeletedEvent?.Invoke(properties);
            } catch (PDException ex) {
                throw new PDException("invalid device removed", ex);
            }
        }

        public Action<Dictionary<string, string>> DeviceAddedEvent;
        public Action<Dictionary<string, string>> DeviceDeletedEvent;

        // not used yet, however, tested and works
        // examples: generic_monitor("Win32_USBHub"); generic_monitor("Win32_DiskDrive");
        public void Monitor(string class_name)
        {
            WqlEventQuery insertQuery = new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA '" + class_name + "'");

            ManagementEventWatcher insertWatcher = new ManagementEventWatcher(insertQuery);
            insertWatcher.EventArrived += new EventArrivedEventHandler(DeviceInsertedEvent);
            insertWatcher.Start();

            WqlEventQuery removeQuery = new WqlEventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 2 WHERE TargetInstance ISA '" + class_name + "'");
            ManagementEventWatcher removeWatcher = new ManagementEventWatcher(removeQuery);
            removeWatcher.EventArrived += new EventArrivedEventHandler(DeviceRemovedEvent);
            removeWatcher.Start();
        }

    }
}
