// Better Splits Project - https://board.ipitting.com/bettersplits
// Written by Sebastien Mallet (seubiracing@gmail.com - iRacer #281664)
// --------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ConsoleTables;

namespace BetterMatchMaking.Library.Calc
{

    /// <summary>
    /// All methods here are the debug method of the 
    /// PredictionsEvaluator class
    /// 
    /// If ParameterDebugFileValue is set to 0,
    /// all these methods are disabled.
    /// 
    /// It ParameterDebugFileValue is set to 1,
    /// these methods will build the 'predictlogs'.
    /// A text file will be generated for each split
    /// to evaluate. It will contains all the possible
    /// predictions + all filtering operations to try
    /// filtering the best option.
    /// </summary>
    partial class PredictionsEvaluator
    {

        /// <summary>
        /// Main switch for all the methos bellow
        /// </summary>
        public int ParameterDebugFileValue { get; private set; }


        /// <summary>
        /// Emtpy the 'predictlogs' folder
        /// </summary>
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


        #region string ReadClassName(int classId)
        /// <summary>
        /// If a 'carclasses.csv' file is available in the root directory,
        /// than it try to find the name corresponding to the classId.
        /// It will give a more intelligible debug/log file.
        /// 
        /// CSV file have to contains at leas two columns, following
        /// this format :
        /// 
        /// id;shortname
        /// 77;C7
        /// 473;GT3
        /// 100;GTE
        /// 
        /// </summary>
        /// <param name="classId"></param>
        /// <returns></returns>
        private string ReadClassName(int classId)
        {
            if (cacheClassNames.ContainsKey(classId)) return cacheClassNames[classId];

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
                    var line = (from r in debugCsvCarClassesName where r.StartsWith(classId + ";") select r).FirstOrDefault();
                    if (!String.IsNullOrWhiteSpace(line))
                    {
                        string ret = line.Split(';').LastOrDefault().Trim();
                        cacheClassNames.Add(classId, ret);
                        return ret;
                    }
                }
            }
            catch { }

            return classId.ToString();

        }
        // static variables to optimize / cache the ReadClassName method
        static string[] debugCsvCarClassesName;
        static Dictionary<int, string> cacheClassNames = new Dictionary<int, string>();
        #endregion


        #region WriteSplitPredictionsFile
        /// <summary>
        /// This method will allow all predictions possible
        /// for a split in 'predictlogs\XX.txt'
        /// XX is the split number, starting from 01.
        /// </summary>
        private void WriteSplitPredictionsFile()
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

        #endregion



        #region xxx


        // temporary stores texts generated these both methods :
        /// AppendDebugDecisionMessage
        /// AppendDebugDecisionResults
        StringBuilder sbDecisionsAppends = new StringBuilder();


        /// <summary>
        /// Commit all logs to append to the debugging file from
        /// including all or call to these both methods :
        /// AppendDebugDecisionMessage
        /// AppendDebugDecisionResults
        /// </summary>
        /// <param name="split"></param>
        private void CommitDecisionsAppends(int split)
        {
            if (ParameterDebugFileValue == 1)
            {
                string splitNumber = _predictions.First().CurrentSplit.Number.ToString();
                while (splitNumber.Length < 2) splitNumber = "0" + splitNumber;

                DirectoryInfo di = new DirectoryInfo("predictlogs");
                if (!di.Exists) di.Create();


                string file = Path.Combine(di.FullName, splitNumber + ".txt");
                System.IO.File.AppendAllText(file, sbDecisionsAppends.ToString());


                sbDecisionsAppends.Clear();
            }
        }


        /// <summary>
        /// This method appends a simple text line to the debug file
        /// </summary>
        /// <param name="testdescription"></param>
        private void AppendDebugDecisionMessage(string testdescription)
        {
            if (ParameterDebugFileValue == 1)
            {
                sbDecisionsAppends.AppendLine(testdescription);
            }
        }

        
        /// <summary>
        /// This method appends to the debug file a list of predictions results
        /// in a compact like table style.
        /// </summary>
        /// <param name="predictions">the predictions</param>
        /// <param name="take">number of predictions to output (null = all)</param>
        private void AppendDebugDecisionResults(List<Data.PredictionOfSplits> predictions, int? take)
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
            
            
            sbDecisionsAppends.AppendLine(table.ToString());
        }
        #endregion

    }
}
