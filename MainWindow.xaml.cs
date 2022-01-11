using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Xml;

namespace Urlaubsplaner
{

    public partial class MainWindow : Window
    {
        public JObject hollidays;
        public float Urlaubsstunden;
        private string Filename = "Urlaupsspeicher.xml";

        public class Vacation
        {
            public DateTime Startdatum { get; set; }
            public DateTime Enddatum { get; set; }
            public float Stunden { get; set; }
            public bool Genommen { get; set; }
        }

        public class VacationDay
        {
            public DateTime Tag { get; set; }
            public float Stunden { get; set; }
            public DateTime Startzeitpunkt { get; set; }
            public DateTime Endzeitpunkt { get; set; }
        }

        public List<Vacation> VacationList { get; set; } = new List<Vacation>();

        public MainWindow()
        {
            OnOpenProgramm();
            UpdateGui();
        }

        private void UrlaubEintagen(object sender, RoutedEventArgs e)
        {
            //Adds a new vacation object to the list
            Vacation tVaction = InstantiateVacation();
            DGUrlaub.Items.Refresh();
            if (tVaction != null)
            {
                if (tVaction.Stunden != 0)
                {
                    if (CheckVacation(tVaction))
                    {
                        VacationList.Add(tVaction);
                        //Updates the Datagrid
                        DGUrlaub.Items.Refresh();
                    }
                    else
                    {
                        MessageBox.Show("Das angegebene Datum ist bereits verplant");
                    }
                }
            }
            BtnOutlookExport.IsEnabled = true;
        }

        private void Speichern(object sender, RoutedEventArgs e)
        {
            UpdateGui();
            WriteXML();
        }

        private void OutlookExport(object sender, RoutedEventArgs e)
        {
            BtnOutlookExport.IsEnabled = false;
            Calendar calendar = new Calendar();
            foreach (var item in DGUrlaub.ItemsSource)
            {
                Vacation vacation = (item as Vacation);
                if (vacation.Enddatum.Hour == 0)
                {
                    calendar.NewEntry(vacation.Startdatum, vacation.Enddatum.AddDays(1), "Urlaub", "Urlaub");
                }
                else
                {
                    calendar.NewEntry(vacation.Startdatum, vacation.Enddatum, "Urlaub", "Urlaub");
                }
            }
        }

        private void OnOpenProgramm()
        {
            InitializeComponent();

            Urlaubsstunden = 240;
            DPEndDatum.SelectedDate = DateTime.Now;
            DPStartDatum.SelectedDate = DateTime.Now;
            //sets the Datagrid Item Source
            DGUrlaub.ItemsSource = VacationList;

            hollidays = RequestApi();
            IsHollyday(DateTime.Now);
            ReadXML();
        }

        private Vacation InstantiateVacation()
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
                Startdatum = tStartDate,
                Enddatum = tEndDate,
                Stunden = tHours,
                Genommen = false
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

                if (!IsHolidayOrWeekend(Start.AddDays(i)))
                {
                    if (IsHalfHolliday(Start.AddDays(i)))
                    {
                        lHalfHollidayHoursFriday = 2.5f;
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

        public float CalcStunden(List<VacationDay> vacationDays, bool? changeHours)
        {
            float hours = 0;
            if (changeHours == true)
            {
                ChangeHours(vacationDays);
            }
            else
            {
                foreach (VacationDay day in vacationDays)
                {
                    hours += day.Stunden;
                }
            }
            return hours;
        }

        private float CalcRestHollidayPlaned()
        {
            float hours = 0;
            foreach (var item in DGUrlaub.ItemsSource)
            {
                hours += (item as Vacation).Stunden;
            }
            return hours;
        }

        private float CalcRestHollidayActual()
        {
            float hours = 0;
            foreach (var item in DGUrlaub.ItemsSource)
            {
                if ((item as Vacation).Genommen)
                {
                    hours += (item as Vacation).Stunden;
                }
            }
            return hours;
        }

        private void ChangeHours(List<VacationDay> vacationDays)
        {
            // Opens a new formular to edit the given entry
            Window1 urlaubBearbeiten = new Window1(VacationList, DGUrlaub);
            urlaubBearbeiten.DGUrlaubstage.ItemsSource = vacationDays;
            urlaubBearbeiten.ShowDialog();
            urlaubBearbeiten.Close();
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
            //Checks if the given day is a hollyday
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
            //Checks if the given day is a "half-hollyday"

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

        private void OnAutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyType == typeof(System.DateTime))
            {
                if (e.Column.Header.ToString() == "Startdatum" || e.Column.Header.ToString() == "Enddatum")
                {
                    (e.Column as DataGridTextColumn).Binding.StringFormat = "dddd, dd.MM.yyyy";
                }
            }
        }

        public bool CheckVacation(Vacation vacation)
        {
            DateTime start = vacation.Startdatum;
            DateTime end = vacation.Enddatum;
            int i = 0;

            if (DGUrlaub.HasItems)
            {
                do
                {
                    foreach (var vacationDataGrid in DGUrlaub.ItemsSource)
                    {
                        Vacation date = (vacationDataGrid as Vacation);

                        if (date.Startdatum == vacation.Startdatum) return false;
                        if (date.Enddatum == vacation.Enddatum) return false;
                        if (date.Startdatum == vacation.Enddatum) return false;
                    }
                    i++;
                } while (start.AddDays(i).ToShortTimeString() != end.ToShortTimeString());
            }
            return true;
        }

        private void WriteXML()
        {
            XmlWriter xmlWriter;
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;

            try
            {
                xmlWriter = XmlWriter.Create(Filename, settings);
            }
            catch (Exception)
            {
                MessageBox.Show("Der angegebene Pfad ist nicht erreichbar");
                return;
            }

            xmlWriter.WriteStartDocument();
            xmlWriter.WriteStartElement("Urlaubstabelle");
            int i = 1;

            foreach (var item in DGUrlaub.ItemsSource)
            {
                Vacation vacation = (item as Vacation);
                xmlWriter.WriteStartElement("Urlaub" + i);
                xmlWriter.WriteElementString("Startdatum", vacation.Startdatum.ToString());
                xmlWriter.WriteElementString("Enddatum", vacation.Enddatum.ToString());
                xmlWriter.WriteElementString("Stunden", vacation.Stunden.ToString());
                xmlWriter.WriteElementString("Genommen", vacation.Genommen.ToString());
                xmlWriter.WriteEndElement();
                i++;
            }


            xmlWriter.WriteElementString("Reststunden_des_Vorjahres", txtReststundenDesVorjahres.Text);

            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndDocument();
            xmlWriter.Close();
        }

        private void ReadXML()
        {
            DateTime start = DateTime.Now, ende = DateTime.Now;
            bool taken = false;
            double hours = 0;
            string name;
            bool flagHolidayComplet = false;
            XmlReader xmlReader;

            try
            {
                xmlReader = XmlReader.Create(Filename);
            }
            catch (Exception)
            {
                MessageBox.Show("Die angegebene Datei kann nicht gefunden werden");
                return;
            }

            try
            {
                while (xmlReader.Read())
                {
                    // Do some work here on the data.
                    name = xmlReader.Name;
                    if (xmlReader.NodeType == XmlNodeType.Element)
                    {
                        switch (name)
                        {
                            case "Startdatum":
                                start = Convert.ToDateTime(xmlReader.ReadElementContentAsString());
                                break;
                            case "Enddatum":
                                ende = Convert.ToDateTime(xmlReader.ReadElementContentAsString());
                                break;
                            case "Stunden":
                                hours = Convert.ToDouble(xmlReader.ReadElementContentAsString());
                                break;
                            case "Genommen":
                                taken = Convert.ToBoolean(xmlReader.ReadElementContentAsString());
                                flagHolidayComplet = true;
                                break;
                            case "Reststunden_des_Vorjahres":
                                txtReststundenDesVorjahres.Text = xmlReader.ReadElementContentAsString();
                                break;
                        }
                    }
                    else if (xmlReader.NodeType == XmlNodeType.EndElement && flagHolidayComplet)
                    {
                        Vacation vacation = new Vacation()
                        {
                            Startdatum = start,
                            Enddatum = ende,
                            Stunden = (float)hours,
                            Genommen = taken
                        };
                        flagHolidayComplet = false;
                        VacationList.Add(vacation);
                        DGUrlaub.Items.Refresh();
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Beim lesen der XML Datei ist ein Fehler aufgetreten.");
                return;
            }
            
        }

        private void UpdateGui()
        {
            DGUrlaub.Items.Refresh();

            if (txtReststundenDesVorjahres.Text != "")
            {
                txtUrlaubsstunden.Text = Convert.ToString(Urlaubsstunden + Convert.ToInt32(txtReststundenDesVorjahres.Text));
            }

            txtGeplanteReststunden.Text = (Convert.ToDouble(txtUrlaubsstunden.Text) - CalcRestHollidayPlaned()).ToString();
            txtTatsaechlicheReststunden.Text = (Convert.ToDouble(txtUrlaubsstunden.Text) - CalcRestHollidayActual()).ToString();
        }
    }
}

