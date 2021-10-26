using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Windows;

namespace Urlaubsplaner
{

    public partial class MainWindow : Window
    {

        public JObject hollidays;

        public struct Feiertag
        {
            public DateTime datum;
            public string hinweis;
        }

        private List<Vacation> vacationList = new List<Vacation>();

        public MainWindow()
        {
            InitializeComponent();
            //sets the Datagrid Item Source
            DGUrlaub.ItemsSource = vacationList;

            hollidays = RequestApi();
            IsHollyday(DateTime.Now);
        }
        public class Vacation
        {
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public float Hours { get; set; }
            public bool Taken { get; set; }
        }

        private void UrlaubEintagen(object sender, RoutedEventArgs e)
        {
            //Adds a new vacation object to the list
            Vacation tVaction = InstantiateHollyday();
            if (tVaction != null)
            {
                vacationList.Add(tVaction);
                //Updates the Datagrid
                DGUrlaub.Items.Refresh();
            }
        }

        private Vacation InstantiateHollyday()
        {
            DateTime tStartDate, tEndDate;
            try
            {
                tStartDate = DPStartDatum.SelectedDate.Value;
                tEndDate = DPEndDatum.SelectedDate.Value;
            }
            catch (Exception)
            {
                MessageBox.Show("Bitte ein gültiges Datum angeben");
                return null;
            }

            float tHours = CalcStunden(tStartDate, tEndDate);
            bool tTaken = false;

            Vacation urlaub = new Vacation()
            {
                StartDate = tStartDate,
                EndDate = tEndDate,
                Hours = tHours,
                Taken = tTaken
            };
            return urlaub;
        }

        private float CalcStunden(DateTime Start, DateTime End)
        {
            float lHours = 0;
            int lDays;

            //calculate the days from end to start
            lDays = (End - Start).Days + 1;

            //loops through each day between Start and End and summed up the hours.
            for (int i = 0; i < lDays; i++)
            {
                if (IsHolidayOrWeekend(Start.AddDays(i)))
                {
                    lHours += 0;
                }
                else
                {
                    if (Start.AddDays(i).DayOfWeek != DayOfWeek.Friday)
                    {
                        lHours += 8.75f;
                    }
                    else
                    {
                        lHours += 5;
                    }
                }
            }
            return lHours;
        }

        private bool IsHolidayOrWeekend(DateTime tDay)
        {
            //Checks, if the given day is either a holiday or a day in the weekend.
            if (tDay.DayOfWeek == DayOfWeek.Saturday || tDay.DayOfWeek == DayOfWeek.Sunday || IsHollyday(tDay))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool IsHollyday(DateTime lDate)
        {
            return false;
        }

        private JObject RequestApi()
        {
            string lUrlString = "https://feiertage-api.de/api/?jahr=2021&nur_land=NI";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(lUrlString);
            WebResponse response = request.GetResponse();

            using (Stream dataStream = response.GetResponseStream())
            {
                // Open the stream using a StreamReader for easy access.
                StreamReader reader = new StreamReader(dataStream);
                // Read the content.
                string responseFromServer = reader.ReadToEnd();

                JObject json = JObject.Parse(responseFromServer);
                return json;

            }
        }
    }
}
