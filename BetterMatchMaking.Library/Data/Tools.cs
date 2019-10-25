// Better Splits Project - https://board.ipitting.com/bettersplits
// Written by Sebastien Mallet (seubiracing@gmail.com - iRacer #281664)
// --------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
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

        /// <summary>
        /// To clone an object (without reference on the source)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static T Clone<T>(T source)
        {
            if (!typeof(T).IsSerializable)
            {
                throw new ArgumentException("The type must be serializable.", "source");
            }

            if (Object.ReferenceEquals(source, null))
            {
                return default(T);
            }

            System.Runtime.Serialization.IFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            Stream stream = new MemoryStream();
            using (stream)
            {
                formatter.Serialize(stream, source);
                stream.Seek(0, SeekOrigin.Begin);
                return (T)formatter.Deserialize(stream);
            }
        }


        /// <summary>
        /// Another way of cloning object, by copying all propertis
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">item to copy</param>
        /// <param name="target">item to write in</param>
        public static void CopyAllProperties<T>(T source, T target)
        {
            var t = source.GetType();
            foreach (var p in t.GetProperties())
            {
                var o = p.GetValue(source);
                if(p.CanWrite) p.SetValue(target, o);
            }
        }

        /// <summary>
        /// Optimized way to clone splits
        /// </summary>
        /// <param name="splits">source to clone</param>
        /// <param name="numberofsplitsneeded">number of splits you need</param>
        /// <returns></returns>
        public static List<Data.Split> SplitsCloner(List<Data.Split> splits, int numberofsplitsneeded)
        {
            

            List <Data.Split> ret = new List<Split>();
            for (int i = 0; i < Math.Min(splits.Count, numberofsplitsneeded); i++)
            {
                var source = splits[i];
                var target = new Data.Split();
                CopyAllProperties(source, target);
                // rebuild new array to not share pointer on same reference
                if (target.Class1Cars != null)
                {
                    target.Class1Cars = new List<Line>();
                    target.Class1Cars.AddRange(source.Class1Cars);
                }
                if (target.Class2Cars != null)
                {
                    target.Class2Cars = new List<Line>();
                    target.Class2Cars.AddRange(source.Class2Cars);
                }
                if (target.Class3Cars != null)
                {
                    target.Class3Cars = new List<Line>();
                    target.Class3Cars.AddRange(source.Class3Cars);
                }
                if (target.Class4Cars != null)
                {
                    target.Class4Cars = new List<Line>();
                    target.Class4Cars.AddRange(source.Class4Cars);
                }
                // -->
                ret.Add(target);
            }
            return ret;
        }
    }
}
