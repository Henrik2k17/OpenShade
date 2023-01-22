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
using System.Windows.Shapes;

namespace OpenShade.Pages
{
    /// <summary>
    /// Interaktionslogik für TerrainLightingCompare.xaml
    /// </summary>
    public partial class TerrainLightingCompare : Window
    {

        int i = 1;
        public TerrainLightingCompare()
        {
            InitializeComponent();
        }

        private void CloseBTN_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void PrevBTN_Click(object sender, RoutedEventArgs e)
        {
            i--; // this will decrease 1 from i


            // if the value of i is less than 1
            // then give i the value of 6
            if (i < 1)
            {
                i = 3;
            }

            // change the picture according to the i's value
            picHolder.Source = new BitmapImage(new Uri(@"/Resources/Images/TerrainReflectance/Custom/" + i + ".png", UriKind.Relative));
        }

        private void XextBTN_Click(object sender, RoutedEventArgs e)
        {

            i++; // increase i by 1

            // if i's value gets larger than 6 then reset i back to 1

            if (i > 3)
            {
                i = 1;
            }

            // change the picture according to the i's value
            picHolder.Source = new BitmapImage(new Uri(@"/Resources/Images/TerrainReflectance/Custom/" + i + ".png", UriKind.Relative));


        }
    }
}
