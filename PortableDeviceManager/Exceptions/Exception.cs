using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortableDeviceManager.Exceptions
{
    class PDException : System.IO.IOException
    {
        public PDException(string s) : base(s) {
            
        }
        public PDException(string s, PDException inner) : base(s, inner) {
            
        }
    }
}
