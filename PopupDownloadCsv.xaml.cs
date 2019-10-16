using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BetterMatchMaking
{
    /// <summary>
    /// Logique d'interaction pour PopupDownloadCsv.xaml
    /// </summary>
    public partial class PopupDownloadCsv : Window
    {
        public PopupDownloadCsv()
        {
            InitializeComponent();
        }

        string csv = "";
        string name = "";
        long raceid = 0;

        private void BtnAskIpitting_Click(object sender, RoutedEventArgs e)
        {
            string race = SyncSliderBox.CleanStringOfNonDigits(tbxRaceID.Text);
            if (!String.IsNullOrWhiteSpace(race))
            {
                raceid = Convert.ToInt64(race);
                if (raceid > 0)
                {
                    pg.Visibility = Visibility.Visible;

                    System.Threading.Thread t = new System.Threading.Thread(new System.Threading.ThreadStart(GetData));
                    t.Start();
                }
            }
        }


        private void GetData()
        {
            try
            {

                WebClient wc = new WebClient();
                string url = "http://api.ipitting.com/bettermatchmaking-service.php?q=" + raceid;
                string json = wc.DownloadString(url);
                dynamic ipitting = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
                csv = ipitting.csv;
                name = ipitting.name;

            }
            catch
            {

                csv = null;
                name = null;
                MessageBox.Show("Impossible to get results. Maybe the race is not available in ipitting, or the race id is wrong.");
            }

            Dispatcher.BeginInvoke(new Action(OnDataRetreived));
        }

        private void OnDataRetreived()
        {
            pg.Visibility = Visibility.Hidden;

            if(csv == null)
            {
                cbxDataAvailable.IsChecked = false;
                btnSave.Visibility = Visibility.Collapsed;
                gridResult.Visibility = Visibility.Collapsed;
            }
            else
            {
                lblName2.Text = "-" + name + "-fieldsize";
                cbxDataAvailable.IsChecked = true;
                btnSave.Visibility = Visibility.Visible;
                gridResult.Visibility = Visibility.Visible;
            }
        }

        public string File { get; private set; }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            int fieldsize = 45;
            int.TryParse(tbxFieldSize.Text, out fieldsize);

            string filename = tbxCustomName.Text;
            filename += "-";
            filename += name;
            filename += "-fieldsize";
            filename += fieldsize;
            filename += ".csv";

            System.IO.File.WriteAllText(filename, csv);
            File = filename;
            DialogResult = true;
        }
    }
}
