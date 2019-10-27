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
    public class EveryCombinations
    {

        public List<Combination> Combinations { get; private set; }

        public EveryCombinations(List<int> classIds)
        {

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

            
            Combinations = (from r in Combinations where r.NumberOfTrue > 0 orderby r.NumberOfTrue descending  select r).ToList();


            

        }
        
    }
    
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
        public int[] ClassesId { get; internal set; }
        public bool[] Enabled { get; internal set; }

        public int NumberOfTrue
        {
            get
            {
                return (from r in Enabled where r select r).Count();
            }
        }

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
