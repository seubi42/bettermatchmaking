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


        static string[] debugCsvCarClassesName;
        static Dictionary<int, string> cacheClassNames = new Dictionary<int, string>();

        private string ReadClassName(int id)
        {
            if (cacheClassNames.ContainsKey(id)) return cacheClassNames[id];

            if (debugCsvCarClassesName == null)
            {
                string path = "carclasses.csv";
                try
                {
                    if (System.IO.File.Exists(path))
                    {
                        debugCsvCarClassesName = System.IO.File.ReadAllLines(path);
                        
                    }
                }
                catch { }
            }

            try
            {
                if (debugCsvCarClassesName != null)
                {
                    var line = (from r in debugCsvCarClassesName where r.StartsWith(id + ";") select r).FirstOrDefault();
                    if (!String.IsNullOrWhiteSpace(line))
                    {
                        string ret = line.Split(';').LastOrDefault().Trim();
                        cacheClassNames.Add(id, ret);
                        return ret;
                    }
                }
            }
            catch { }

            return id.ToString();

        }

        private void WriteDebugsFiles()
        {
          

            string splitNumber = _predictions.First().CurrentSplit.Number.ToString();
            while (splitNumber.Length < 2) splitNumber = "0" + splitNumber;

            DirectoryInfo di = new DirectoryInfo("predictlogs");
            if (!di.Exists) di.Create();

            StringBuilder sb = new StringBuilder();

            foreach (var item in _predictions)
            {
                

                sb.AppendLine(" > PREDICTION " + item.Id);


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
                    columns.Add(ReadClassName(c.CarClassId));
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
                table.AddRow("Diff Between Classes (%)", Math.Round(item.DiffBetweenClassesPercent,2));
                table.AddRow("Diff Between Classes (pts)", item.DiffBetweenClassesPoints);
                table.AddRow("All SoFs are Lower than Previous split Max", item.AllSofsLowerThanPrevSplitMax);
                table.AddRow("All SoFs are Higher than Next split Min", item.AllSofsHigherThanNextSplitMax);
                table.AddRow("Most Populated Class has the Max Split SoF", item.MostPopulatedClassIsTheMaxSox);
                table.AddRow("Diff between Current split Min Sof and Next split Max Sof (%)", Math.Round(item.DiffBetweenMinCurrentSplitSofAndMaxNextSplitSof,2));
                
                foreach (var rd in item.RatingDiffPerClassPercent)
                {
                    table.AddRow("iRating diff in class Percent (" + ReadClassName(rd.Key) +")", Math.Round(rd.Value,2));
                    table.AddRow("iRating diff in class Points (" + ReadClassName(rd.Key) + ")", item.RatingDiffPerClassPoints[rd.Key]);
                }
                foreach (var cut in item.ClassesCuttedAroundRatingThreshold)
                {
                    table.AddRow("Variation with class cutted around rating threshold", "(" + ReadClassName(cut) + ")");
                }

                sb.AppendLine(table.ToString());

                sb.AppendLine("");
                sb.AppendLine("");

            }


            string file = Path.Combine(di.FullName, splitNumber + ".txt");
            System.IO.File.WriteAllText(file, sb.ToString());

        }


        StringBuilder sbDecisions = new StringBuilder();

        private void OutputDebugDecisionMessage(string testdescription)
        {
            if (ParameterDebugFileValue == 1)
            {
                sbDecisions.AppendLine(testdescription);
            }
        }

        private void CommitDebugDecisions(int split)
        {
            if (ParameterDebugFileValue == 1)
            {
                string splitNumber = _predictions.First().CurrentSplit.Number.ToString();
                while (splitNumber.Length < 2) splitNumber = "0" + splitNumber;

                DirectoryInfo di = new DirectoryInfo("predictlogs");
                if (!di.Exists) di.Create();


                string file = Path.Combine(di.FullName, splitNumber + ".txt");
                System.IO.File.AppendAllText(file, sbDecisions.ToString());


                sbDecisions.Clear();
            }
        }

        private void OutputDebugDecisionResults(List<Data.PredictionOfSplits> predictions, int? take)
        {
            List<Data.PredictionOfSplits> choices = predictions;
            if (take != null) choices = choices.Take(take.Value).ToList();

            List<string> columns = new List<string>();
            columns.Add("Prediction");
            foreach (var carClass in _classesQueues)
            {
                columns.Add(ReadClassName(carClass.CarClassId) + " cars");
                columns.Add(ReadClassName(carClass.CarClassId) + " SoF");
            }
            columns.Add("Diff.");


            var opt = new ConsoleTableOptions
            {
                Columns = columns.ToArray(),
                EnableCount = false,
                NumberAlignment = Alignment.Right
            };
            var table = new ConsoleTable(opt);
            foreach (var c in choices)
            {
                columns.Clear();
                columns.Add(c.Id);
                foreach (var carClass in _classesQueues)
                {
                    int classIndex = c.CurrentSplit.GetClassIndexOfId(carClass.CarClassId);
                    if (classIndex > -1)
                    {

                        columns.Add(c.CurrentSplit.CountClassCars(classIndex).ToString());
                        columns.Add(c.CurrentSplit.GetClassSof(classIndex).ToString());
                        
                    }
                    else
                    {
                        columns.Add("x");
                        columns.Add("");
                    }

                }
                columns.Add(c.CurrentSplit.ClassesSofDifference);
                table.AddRow2(columns.ToArray());
            }
            
            
            sbDecisions.AppendLine(table.ToString());
        }


    }
}
