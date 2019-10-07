using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterMatchMaking.Data
{
    public class CarsPerClass
    {
        public int CarClassId { get; set; }
        public List<Data.Line> Cars { get; set; }

        public List<Data.Line> GetCars(int i)
        {
            var selection = Cars.Take(i).ToList();
            foreach (var c in selection) Cars.Remove(c);
            return selection;
        }



    }
}
