using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterMatchMaking.Library
{
    public class BetterMatchMakingCalculator
    {
        public Calc.IMatchMaking Calculator { get; private set; }

        public BetterMatchMakingCalculator(string algorithm)
        {
            var type = this.GetType().Assembly.GetType("BetterMatchMaking.Library.Calc." + algorithm);
            Calculator = Activator.CreateInstance(type) as Calc.IMatchMaking;
        }

        public static void CopyParameters(Calc.IMatchMaking source, Calc.IMatchMaking target)
        {
            // copy all .ParameterXXX properties from source to target instance
            var type = typeof(Calc.IMatchMaking);
            var parameters = (from r in type.GetProperties() where r.Name.StartsWith("Parameter") select r).ToList();
            foreach (var parameter in parameters)
            {
                var o = parameter.GetValue(source);
                parameter.SetValue(target, o);
            }
        }
    }
}

