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
    /// Interaktionslogik für AdvancedPBRCompare.xaml
    /// </summary>
    public partial class RayleighScatteringCompare : Window
    {

        //int i = 1;
        public RayleighScatteringCompare()
        {
            InitializeComponent();
        }

        private void CloseBTN_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

    }
}
