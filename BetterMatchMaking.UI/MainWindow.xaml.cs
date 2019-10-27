using BetterMatchMaking.Library.Data;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BetterMatchMaking.UI
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Library.Data.CsvParser parser;

        SyncSliderBox sspP;
        SyncSliderBox sspMinCars;
        SyncSliderBox sspIR;
        SyncSliderBox sspMaxSofDiff;
        SyncSliderBox sspMaxSofFunctExtraThresoldPerK;
        SyncSliderBox sspMaxSofFunctStartingIRValue;
        SyncSliderBox sspMaxSofFunctStartingThreshold;
        SyncSliderBox sspTopSplitExc;
        SyncSliderBox sspDebug;
        SyncSliderBox sspForceMidClass;

        public MainWindow()
        {
            InitializeComponent();
            parser = new Library.Data.CsvParser();

            sspP = new SyncSliderBox(lblParameterP, tbxParameterP, sldParameterP, 5, 66, 37);
            sspMinCars = new SyncSliderBox(lblParameterMinCars, tbxParameterMinCars, sldParameterMinCars, 1, 20, 10);
            sspIR = new SyncSliderBox(lblParameterIR, tbxParameterIR, sldParameterIR, 0, 3200, 1700);
            sspMaxSofDiff = new SyncSliderBox(lblParameterMaxSoffDiff, tbxParameterMaxSoffDiff, sldParameterMaxSoffDiff, 3, 100, 15);
            sspMaxSofFunctExtraThresoldPerK = new SyncSliderBox(lblParameterMaxSoffFunctExtrPctPerK, tbxParameterMaxSoffFunctExtrPctPerK, sldParameterMaxSoffFunctExtrPctPerK, 0, 50, 11);
            sspMaxSofFunctStartingIRValue = new SyncSliderBox(lblParameterMaxSoffFunctStartIR, tbxParameterMaxSoffFunctStartIR, sldParameterMaxSoffFunctStartIR, 500, 9000, 2800);
            sspMaxSofFunctStartingThreshold = new SyncSliderBox(lblParameterMaxSoffFunctStartPct, tbxParameterMaxSoffFunctStartPct, sldParameterMaxSoffFunctStartPct, 0, 50, 20);
            sspTopSplitExc = new SyncSliderBox(lblParameterTopSplitExc, tbxParameterTopSplitExc, sldParameterTopSplitExc, 0, 1, 0);
            sspDebug = new SyncSliderBox(lblParameterDebug, tbxParameterDebug, sldParameterDebug, 0, 1, 1);
            sspForceMidClass = new SyncSliderBox(lblParameterForceMidClass, tbxParameterForceMidClass, sldParameterForceMidClass, 0, 1, 0);

            sspP.Visible = false;
            sspMinCars.Visible = false;
            sspIR.Visible = false;
            sspMaxSofDiff.Visible = false;
            sspMaxSofFunctExtraThresoldPerK.Visible = false;
            sspMaxSofFunctStartingIRValue.Visible = false;
            sspMaxSofFunctStartingThreshold.Visible = false;
            sspTopSplitExc.Visible = false;
            sspDebug.Visible = false;
            sspForceMidClass.Visible = false;


            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (cboAlgorithm.SelectedIndex >= 0)
            {
                OnAlgorithmChanged();
            }
        }

        private void BtnBrowseRegistrationFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = new System.IO.FileInfo(GetType().Assembly.Location).Directory.FullName;
            openFileDialog.Multiselect = false;
            openFileDialog.Filter = "CSV file (*.csv)|*.csv|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                tbxRegistrationFile.Text = openFileDialog.FileName;
                if(tbxRegistrationFile.Text.ToLower().StartsWith(openFileDialog.InitialDirectory.ToLower() + "\\"))
                {
                    tbxRegistrationFile.Text = openFileDialog.FileName.Substring(openFileDialog.InitialDirectory.Length + 1);
                }
            }
        }

        private void BtnLoadRegistrationFile_Click(object sender, RoutedEventArgs e)
        {
            Load(true);
        }


        private void Load(bool overrideFieldSizeFromFileName)
        {
            try
            {
                // parse file
                parser.Read(tbxRegistrationFile.Text);


                // if -fieldsizeXX is in file name, get it
                if (overrideFieldSizeFromFileName)
                {
                    string cst_fieldsize = "-fieldsize";
                    if (tbxRegistrationFile.Text.Contains(cst_fieldsize))
                    {
                        string fieldsize = tbxRegistrationFile.Text.Substring(
                            tbxRegistrationFile.Text.IndexOf(cst_fieldsize) + cst_fieldsize.Length,
                            2
                            );
                        tbxFieldSize.Text = fieldsize;
                    }
                }
                //-->

                grid.ItemsSource = parser.DistinctCars;
                gridResult.ItemsSource = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        List<Library.Data.Split> result;
        int fieldsize;

        private void BtnCompute_Click(object sender, RoutedEventArgs e)
        {

            if (!String.IsNullOrWhiteSpace(tbxRegistrationFile.Text))
            {
                Load(true);
            }


            bool nodata = false;
            if (parser.Data == null) nodata = true;
            if (parser.Data.Count == 0) nodata = true;

            if (nodata)
            {
                return;
            }

            int defaultFieldSizeValue = 45;

            int fieldSize = defaultFieldSizeValue;
            int.TryParse(tbxFieldSize.Text, out fieldSize);
            this.fieldsize = fieldSize;
            if (fieldSize == 0) fieldSize = defaultFieldSizeValue;
            tbxFieldSize.Text = fieldSize.ToString();


  
            string strAlgo = (cboAlgorithm.SelectedItem as ComboBoxItem).Tag.ToString();


            // instanciate the good algorithm
            var mm = new BetterMatchMaking.Library.BetterMatchMakingCalculator(strAlgo);
            mm.ParameterClassPropMinPercentValue = sspP.Value;
            mm.ParameterMinCarsValue = sspMinCars.Value;
            mm.ParameterRatingThresholdValue = sspIR.Value;
            mm.ParameterMaxSofDiffValue = sspMaxSofDiff.Value;
            mm.ParameterMaxSofFunctStartingIRValue = sspMaxSofFunctStartingIRValue.Value;
            mm.ParameterMaxSofFunctExtraThresoldPerK = sspMaxSofFunctExtraThresoldPerK.Value;
            mm.ParameterMaxSofFunctStartingThreshold = sspMaxSofFunctStartingThreshold.Value;
            mm.ParameterTopSplitExceptionValue = sspTopSplitExc.Value;
            mm.ParameterDebugFileValue = sspDebug.Value;
            mm.ParameterNoMiddleClassesEmptyValue = sspForceMidClass.Value;


            mm.Compute(parser.DistinctCars, fieldSize);
            gridResult.ItemsSource = mm.Splits;
            result = mm.Splits;

            AddClassNamesToResults(result);
            var audit = mm.GetAudit();

            
            string morestats = mm.Splits.Count + " splits. ";
            morestats += audit.Cars + " cars. ";
            morestats += "Computed in " + audit.ComputingTimeInMs + " ms. Average split car classes difference: ";
            morestats += audit.AverageSplitClassesSofDifference + "%";
            tbxStats.Text = morestats;

            tbxStats.Background = ColorConverter.GetPercentColor(audit.AverageSplitClassesSofDifference);


            CheckAudit(audit);
        }

        private void AddClassNamesToResults(List<Split> result)
        {
            foreach (var split in result)
            {
                for (int i = 0; i < 4; i++)
                {
                    int id = split.GetClassId(i);
                    if (id > 0) split.SetClassName(i, ReadClassName(id));
                }
            }
        }

        private string ReadClassName(int id)
        {
            string path = "carclasses.csv";
            try
            {
                if (System.IO.File.Exists(path))
                {
                    string[] lines = System.IO.File.ReadAllLines(path);
                    var line = (from r in lines where r.StartsWith(id + ";") select r).FirstOrDefault();
                    if (!String.IsNullOrWhiteSpace(line))
                    {
                        return line.Split(';').LastOrDefault().Trim();
                    }
                }
            }
            catch { }
            return null;
        }

        private void CheckAudit(Library.Data.Audit audit)
        {
            if (!audit.Success)
            {
                MessageBox.Show(audit.ToString());
            }
        }

        private void GridResult_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var split = gridResult.SelectedItem as Library.Data.Split;

            if (split == null)
            {
                tbxDetails.Text = "";
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("SPLIT " + split.Number + ", SoF " + split.GlobalSof);
            sb.AppendLine(" ");

            if (split.Class1Cars != null)
            {
                sb.AppendLine("CLASS " + split.Class1Name + ", SoF " + split.Class1Sof);
                foreach (var item in split.Class1Cars)
                {
                    sb.AppendLine(" - iR:" + item.rating + ". " + item.name);
                }
                sb.AppendLine(" ");
            }

            if (split.Class2Cars != null)
            {
                sb.AppendLine("CLASS " + split.Class2Name + ", SoF " + split.Class2Sof);
                foreach (var item in split.Class2Cars)
                {
                    sb.AppendLine(" - iR:" + item.rating + ". " + item.name);
                }
                sb.AppendLine(" ");
            }

            if (split.Class3Cars != null)
            {
                sb.AppendLine("CLASS " + split.Class3Name + ", SoF " + split.Class3Sof);
                foreach (var item in split.Class3Cars)
                {
                    sb.AppendLine(" - iR:" + item.rating + ". " + item.name);
                }
                sb.AppendLine(" ");
            }

            if (split.Class4Cars != null)
            {
                sb.AppendLine("CLASS " + split.Class4Name + ", SoF " + split.Class4Sof);
                foreach (var item in split.Class4Cars)
                {
                    sb.AppendLine(" - iR:" + item.rating + ". " + item.name);
                }
                sb.AppendLine(" ");
            }

            tbxDetails.Text = sb.ToString();



        }

        private void CboAlgorithm_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sspP == null) return;

            OnAlgorithmChanged();
        }

        private void OnAlgorithmChanged()
        {
            if (cboAlgorithm.SelectedIndex >= 0)
            {
                string strAlgo = (cboAlgorithm.SelectedItem as ComboBoxItem).Tag.ToString();
                BetterMatchMaking.Library.BetterMatchMakingCalculator calc = new Library.BetterMatchMakingCalculator(strAlgo);
                

                sspP.Visible = calc.UseParameterClassPropMinPercent;
                sspMinCars.Visible = calc.UseParameterMinCars;
                sspIR.Visible = calc.UseParameterRatingThreshold;
                sspMaxSofDiff.Visible = calc.UseParameterMaxSofDiff;
                sspMaxSofFunctStartingIRValue.Visible = calc.UseParameterMaxSofFunct;
                sspMaxSofFunctStartingThreshold.Visible = calc.UseParameterMaxSofFunct;
                sspMaxSofFunctExtraThresoldPerK.Visible = calc.UseParameterMaxSofFunct;
                sspTopSplitExc.Visible = calc.UseParameterTopSplitException;
                sspDebug.Visible = calc.UseParameterDebugFile;
                sspForceMidClass.Visible = calc.UseParameterNoMiddleClassesEmpty;
            }
            else
            {
                sspP.Visible = false;
                sspMinCars.Visible = false;
                sspIR.Visible = false;
                sspMaxSofDiff.Visible = false;
                sspMaxSofFunctStartingIRValue.Visible = false;
                sspMaxSofFunctStartingThreshold.Visible = false;
                sspMaxSofFunctExtraThresoldPerK.Visible = false;
                sspTopSplitExc.Visible = false;
                sspDebug.Visible = false;
                sspForceMidClass.Visible = false;

            }
        }

        private void btnDownloadCsv_Click(object sender, RoutedEventArgs e)
        {
            PopupDownloadCsv w = new PopupDownloadCsv();
            w.Owner = this;
            w.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if(w.ShowDialog()==true)
            {
                tbxRegistrationFile.Text = w.File;
                BtnLoadRegistrationFile_Click(sender, e);
            }
        }

        private void LblParameterMaxSoffDiff_MouseUp(object sender, MouseButtonEventArgs e)
        {
            PopupPreviewFunctionTable p = new PopupPreviewFunctionTable();
            p.Owner = this;
            p.StartingIR = sspMaxSofFunctStartingIRValue.Value;
            p.StartingThreshold = sspMaxSofFunctStartingThreshold.Value;
            p.ExtraThresoldPerK = sspMaxSofFunctExtraThresoldPerK.Value;
            
            p.Render();
            p.Show();
        }

        private void TextBlock_MouseUp(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start("http://board.ipitting.com/bettersplits/");
        }
    }
}

