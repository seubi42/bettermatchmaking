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

namespace BetterMatchMaking
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Data.CsvParser parser;

        public MainWindow()
        {
            InitializeComponent();
            parser = new Data.CsvParser();
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
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void BtnCompute_Click(object sender, RoutedEventArgs e)
        {
            int defaultFieldSizeValue = 45;

            int fieldSize = defaultFieldSizeValue;
            int.TryParse(tbxFieldSize.Text, out fieldSize);
            if(fieldSize == 0) fieldSize = defaultFieldSizeValue;
            tbxFieldSize.Text = fieldSize.ToString();


            Calc.IMatchMaking mm = null;

            string strAlgo = (cboAlgorithm.SelectedItem as ComboBoxItem).Tag.ToString();


            // instanciate the good algorithm
            var type = System.Reflection.Assembly.GetExecutingAssembly().GetType("BetterMatchMaking.Calc." + strAlgo);
            mm = Activator.CreateInstance(type) as Calc.IMatchMaking;
            
            mm.Compute(parser.DistinctCars, fieldSize);
            gridResult.ItemsSource = mm.Splits;
        }

        private void GridResult_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var split = gridResult.SelectedItem as Data.Split;

            if(split == null)
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
    }
}
