using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterMatchMaking.Library.Data
{
    public class MultiClassChanges
    {
        public int FromSplit { get; set; }
        public int ToSplit { get; set; }
        public int ClassesCount { get; set; }

        public Dictionary<int, int> ClassCarsTarget { get; set; }
    }
}
