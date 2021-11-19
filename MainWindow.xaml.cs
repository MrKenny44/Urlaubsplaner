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
            DPEndDatum.SelectedDate = DateTime.Now;
            DPStartDatum.SelectedDate = DateTime.Now;
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

        public class VacationDay
        {
            public DateTime Tag { get; set; }
            public float Stunden { get; set; }
            public DateTime Startzeitpunkt { get; set; }
            public DateTime Endzeitpunkt { get; set; }
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
            bool? changeHours = cbStundenbearbeiten.IsChecked;

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

            float tHours = CalcStunden(CreateVacationDaysList(tStartDate, tEndDate), changeHours);

            Vacation urlaub = new Vacation()
            {
                StartDate = tStartDate,
                EndDate = tEndDate,
                Hours = tHours,
                Taken = false
            };

            return urlaub;
        }

        private List<VacationDay> CreateVacationDaysList(DateTime Start, DateTime End)
        {
            float lHours = 0;

            float lHalfHollidayHours, lHalfHollidayHoursFriday, lPauseHours;

            int lDays;
            DateTime EndTime, StartTime;
            List<VacationDay> vacationDays = new List<VacationDay>();

            //calculate the days from end to start
            lDays = (End - Start).Days + 1;

            //loops through each day between Start and End and summed up the hours.
            for (int i = 0; i < lDays; i++)
            {
                lHalfHollidayHoursFriday = 0;
                lHalfHollidayHours = 0;
                EndTime = Start.AddDays(i);
                StartTime = Start.AddDays(i).AddHours(7);
                lPauseHours = 0;

                if (IsHolidayOrWeekend(Start.AddDays(i)))

                {
                    lHours = 0;
                    StartTime = StartTime.AddHours(-7);
                }
                else
                {
                    if (Start.AddDays(i).DayOfWeek != DayOfWeek.Friday)
                    {
                        lHours += 8.75f;
                    }
                    else
                    {
                        if (IsHalfHolliday(Start.AddDays(i)))
                        {
                            lHalfHollidayHoursFriday = 1;
                            lHalfHollidayHours = 4.75f;
                            lPauseHours = 0.75f;
                        }
                        if (Start.AddDays(i).DayOfWeek != DayOfWeek.Friday)
                        {
                            lHours += 8.75f - lHalfHollidayHours;
                            EndTime = EndTime.AddHours(16.75 - lHalfHollidayHours - lPauseHours);
                        }
                        else
                        {
                            lHours += 5 - lHalfHollidayHoursFriday;
                            EndTime = EndTime.AddHours(12.25 - lHalfHollidayHoursFriday);
                        }
                    }
                    VacationDay vacationDay = new VacationDay()
                    {
                        Tag = Start.AddDays(i),
                        Stunden = lHours,
                        Startzeitpunkt = StartTime,
                        Endzeitpunkt = EndTime
                    };
                    vacationDays.Add(vacationDay);
                    lHours = 0;
                }
            }
            return vacationDays;
        }

        private float CalcStunden(List<VacationDay> vacationDays, bool? changeHours)
        {
            float houres = 0;
            if (changeHours == true)
            {
                ChangeHours(vacationDays);
            }
            else
            {
                foreach (VacationDay day in vacationDays)
                {
                    houres += day.Stunden;
                }
            }
            return houres;
        }

        private float ChangeHours(List<VacationDay> vacationDays)
        {
            Window1 urlaubBearbeiten = new Window1();
            urlaubBearbeiten.DGUrlaubstage.ItemsSource = vacationDays;
            urlaubBearbeiten.Show();
            return 0;
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

        private bool IsHollyday(DateTime pDate)
        {
            if (hollidays == null)
            {
                return false;
            }
            DateTime date;
            foreach (var child in hollidays.Children())
            {
                foreach (var innerCild in child.Children())
                {
                    date = Convert.ToDateTime(innerCild["datum"]);

                    if (date.ToShortDateString() == pDate.ToShortDateString())
                    {
                        return true;
                    }
                }
            } return false;
        }

        private bool IsHalfHolliday(DateTime pDate)
        {
            //create "half" hollidays
            DateTime halfday1 = new DateTime(pDate.Year, 12, 24);
            DateTime halfday2 = new DateTime(pDate.Year, 12, 31);

            if (pDate.DayOfYear == halfday1.DayOfYear || pDate.DayOfYear == halfday2.DayOfYear)
            {
                return true;
            }
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

