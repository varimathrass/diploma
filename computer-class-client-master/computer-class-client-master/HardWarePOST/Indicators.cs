using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HardWarePOST
{
    class Indicators
    {
        public CPU CPU { get; set; }
        public GPU GPU { get; set; }
        public RAM RAM { get; set; }
        public HDD HDD { get; set; }

        public Indicators()
        {
            CPU = new CPU();
            GPU = new GPU();
            RAM = new RAM();
            HDD = new HDD();
        }
    }

    class CPU
    {
        public float LoadTotal { get; set; }
        public float TempetureTotal { get; set; }
        public float[] Load { get; set; }
        public float[] Tempeture { get; set; }
        public int Cores { get; set; }
    }

    class RAM
    {
        public float UsedMemory { get; set; }
        public float AvaliableMemory { get; set; }
    }

    class GPU
    {
        public float Tempeture { get; set; }
        public float FreeMemory { get; set; }
        public float UsedMemory { get; set; }
    }

    class HDD
    {
        public float Tempeture { get; set; }
        public float UsedSpace { get; set; }
    }
}
