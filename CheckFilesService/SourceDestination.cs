using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheckFilesService
{
    public class SourceDestination
    {
        public string SourceAddress { get; set; }
        public List<Destination> Destinations { get; set; }
    }
}
