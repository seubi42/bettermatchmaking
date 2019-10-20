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

        public string ClassesKey
        {
            get
            {
                string ret = "";
                List<int> ids = (from r in ClassCarsTarget orderby r.Key ascending select r.Key).ToList();
                foreach (var id in ids)
                {
                    ret += id + ";";
                }
                return ret;
            }
        }

        public int CountTotalTargets()
        {
            return (from r in ClassCarsTarget select r.Value).Sum();
        }
    }
}
