using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterMatchMaking.Library.Calc
{
    public class Tools
    {
        public static int CountClasses(List<Data.Line> data)
        {
            return (from r in data select r.car_class_id).Distinct().Count();
        }




        internal static List<Data.ClassCarsQueue> SplitCarsPerClass(List<Data.Line> data)
        {
            List<Data.ClassCarsQueue> ret = new List<Data.ClassCarsQueue>();
            List<int> classes = (from r in data select r.car_class_id).Distinct().ToList();
            foreach (var c in classes)
            {
                ret.Add(new Data.ClassCarsQueue
                {
                    CarClassId = c,
                    Cars = (from r in data where r.car_class_id == c orderby r.rating descending select r).ToList()
                });
            }
            ret = (from r in ret orderby r.Cars.Count ascending select r).ToList();
            return ret;
        }

        internal static int CountRemainingCars(List<Data.ClassCarsQueue> data)
        {
            return (from r in data select r.Cars.Count).Sum();
        }

        public static int Sof(List<int> ratings)
        {
            if (ratings.Count == 0) return 0;

            double log2 = Math.Log(2);
            double ln = Convert.ToDouble(1600) / log2;

            double v = 0;
            foreach (var ir in ratings)
            {
                v += Math.Exp((ir * -1) / ln);
            }
            double c = ratings.Count;

            var sof = Math.Floor(ln * Math.Log(c / v));

            return Convert.ToInt32(sof);
        }
    }
}
