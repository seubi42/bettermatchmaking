using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ConsoleTables;

namespace BetterMatchMaking.Library.Calc
{
    partial class PredictionsEvaluator
    {
        public int ParameterDebugFileValue { get; private set; }
        public static void CleanOldDebugFiles()
        {
            DirectoryInfo di = new DirectoryInfo("predictlogs");
            if (di.Exists)
            {

                foreach (var existingDebugFile in di.GetFiles("*.txt"))
                {
                    try
                    {
                        existingDebugFile.Delete();
                    }
                    catch { }
                }
            }
        }

        private void WriteDebugsFiles()
        {
          

            string splitNumber = _predictions.First().CurrentSplit.Number.ToString();
            while (splitNumber.Length < 2) splitNumber = "0" + splitNumber;

            DirectoryInfo di = new DirectoryInfo("predictlogs");
            if (!di.Exists) di.Create();

            StringBuilder sb = new StringBuilder();

            int countPrediction = 0;
            foreach (var item in _predictions)
            {
                countPrediction++;

                sb.AppendLine(" > PREDICTION " + countPrediction);


                /*
                 * OUTPUT SPLITS TABLE
                 * colums are classes
                 * rows : 
                 *  - car count for current split
                 *  - sofs for current split
                 *  - car count for next split
                 *  - sofs count for next split
                 */
                List<string> columns = new List<string>();
                columns.Add("SPLIT");
                foreach (var c in _classesQueues)
                {
                    columns.Add(c.CarClassId.ToString());
                }


                var opt = new ConsoleTableOptions
                {
                    Columns = columns.ToArray(),
                    EnableCount = false,
                    NumberAlignment = Alignment.Right
                };
                var table = new ConsoleTable(opt);
                columns = new List<string>();
                columns.Add(item.CurrentSplit.Number.ToString());
                foreach (var carClass in _classesQueues)
                {
                    string cell = "x";
                    int classIndex = item.CurrentSplit.GetClassIndexOfId(carClass.CarClassId);
                    if (classIndex > -1)
                    {
                        string cell2 = item.CurrentSplit.CountClassCars(classIndex).ToString();
                        if (cell2 != "0") cell = cell2;                        
                    }
                    columns.Add(cell);
                }
                table.AddRow2(columns.ToArray());

                columns = new List<string>();
                columns.Add("");
                foreach (var carClass in _classesQueues)
                {
                    string cell = "x";
                    int classIndex = item.CurrentSplit.GetClassIndexOfId(carClass.CarClassId);
                    if (classIndex > -1)
                    {
                        string cell2 = item.CurrentSplit.GetClassSof(classIndex).ToString();
                        if (cell2 != "0") cell = cell2;
                    }
                    columns.Add(cell);
                }
                table.AddRow2(columns.ToArray());


                columns = new List<string>();
                columns.Add(item.NextSplit.Number.ToString());
                foreach (var carClass in _classesQueues)
                {
                    string cell = "x";
                    int classIndex = item.NextSplit.GetClassIndexOfId(carClass.CarClassId);
                    if (classIndex > -1)
                    {
                        string cell2 = item.NextSplit.CountClassCars(classIndex).ToString();
                        if (cell2 != "0") cell = cell2;
                    }
                    columns.Add(cell);
                }
                table.AddRow2(columns.ToArray());

                columns = new List<string>();
                columns.Add("");
                foreach (var carClass in _classesQueues)
                {
                    string cell = "x";
                    int classIndex = item.NextSplit.GetClassIndexOfId(carClass.CarClassId);
                    if (classIndex > -1)
                    {
                        string cell2 = item.NextSplit.GetClassSof(classIndex).ToString();
                        if (cell2 != "0") cell = cell2;
                    }
                    columns.Add(cell);
                }
                table.AddRow2(columns.ToArray());

                sb.AppendLine(table.ToString());


                /*
                 * OUTPUT STATISTICS ABOUT THAT PREDICTION
                 * 2 columns (name, value)
                 * rows are any available metrics
                 */
                columns = new List<string>();
                columns.Add("Statistics");
                columns.Add("Value");
                opt = new ConsoleTableOptions
                {
                    Columns = columns.ToArray(),
                    EnableCount = false,
                    NumberAlignment = Alignment.Right
                };
                table = new ConsoleTable(opt);

                table.AddRow("Number of Classes", item.NumberOfClasses);
                table.AddRow("No Middle Classes Missing", item.NoMiddleClassesMissing);
                table.AddRow("Diff Between Classes (%)", item.DiffBetweenClassesPercent);
                table.AddRow("Diff Between Classes (pts)", item.DiffBetweenClassesPoints);
                table.AddRow("All SoFs are Lower than Previous split Max", item.AllSofsLowerThanPrevSplitMax);
                table.AddRow("All SoFs are Higher than Next split Min", item.AllSofsHigherThanNextSplitMax);
                table.AddRow("Most Populated Class has the Max Split SoF", item.MostPopulatedClassIsTheMaxSox);
                table.AddRow("Diff between Current split Min Sof and Next split Max Sof (%)", item.DiffBetweenMinCurrentSplitSofAndMaxNextSplitSof);
                
                foreach (var rd in item.RatingDiffPerClassPercent)
                {
                    table.AddRow("iRating diff in class Percent (class " + rd.Key +")", rd.Value);
                    table.AddRow("iRating diff in class Points (class " + rd.Key + ")", item.RatingDiffPerClassPoints[rd.Key]);
                }
                foreach (var cut in item.ClassesCuttedAroundRatingThreshold)
                {
                    table.AddRow("Variation with class cutted around rating threshold", "(class " + cut + ")");
                }

                sb.AppendLine(table.ToString());

                sb.AppendLine("");
                sb.AppendLine("");

            }


            string file = Path.Combine(di.FullName, splitNumber + ".txt");
            System.IO.File.WriteAllText(file, sb.ToString());

        }
    }
}
