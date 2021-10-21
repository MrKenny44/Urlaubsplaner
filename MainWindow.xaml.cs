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

namespace Urlaubsplaner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        public class Urlaub
        {
            public DateTime StartDatum { get; set; }
            public DateTime EndDatum { get; set; }
            public float Stunden { get; set; }
            public bool Genommen { get; set; }
        }

        public List<Urlaub> urlaubsliste = new List<Urlaub>();

        private void UrlaubEintagen(object sender, RoutedEventArgs e)
        {

            DGUrlaub.ItemsSource = urlaubsliste;
            urlaubsliste.Add(UrlaubErstellen());
            DGUrlaub.Items.Refresh();
        }

        private Urlaub UrlaubErstellen()
        {
            DateTime tStartDatum, tEndDatum;
            try
            {
                tStartDatum = DPStartDatum.SelectedDate.Value;
                tEndDatum = DPEndDatum.SelectedDate.Value;
            }
            catch (Exception)
            {
                MessageBox.Show("Bitte ein gültiges Datum angeben");
                throw;
            }

            int tStunden = CalcStunden(tStartDatum,tEndDatum);
            bool tGenommen = false;

            Urlaub urlaub = new Urlaub()
            {
                StartDatum = tStartDatum,
                EndDatum = tEndDatum,
                Stunden = tStunden,
                Genommen = tGenommen
            };
            return urlaub;
        }

        private int CalcStunden(DateTime Start, DateTime Ende)
        {
            int stunden = 0;
            int tage;

            tage = (Ende - Start).Days +1;

            for (int i = 0; i < tage; i++)
            {
                stunden += 8;
            }
            return stunden;
        }
    }
}
