using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterMatchMaking.Sample
{
    class Program
    {
        static void Main(string[] args)
        {

            //new Sandbox().Run();
            //return;

            if (args.Length == 1 && args[0] == "stresstest")
            {
                new StressTests().Tests();
            }
            else
            {
                new HowToCodeId().Demo();
            }
            

        }

        

        

    }
}
