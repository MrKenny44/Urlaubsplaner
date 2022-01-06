using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using static Urlaubsplaner.MainWindow;

namespace Urlaubsplaner
{
    /// <summary>
    /// Interaktionslogik für Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        private List<Vacation> vacations;
        private DataGrid datagridMain;

        public Window1(List<Vacation> vacations, DataGrid dataGrid)
        {
            InitializeComponent();
            this.vacations = vacations;
            this.datagridMain = dataGrid;
        }

        private void OnAutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyType == typeof(System.DateTime))
            {
                if (e.Column.Header.ToString() == "Startzeitpunkt" || e.Column.Header.ToString() == "Endzeitpunkt")
                {
                    (e.Column as DataGridTextColumn).Binding.StringFormat = "HH:mm";
                }
                else
                {
                    if (e.Column.Header.ToString() == "Tag")
                    {
                        (e.Column as DataGridTextColumn).Binding.StringFormat = "dddd, dd.MM.yyyy";
                        e.Column.IsReadOnly = true;
                    }
                }
            }            
        }

        private void DGUrlaubstage_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                DataGridCellInfo currentCell = DGUrlaubstage.CurrentCell;
                string editingElmementValue = (e.EditingElement as TextBox).Text.ToString();
                VacationDay vacationDay;
                DateTime roundedTime; 
                if (e.Column.Header.ToString() == "Startzeitpunkt" || e.Column.Header.ToString() == "Endzeitpunkt")
                {
                    try
                    {
                        roundedTime = TimeRound(Convert.ToDateTime(editingElmementValue));
                        vacationDay = currentCell.Item as VacationDay;
                        (e.EditingElement as TextBox).Text = roundedTime.ToString("HH:mm");
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Bitte geben Sie die Zeit richtig an");
                        return; ;
                    }

                    if (e.Column.Header.ToString() == "Startzeitpunkt")
                    {
                        vacationDay.Startzeitpunkt = roundedTime;
                    }
                    else
                    {
                        vacationDay.Endzeitpunkt = roundedTime;
                    }

                    if (vacationDay != null)
                    {
                        vacationDay.Stunden = CalcHours(vacationDay.Startzeitpunkt, vacationDay.Endzeitpunkt);
                    }
                }
            }
        }

        private DateTime TimeRound(DateTime dateTime)
        {
            int minute = dateTime.Minute;
            if (minute > 45)
            {
                dateTime = dateTime.AddMinutes(60 - minute);
                dateTime = dateTime.AddHours(1);
            }
            else if (minute > 30)
            {
                dateTime = dateTime.AddMinutes(45 - minute);
            }
            else if (minute > 15)
            {
                dateTime = dateTime.AddMinutes(30 - minute);
            }
            else if (minute > 0)
            {
                dateTime = dateTime.AddMinutes(15 - minute);
            }

            return dateTime;
        }

        private float CalcHours(DateTime startTime, DateTime endTime)
        {
            bool breakfastBreak = false;
            bool luchBreak = false;
            float hours;

            //this line of code ensures that the condition for the breakfast and lunch break works properly
            //Editing the start and end time will otherwise cause this condition to break.
            DateTime tempDateTimeEnd = new DateTime(startTime.Year, startTime.Month, startTime.Day, endTime.Hour, endTime.Minute, endTime.Second);

            DateTime tempDate = endTime;
            DateTime breakfastStart = new DateTime(startTime.Year, startTime.Month, startTime.Day, 9, 0,0);
            DateTime breakfastEnd = new DateTime(startTime.Year, startTime.Month, startTime.Day, 9, 15, 0);
            DateTime lunchStart = new DateTime(startTime.Year, startTime.Month, startTime.Day, 12, 15, 0);
            DateTime lunchEnd = new DateTime(startTime.Year, startTime.Month, startTime.Day, 13, 00, 0);

            //will not work if the start or endtime is set between "BreakStart" and "BreakEnd"
            if (startTime <= breakfastStart && tempDateTimeEnd >= breakfastEnd)
            {
                breakfastBreak = true;
            }
            if (startTime <= lunchStart && tempDateTimeEnd >= lunchEnd)
            {
                luchBreak = true;
            }

            tempDate = tempDate.AddMinutes(-startTime.Minute);
            tempDate = tempDate.AddHours(-startTime.Hour);
            if (breakfastBreak)
            {
                tempDate = tempDate.AddMinutes(-15);
            }
            if (luchBreak)
            {
                tempDate = tempDate.AddMinutes(-45);
            }
            hours = tempDate.Hour;
            if (tempDate.Minute == 15)
            {
                hours += 0.25f;
            }
            else if (tempDate.Minute == 30)
            {
                hours += 0.5f;
            }
            else if (tempDate.Minute ==45)
            {
                hours += 0.75f;
            }
            return hours;
        }

        private void DGUrlaubstage_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            //funktioniert nur beim wechseln der Zeile
            DGUrlaubstage.Items.Refresh();
        }

        private void Speichern(object sender, RoutedEventArgs e)
        {
            NewEntryInMain();
            this.Close();
        }

        private void NewEntryInMain()
        {
            float hours;
            List<VacationDay> vacationDays = new List<VacationDay>();

            DGUrlaubstage.Items.Refresh();
            foreach (var item in DGUrlaubstage.ItemsSource)
            {
                vacationDays.Add((item as VacationDay));
            }
            MainWindow main = new MainWindow();
            hours = main.CalcStunden(vacationDays, false);

            //The editing of the Vacation.Startzeit or Vacation.Endzeit changes the date of the entry to the current one. The next lines are to compat this behaviour
            //tempStarTime and tempEndTime combinat the Vacation.Startzeit (or Endzeit) with Vaction.Tag to ensure that the rigth date is used.
            VacationDay startDay = (DGUrlaubstage.Items[0] as VacationDay);
            VacationDay endDay = (DGUrlaubstage.Items[DGUrlaubstage.Items.Count - 1] as VacationDay);
            DateTime tempStartTime = new DateTime(startDay.Tag.Year, startDay.Tag.Month, startDay.Tag.Day, startDay.Startzeitpunkt.Hour, startDay.Startzeitpunkt.Minute, startDay.Startzeitpunkt.Second);
            DateTime tempEndTime = new DateTime(endDay.Tag.Year, endDay.Tag.Month, endDay.Tag.Day, endDay.Endzeitpunkt.Hour, endDay.Endzeitpunkt.Minute, endDay.Endzeitpunkt.Second);

            Vacation vacation = new Vacation()
            {
                Startdatum = tempStartTime,
                Enddatum = tempEndTime,
                Stunden = hours,
                Genommen = false
            };

            vacations.Add(vacation);
            datagridMain.Items.Refresh();

            main.Close();
            DGUrlaubstage.ItemsSource = null;
            DGUrlaubstage.Items.Clear();
        }
    }
}