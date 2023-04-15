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
    /// Interaktionslogik für TerrainSaturationCompare.xaml
    /// </summary>
    public partial class TerrainSaturationCompare : Window
    {

        //int i = 1;
        public TerrainSaturationCompare()
        {
            InitializeComponent();
        }

        private void CloseBTN_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        //private void PrevBTN_Click(object sender, RoutedEventArgs e)
        //{
        //    i--;
        //    if (i < 1)
        //    {
        //        i = 3;
        //    }
        //    picHolder.Source = new BitmapImage(new Uri(@"/Resources/Images/TerrainReflectance/Custom/" + i + ".png", UriKind.Relative));
        //}

        //private void XextBTN_Click(object sender, RoutedEventArgs e)
        //{
        //    i++;
        //    if (i > 3)
        //    {
        //        i = 1;
        //    }
        //    picHolder.Source = new BitmapImage(new Uri(@"/Resources/Images/TerrainReflectance/Custom/" + i + ".png", UriKind.Relative));
        //}
    }
}
