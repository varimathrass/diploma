using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HardWarePOST
{
    class ComputerInfo
    {
        public string hostName { get; set; }
        public string macAddress { get; set; }
        public string motherboard { get; set; }
        public string cpuName { get; set; }
        public int cpuCores { get; set; }
        public string gpuName { get; set; }
        public float ram { get; set; }
    }
}
