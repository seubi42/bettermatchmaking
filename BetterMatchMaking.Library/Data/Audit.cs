using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterMatchMaking.Library.Data
{
    public class Audit
    {
        public bool Success { get; set; }
        public List<int> SplitsExceedsFieldSize { get; set; }
        public List<int> CarsMissingInAnySplit { get; set; }
        public List<int> NotExpectedCarsRegistred { get; set; }

        public int ComputingTimeInMs { get; set; }

        public int Cars { get; set; }
        public int Splits { get; set; }

        public int AverageSplitClassesSofDifference { get; set; }

        public double MinSplitSizePercent { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (Success) sb.AppendLine("SUCCESS");
            else sb.AppendLine("ERROR");

            if(SplitsExceedsFieldSize.Count > 0)
            {
                sb.AppendLine("SplitsExceedsFieldSize : " + Contact(SplitsExceedsFieldSize));
            }
            if (CarsMissingInAnySplit.Count > 0)
            {
                sb.AppendLine("CarsMissingInAnySplit : " + Contact(CarsMissingInAnySplit));
            }
            if (NotExpectedCarsRegistred.Count > 0)
            {
                sb.AppendLine("NotExpectedCarsRegistred : " + Contact(NotExpectedCarsRegistred));
            }

            string ret = sb.ToString();
            ret = ret.Trim();
            return ret;
        }

        private static string Contact(List<int> li)
        {
            string ret = "";
            for (int i = 0; i < li.Count; i++)
            {
                ret += li[i];
                if (i < li.Count - 1) ret += ", ";
            }
            return ret;
        }
    }
}
