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

        public MainWindow()
        {
            InitializeComponent();
            parser = new Library.Data.CsvParser();

            sspP = new SyncSliderBox(lblParameterP, tbxParameterP, sldParameterP, 1, 66, 37);
            sspIR = new SyncSliderBox(lblParameterIR, tbxParameterIR, sldParameterIR, 800, 3200, 1900);
            sspMaxSofDiff = new SyncSliderBox(lblParameterMaxSoffDiff, tbxParameterMaxSoffDiff, sldParameterMaxSoffDiff, 1, 100, 18);
            sspMaxSofFx = new SyncSliderBox(lblParameterMaxSoffFunctX, tbxParameterMaxSoffFunctX, sldParameterMaxSoffFunctX, 0, 9999, 1000);
            sspMaxSofFa = new SyncSliderBox(lblParameterMaxSoffFunctA, tbxParameterMaxSoffFunctA, sldParameterMaxSoffFunctA, 0, 150, 12);
            sspMaxSofFb = new SyncSliderBox(lblParameterMaxSoffFunctB, tbxParameterMaxSoffFunctB, sldParameterMaxSoffFunctB, -50, 50, -20);
            sspTopSplitExc = new SyncSliderBox(lblParameterTopSplitExc, tbxParameterTopSplitExc, sldParameterTopSplitExc, 0, 1, 1);

            sspP.Visible = false;
            sspIR.Visible = false;
            sspMaxSofDiff.Visible = false;
            sspMaxSofFx.Visible = false;
            sspMaxSofFa.Visible = false;
            sspMaxSofFb.Visible = false;
            sspTopSplitExc.Visible = false;

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
            if (fieldSize == 0) fieldSize = defaultFieldSizeValue;
            tbxFieldSize.Text = fieldSize.ToString();


            Library.Calc.IMatchMaking mm = null;

            string strAlgo = (cboAlgorithm.SelectedItem as ComboBoxItem).Tag.ToString();


            // instanciate the good algorithm
            var calc = new BetterMatchMaking.Library.BetterMatchMakingCalculator(strAlgo);
            mm = calc.Calculator;
            mm.ParameterPValue = sspP.Value;
            mm.ParameterIRValue = sspIR.Value;
            mm.ParameterMaxSofDiff = sspMaxSofDiff.Value;
            mm.ParameterMaxSofFunctA = sspMaxSofFa.Value;
            mm.ParameterMaxSofFunctX = sspMaxSofFx.Value;
            mm.ParameterMaxSofFunctB = sspMaxSofFb.Value;
            mm.ParameterTopSplitException = sspTopSplitExc.Value;

            DateTime dtStart = DateTime.Now;
            mm.Compute(parser.DistinctCars, fieldSize);
            var time = Convert.ToInt32(DateTime.Now.Subtract(dtStart).TotalMilliseconds);
            gridResult.ItemsSource = mm.Splits;
            result = mm.Splits;

            AddClassNamesToResults(result);

            double pcent = 0;
            int splitsHavingDiffClassesSof = (from r in mm.Splits where r.ClassesSofDiff > 0 select r.ClassesSofDiff).Count();
            if(splitsHavingDiffClassesSof > 0) pcent = Math.Round((from r in mm.Splits where r.ClassesSofDiff > 0 select r.ClassesSofDiff).Average());
            string morestats = mm.Splits.Count + " splits. ";
            morestats += (from r in mm.Splits select r.AllCars.Count).Sum() + " cars. ";
            morestats += "Computed in " + time + " ms. Average split car classes difference: ";
            morestats += pcent + "%";
            tbxStats.Text = morestats;

            tbxStats.Background = ColorConverter.GetPercentColor(Convert.ToInt32(pcent));


            CheckNobodyIsMissing();
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

        private void CheckNobodyIsMissing()
        {
            int missingInList = 0;

            List<int> allcars = (from r in parser.DistinctCars select r.car_id).ToList();
            foreach (var split in result)
            {
                foreach (var car in split.AllCars)
                {
                    int car_id = car.car_id;
                    if(allcars.Contains(car_id))
                    {
                        allcars.Remove(car_id);
                    }
                    else
                    {
                        missingInList++;
                    }
                }
            }



            if(allcars.Count > 0 || missingInList > 0)
            {
                MessageBox.Show("Error in algorithm, cars are missing !");
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
                var mm = calc.Calculator as Library.Calc.IMatchMaking;

                sspP.Visible = mm.UseParameterP;
                sspIR.Visible = mm.UseParameterIR;
                sspMaxSofDiff.Visible = mm.UseParameterMaxSofDiff;
                sspMaxSofFa.Visible = mm.UseParameterMaxSofDiff;
                sspMaxSofFb.Visible = mm.UseParameterMaxSofDiff;
                sspMaxSofFx.Visible = mm.UseParameterMaxSofDiff;
                sspTopSplitExc.Visible = mm.UseParameterTopSplitException;
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

