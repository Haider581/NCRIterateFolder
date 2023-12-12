using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheckFilesService
{
    public class Destination
    {
        public string Address { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool IsNetworkBasedLocation { get; set; }
        public bool IsAuthenticationRequired { get; set; }
    }
}
