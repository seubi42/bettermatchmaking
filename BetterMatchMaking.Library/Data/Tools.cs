// Better Splits Project - https://board.ipitting.com/bettersplits
// Written by Sebastien Mallet (seubiracing@gmail.com - iRacer #281664)
// --------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterMatchMaking.Library.Data
{
    /// <summary>
    /// This class is an internal tool to help "Split" class management
    /// </summary>
    static class Tools
    {

        /// <summary>
        /// This Methold help to set a property by it's number with a {i} variable in its name
        /// Exemple : SetProperty(mysplit, "Class{i}Name", 1, "Lapin")
        ///           is equivalent to mysplit.Class2Name = "Lapin"
        ///           (because {i} = index + 1)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">object to update</param>
        /// <param name="naming">name of property including {i} variable</param>
        /// <param name="index">index (starting from 0)</param>
        /// <param name="value">value to set</param>
        public static void SetProperty<T>(object obj, string naming, int index, T value)
        {
            string prop = naming.Replace("{i}", (index + 1).ToString());
            obj.GetType().GetProperty(prop).SetValue(obj, value);
        }

        /// <summary>
        /// This Methold help to get a property by it's number with a {i} variable in its name
        /// Exemple : var test = GetProperty(mysplit, "Class{i}Name", 1)
        ///           is equivalent to var test = mysplit.Class2Name
        ///           (because {i} = index + 1)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="naming"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static T GetProperty<T>(object obj, string naming, int index)
        {
            string prop = naming.Replace("{i}", (index + 1).ToString());
            object ret = obj.GetType().GetProperty(prop).GetValue(obj);
            return (T)ret;
        }
    }
}
