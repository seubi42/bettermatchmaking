using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace BetterMatchMaking
{
    public class SyncSliderBox
    {
        public TextBlock Label { get; private set; }
        public TextBox Tbx { get; private set; }
        public Slider Sld { get; private set; }

        public int Min { get; private set; }
        public int Max { get; private set; }
        public int DefaultValue { get; private set; }


        public int Value
        {
            get
            {
                return Convert.ToInt32(Sld.Value);
            }
            set
            {
                Sld.Focus();
                Sld.Value = value;
            }
        }

        public bool Visible
        {
            get
            {
                return Sld.Visibility == System.Windows.Visibility.Visible;
            }
            set
            {
                if (value)
                {
                    Label.Visibility = System.Windows.Visibility.Visible;
                    Sld.Visibility = System.Windows.Visibility.Visible;
                    Tbx.Visibility = System.Windows.Visibility.Visible;
                }
                else
                {
                    Label.Visibility = System.Windows.Visibility.Collapsed;
                    Sld.Visibility = System.Windows.Visibility.Collapsed;
                    Tbx.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
        }

        public SyncSliderBox(TextBlock label, TextBox tbx, Slider sld, int min, int max, int defaultValue)
        {
            Label = label;

            Tbx = tbx;
            Sld = sld;

            Min = min;
            Max = max;
            DefaultValue = defaultValue;

            sld.Minimum = min;
            sld.Maximum = max;
            sld.Value = defaultValue;
            tbx.Text = defaultValue.ToString();

            sld.ValueChanged += Sld_ValueChanged;
            tbx.KeyUp += Tbx_KeyUp;
            tbx.TextChanged += Tbx_TextChanged;
        }



        private void Tbx_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Tbx.IsFocused)
                OnTextChange();
        }

        private void Tbx_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {

            if (e.Key == System.Windows.Input.Key.Up || e.Key == System.Windows.Input.Key.Down)
            {
                string txtWithoutDigits = CleanStringOfNonDigits(Tbx.Text);
                if (Tbx.Text != txtWithoutDigits)
                {
                    Tbx.Text = txtWithoutDigits;
                }


                int tbxValue = -1;
                if (int.TryParse(Tbx.Text, out tbxValue))
                {
                    if (Max < 100)
                    {
                        if (e.Key == System.Windows.Input.Key.Up) tbxValue++;
                        else tbxValue--;
                    }
                    else
                    {
                        if (e.Key == System.Windows.Input.Key.Up) tbxValue += 100;
                        else tbxValue -= 100;
                    }

                    Tbx.Text = tbxValue.ToString();
                }
            }

            if (Tbx.IsFocused)
                OnTextChange();
        }

        private void Sld_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if(!Tbx.IsFocused)
                OnSliderChange();
        }

        private static readonly System.Text.RegularExpressions.Regex rxNonDigits = new System.Text.RegularExpressions.Regex(@"[^\d]+");
        private string CleanStringOfNonDigits(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            string cleaned = rxNonDigits.Replace(s, "");
            return cleaned;
        }


        private void OnTextChange()
        {
            string txtWithoutDigits = CleanStringOfNonDigits(Tbx.Text);
            if(Tbx.Text != txtWithoutDigits)
            {
                Tbx.Text = txtWithoutDigits;
            }


            int tbxValue = -1;
            if(int.TryParse(Tbx.Text, out tbxValue))
            {
                if(tbxValue>= Min && tbxValue <= Max)
                {
                    Sld.Value = tbxValue;
                }
                else
                {
                    if(tbxValue < Min) Sld.Value = Min;
                    if(tbxValue > Max) Sld.Value = Max;
                }
            }
        }

        private void OnSliderChange()
        {
            int tbxValue = -1;
            int.TryParse(Tbx.Text, out tbxValue);

            int sldValue = Convert.ToInt32(Sld.Value);

            if (tbxValue != sldValue) Tbx.Text = sldValue.ToString();
        }
    }
}
