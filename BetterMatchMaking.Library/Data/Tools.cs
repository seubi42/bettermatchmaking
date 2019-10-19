using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterMatchMaking.Library.Data
{
    public class Tools
    {
        public static void SetProperty<T>(object obj, string naming, int index, T value)
        {
            string prop = naming.Replace("{i}", (index + 1).ToString());
            obj.GetType().GetProperty(prop).SetValue(obj, value);
        }

        public static T GetProperty<T>(object obj, string naming, int index)
        {
            string prop = naming.Replace("{i}", (index + 1).ToString());
            object ret = obj.GetType().GetProperty(prop).GetValue(obj);
            return (T)ret;
        }
    }
}
