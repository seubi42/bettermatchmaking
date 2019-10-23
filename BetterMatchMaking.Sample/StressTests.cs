﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace BetterMatchMaking.Sample
{
    public class StressTests
    {
        int tests = 0;
        int testsFailed = 0;
        int testsSplitsDiff = 0;

        public void Tests()
        {
            DateTime dtStart = DateTime.Now;

            var di = new DirectoryInfo("..\\..\\..\\BetterMatchMaking.UI\\Bin\\Debug\\");
            var files = di.GetFiles("*-fieldsize*.csv");
            foreach (var file in files)
            {
                Test(file.FullName);
            }

            Console.WriteLine("Test done: " + tests);
            Console.WriteLine("- Failed: " + testsFailed);
            Console.WriteLine("- Warning Splits Diff: " + testsSplitsDiff);

            var bench = DateTime.Now.Subtract(dtStart);
            Console.WriteLine("in " + bench.ToString() + " ("+bench.TotalSeconds+" s)");

            Console.ReadLine();
        }

        public void Test(string csv)
        {
            

            // get fieldsize from file name
            int fieldSize = 0;
            string cst_fieldsize = "-fieldsize";
            if (csv.Contains(cst_fieldsize))
            {
                string strfieldsize = csv.Substring(
                    csv.IndexOf(cst_fieldsize) + cst_fieldsize.Length,
                    2
                    );
                Int32.TryParse(strfieldsize, out fieldSize);
            }
            // -->


            // read csv
            var parser = new BetterMatchMaking.Library.Data.CsvParser();
            parser.Read(csv);
            var entrylist = parser.DistinctCars;
            // -->


            // run algorithm
            BetterMatchMaking.Library.BetterMatchMakingCalculator calculator = new Library.BetterMatchMakingCalculator("SmartMoveDownAffineDistribution");
            for (int maxsofdiff = 5; maxsofdiff < 40; maxsofdiff++)
            {
                for (int mostpop = 0; mostpop < 1; mostpop++)
                {
                    Console.WriteLine("");
                    Console.WriteLine("------------------------------------");

                    Console.WriteLine(new FileInfo(csv).Name);
                    Console.WriteLine("Max Soff Diff = " + maxsofdiff);
                    calculator.ParameterMaxSofDiffValue = maxsofdiff;
                    calculator.ParameterMaxSofFunctAValue = 12;
                    calculator.ParameterMaxSofFunctXValue = 1000;
                    calculator.ParameterMaxSofFunctBValue = -20;
                    calculator.ParameterTopSplitExceptionValue = 0;
                    calculator.ParameterMostPopulatedClassInEverySplitsValue = mostpop;
                    calculator.Compute(entrylist, fieldSize);
                    var audit = calculator.GetAudit();
                    Console.WriteLine(audit.ToString());
                    Console.WriteLine("AverageSplitClassesSofDifference = " + audit.AverageSplitClassesSofDifference);
                    Console.WriteLine("MinSplitSizePercent = " + audit.MinSplitSizePercent);
                    if (!audit.Success)
                    {
                        testsFailed++;
                    }
                    if (audit.MinSplitSizePercent < 0.5)
                    {
                        testsSplitsDiff++;
                    }
                    if (audit.IROrderInconsistencySplits.Count > 0)
                    {

                    }
                    tests++;
                }
            }

            // -->




        }
    }
}
