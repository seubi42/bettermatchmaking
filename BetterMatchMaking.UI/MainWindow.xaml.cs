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
        SyncSliderBox sspIR;
        SyncSliderBox sspMaxSofDiff;
        SyncSliderBox sspMaxSofFx;
        SyncSliderBox sspMaxSofFa;
        SyncSliderBox sspMaxSofFb;
        SyncSliderBox sspTopSplitExc;
        SyncSliderBox sspEqualize;

        public MainWindow()
        {
            InitializeComponent();
            parser = new Library.Data.CsvParser();

            sspP = new SyncSliderBox(lblParameterP, tbxParameterP, sldParameterP, 5, 66, 37);
            sspIR = new SyncSliderBox(lblParameterIR, tbxParameterIR, sldParameterIR, 800, 3200, 1900);
            sspMaxSofDiff = new SyncSliderBox(lblParameterMaxSoffDiff, tbxParameterMaxSoffDiff, sldParameterMaxSoffDiff, 5, 100, 18);
            sspMaxSofFx = new SyncSliderBox(lblParameterMaxSoffFunctX, tbxParameterMaxSoffFunctX, sldParameterMaxSoffFunctX, 0, 9999, 1000);
            sspMaxSofFa = new SyncSliderBox(lblParameterMaxSoffFunctA, tbxParameterMaxSoffFunctA, sldParameterMaxSoffFunctA, 0, 150, 12);
            sspMaxSofFb = new SyncSliderBox(lblParameterMaxSoffFunctB, tbxParameterMaxSoffFunctB, sldParameterMaxSoffFunctB, -50, 50, -20);
            sspTopSplitExc = new SyncSliderBox(lblParameterTopSplitExc, tbxParameterTopSplitExc, sldParameterTopSplitExc, 0, 1, 0);
            sspEqualize = new SyncSliderBox(lblParameterEqualize, tbxParameterEqualize, sldParameterEqualize, 0, 1, 1);

            sspP.Visible = false;
            sspIR.Visible = false;
            sspMaxSofDiff.Visible = false;
            sspMaxSofFx.Visible = false;
            sspMaxSofFa.Visible = false;
            sspMaxSofFb.Visible = false;
            sspTopSplitExc.Visible = false;
            sspEqualize.Visible = false;


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
                if(tbxRegistrationFile.Text.StartsWith(openFileDialog.InitialDirectory + "\\"))
                {
                    tbxRegistrationFile.Text = openFileDialog.FileName.Substring(openFileDialog.InitialDirectory.Length + 1);
                }
            }
        }

        private void BtnLoadRegistrationFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // parse file
                parser.Read(tbxRegistrationFile.Text);


                // if -fieldsizeXX is in file name, get it
                string cst_fieldsize = "-fieldsize";
                if (tbxRegistrationFile.Text.Contains(cst_fieldsize))
                {
                    string fieldsize = tbxRegistrationFile.Text.Substring(
                        tbxRegistrationFile.Text.IndexOf(cst_fieldsize) + cst_fieldsize.Length,
                        2
                        );
                    tbxFieldSize.Text = fieldsize;
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
            bool nodata = false;
            if (parser.Data == null) nodata = true;
            if (parser.Data.Count == 0) nodata = true;

            if (nodata)
            {
                if (!String.IsNullOrWhiteSpace(tbxRegistrationFile.Text))
                {
                    BtnLoadRegistrationFile_Click(sender, e);
                }
                else
                {
                    return;
                }
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
            mm.ParameterRatingThresholdValue = sspIR.Value;
            mm.ParameterMaxSofDiffValue = sspMaxSofDiff.Value;
            mm.ParameterMaxSofFunctAValue = sspMaxSofFa.Value;
            mm.ParameterMaxSofFunctXValue = sspMaxSofFx.Value;
            mm.ParameterMaxSofFunctBValue = sspMaxSofFb.Value;
            mm.ParameterTopSplitExceptionValue = sspTopSplitExc.Value;
            mm.ParameterMostPopulatedClassInEverySplitsValue = sspEqualize.Value;

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
                sspIR.Visible = calc.UseParameterRatingThreshold;
                sspMaxSofDiff.Visible = calc.UseParameterMaxSofDiff;
                sspMaxSofFa.Visible = calc.UseParameterMaxSofDiff;
                sspMaxSofFb.Visible = calc.UseParameterMaxSofDiff;
                sspMaxSofFx.Visible = calc.UseParameterMaxSofDiff;
                sspTopSplitExc.Visible = calc.UseParameterTopSplitException;
                sspEqualize.Visible = calc.UseParameterMostPopulatedClassInEverySplits;
            }
            else
            {
                sspP.Visible = false;
                sspIR.Visible = false;
                sspMaxSofDiff.Visible = false;
                sspMaxSofFa.Visible = false;
                sspMaxSofFb.Visible = false;
                sspMaxSofFx.Visible = false;
                sspTopSplitExc.Visible = false;
                sspEqualize.Visible = false;

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
            p.X = sspMaxSofFx.Value;
            p.A = sspMaxSofFa.Value;
            p.B = sspMaxSofFb.Value;
            p.Min = sspMaxSofDiff.Value;
            p.Render();
            p.Show();
        }

        private void TextBlock_MouseUp(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start("http://board.ipitting.com/bettersplits/");
        }
    }
}

