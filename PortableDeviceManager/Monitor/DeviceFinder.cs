using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace PortableDeviceManager.Monitor
{
    // this is code tested in another project - it's very likely we'll need it at some point
    class DeviceFinder
    {
        public static void ShowLogicalPartitions() {
            var search = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_DiskDrive");
            foreach (ManagementObject drive in search.Get())
            {

                string antecedent = drive["DeviceID"].ToString(); // the disk we're trying to find out about
                antecedent = antecedent.Replace(@"\", "\\"); // this is just to escape the slashes
                string query = "ASSOCIATORS OF {Win32_DiskDrive.DeviceID='" + antecedent + "'} WHERE AssocClass = Win32_DiskDriveToDiskPartition";
                using (ManagementObjectSearcher partitionSearch = new ManagementObjectSearcher(query))
                {
                    foreach (ManagementObject part in partitionSearch.Get())
                    {
                        //foreach (var property in part.Properties)
                        //  Console.WriteLine("root  " + property.Name + " = " + property.Value);

                        //...pull out the partition information
                        Console.WriteLine("root " + part["DeviceID"].ToString());
                        query = "ASSOCIATORS OF {Win32_DiskPartition.DeviceID='" + part["DeviceID"] + "'} WHERE AssocClass = Win32_LogicalDiskToPartition";
                        using (ManagementObjectSearcher logicalpartitionsearch = new ManagementObjectSearcher(query))
                            foreach (ManagementObject logicalpartition in logicalpartitionsearch.Get()) {
                                Console.WriteLine(logicalpartition["DeviceID"].ToString());
                                //foreach (var property in logicalpartition.Properties)
                                //  Console.WriteLine("        " + property.Name + " = " + property.Value);
                            }
                    }
                }
            }
        }

        // ShowObjects("Win32_LogicalDisk");
        public static void ShowObjects(string type) {
            var search = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM " + type);
            foreach (ManagementObject o in search.Get()) {
                Console.WriteLine("-------------");
                foreach (var property in o.Properties)
                    Console.WriteLine(property.Name + " = " + property.Value);                
            }            
        }

        public static List< Dictionary<string,string>> FindObjects(string type) {
            List<Dictionary<string, string>> result = new List<Dictionary<string, string>>();
            var search = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM " + type);
            foreach (ManagementObject o in search.Get()) {
                Dictionary<string,string> properties = new Dictionary<string, string>();
                foreach (var p in o.Properties)
                    if ( p.Value != null)
                        properties.Add(p.Name, p.Value.ToString());                
                result.Add(properties);
            }

            return result;
        }

    }
}
