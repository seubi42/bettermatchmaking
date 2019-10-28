// Better Splits Project - https://board.ipitting.com/bettersplits
// Written by Sebastien Mallet (seubiracing@gmail.com - iRacer #281664)
// --------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterMatchMaking.Library.Calc
{
    /// <summary>
    /// This classe generates Truth table of classes with any combinations,
    /// (the most populate class is always True)
    /// 
    /// Exemple of data generated:
    /// 
    ///  C7 | GTE | GTE
    ///  X     X     X 
    ///  X           X
    ///        X     X
    ///              X
    ///              
    /// A second small class bellow convert booleans to
    /// classes ids avaiable to simplify data querying
    /// 
    /// Same exemple gives :
    /// 77, 473, 100 (=C7, GT3, GTE)
    /// 77, 100 (=C7, GTE)
    /// 473, 100 (=GT3, GTE)
    /// GTE)
    /// 
    /// </summary>
    public class EveryCombinations
    {

        public List<Combination> Combinations { get; private set; }

        public EveryCombinations(List<int> classIds)
        {

            // Using lina que generate truth tables :
            if (classIds.Count == 5)
            {
                Combinations = (from b1 in new[] { false, true }
                                from b2 in new[] { false, true }
                                from b3 in new[] { false, true }
                                from b4 in new[] { false, true }
                                from b5 in new[] { true }
                                select new Combination(classIds.ToArray(), b1, b2, b3, b4, b5)).ToList();
            }
            if (classIds.Count == 4)
            {
                Combinations = (from b1 in new[] { false, true }
                                from b2 in new[] { false, true }
                                from b3 in new[] { false, true }
                                from b4 in new[] { true }
                                select new Combination(classIds.ToArray(), b1, b2, b3, b4)).ToList();
            }
            else if (classIds.Count == 3)
            {
                Combinations = (from b1 in new[] { false, true }
                                from b2 in new[] { false, true }
                                from b3 in new[] { true }
                                select new Combination(classIds.ToArray(), b1, b2, b3)).ToList();
            }
            else if (classIds.Count == 2)
            {
                Combinations = (from b1 in new[] { false, true }
                                from b2 in new[] { true }
                                select new Combination(classIds.ToArray(), b1, b2)).ToList();
            }
            else if (classIds.Count == 1)
            {
                Combinations = (from b1 in new[] { true }
                                select new Combination(classIds.ToArray(), b1)).ToList();
            }

            
            // Sort the combinations
            Combinations = (from r in Combinations where r.NumberOfTrue > 0 orderby r.NumberOfTrue descending  select r).ToList();


            

        }
        
    }


    /// <summary>
    /// This Combinaition class transform a truth table line booleans
    /// to classes ids.
    /// 
    /// Exemple:
    /// Input : 
    /// C7 | GTE | GTE
    ///  X           X
    /// Output :
    /// 77, 100 (=C7, GTE)
    /// </summary>
    public class Combination
    {

        public Combination()
        {

        }

        public Combination(int[] classIds, params bool[] bools)
        {
            ClassesId = classIds;
            Enabled = bools;
        }

        /// <summary>
        /// Array of Classes Ids (Exemple : 77, 473, 100)
        /// </summary>
        public int[] ClassesId { get; internal set; }

        /// <summary>
        /// Truth table line, array indexes matches the 'ClassesId' array.
        /// (Exemple : True, False, True) means : 77 and 473 are availbe
        /// </summary>
        public bool[] Enabled { get; internal set; }


        /// <summary>
        /// Number of True in 'Enabled' array
        /// </summary>
        public int NumberOfTrue
        {
            get
            {
                return (from r in Enabled where r select r).Count();
            }
        }


        /// <summary>
        /// Get the Classes Ids which are true only
        /// (Exemple : 77, 100)
        /// </summary>
        public List<int> EnabledClassesId
        {
            get
            {
                List<int> ret = new List<int>();
                for (int i = 0; i < Enabled.Length; i++)
                {
                    if (Enabled[i]) ret.Add(ClassesId[i]);
                }
                return ret;
            }
        }


        /// <summary>
        /// Just to help debugging
        /// Format the data with a concatenation of enabled ClassesIds.
        /// Disabled ClassesIds are replaces with a dot '.'.
        /// Exemple : " 77 |  .  | 100"
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string ret = "";
            for (int i = 0; i < ClassesId.Length; i++)
            {
                if (Enabled[i]) ret += Data.Tools.CenterString(ClassesId[i].ToString(), 3);
                else ret += Data.Tools.CenterString(".", 3);

                if (i < ClassesId.Length - 1) ret += "|";
            }
            return ret;
        }
    }
}
