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
                        vacationDay = RepairVacationDayData(vacationDay);
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

            DateTime tempDate = endTime;
            DateTime breakfastStart = new DateTime(startTime.Year, startTime.Month, startTime.Day, 9, 0,0);
            DateTime breakfastEnd = new DateTime(startTime.Year, startTime.Month, startTime.Day, 9, 15, 0);
            DateTime lunchStart = new DateTime(startTime.Year, startTime.Month, startTime.Day, 12, 15, 0);
            DateTime lunchEnd = new DateTime(startTime.Year, startTime.Month, startTime.Day, 13, 00, 0);

            //will not work if the start or endtime is set between "BreakStart" and "BreakEnd"
            if (startTime <= breakfastStart && endTime >= breakfastEnd)
            {
                breakfastBreak = true;
            }
            if (startTime <= lunchStart && endTime >= lunchEnd)
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
            //Only triggers when you change to a diffrent cell after eddeting
            DGUrlaubstage.Items.Refresh();
        }

        private void Speichern(object sender, RoutedEventArgs e)
        {
            NewEntryInMain();
            this.Close();
        }

        private void NewEntryInMain()
        {
            //This function loops trough all days of the vacation. If there is a diffrent start time than default and the current day is not the first day it will add the counter to
            //the List "splitPoints". This also applies when the end time is diffent to the deafult and the current day is not the last day. 
            float hours, summHours = 0;
            int i = 0;
            int counterVacationDays;
            int startIndex = 0;
            List<int> splitPoints = GenerateSplitPoints();
            List<VacationDay> vacationDays;

            MainWindow main = new MainWindow();
            do
            {
                vacationDays = CreateVacationDayList(splitPoints[i]);
                counterVacationDays = vacationDays.Count;
                VacationDay startDay = vacationDays[startIndex];
                VacationDay endDay = vacationDays[vacationDays.Count - 1];
                startDay = RepairVacationDayData(startDay);
                endDay = RepairVacationDayData(endDay);

                hours = main.CalcStunden(vacationDays, false) -summHours;
                summHours = summHours + hours;
                //startIndex += vacationDays.Count;
                i++;

                startIndex = splitPoints[i - 1] + 1;

                Vacation vacation = new Vacation()
                {
                    Startdatum = startDay.Startzeitpunkt,
                    Enddatum = endDay.Endzeitpunkt,
                    Stunden = hours,
                    Genommen = false
                };

                vacations.Add(vacation);
                datagridMain.Items.Refresh();

            } while (splitPoints.Count >i);

            main.Close();
            DGUrlaubstage.ItemsSource = null;
            DGUrlaubstage.Items.Clear();
        }

        private List<VacationDay> CreateVacationDayList(int split)
        {
            int i = 0;
            List<VacationDay> vacationDays = new List<VacationDay>();

            DGUrlaubstage.Items.Refresh();
            foreach (var item in DGUrlaubstage.ItemsSource)
            {
                if (i <= split)
                {
                    vacationDays.Add((item as VacationDay));
                }
                if (i == split)
                {
                    break;
                }
                i++;
            }

            return vacationDays;
        }

        private List<int> GenerateSplitPoints()
        {
            VacationDay tempDay;
            List<int> splitPoints = new List<int>();

            for (int i = 0; i < DGUrlaubstage.Items.Count; i++)
            {

                tempDay = (DGUrlaubstage.Items[i] as VacationDay);
                tempDay = RepairVacationDayData(tempDay);

                DateTime tempRegularStartTime = new DateTime(tempDay.Tag.Year, tempDay.Tag.Month, tempDay.Tag.Day, 7, 0, 0);
                DateTime tempRegularEndTimeFriday = new DateTime(tempDay.Tag.Year, tempDay.Tag.Month, tempDay.Tag.Day, 12, 15, 0);
                DateTime tempRegularEndTime = new DateTime(tempDay.Tag.Year, tempDay.Tag.Month, tempDay.Tag.Day, 16, 45, 0);

                if (i < DGUrlaubstage.Items.Count -1)
                {
                    if (i != 0)
                    {
                        if (tempDay.Startzeitpunkt > tempRegularStartTime)
                        {
                            if (!splitPoints.Contains(i-1))
                            {
                                splitPoints.Add(i-1);
                            }
                        }
                    }
                    if (tempDay.Tag.DayOfWeek != DayOfWeek.Friday && tempDay.Endzeitpunkt < tempRegularEndTime)
                    {
                        if (!splitPoints.Contains(i))
                        {
                            splitPoints.Add(i);
                        }
                    }
                    else
                    {
                        if (tempDay.Tag.DayOfWeek == DayOfWeek.Friday && tempDay.Endzeitpunkt < tempRegularEndTimeFriday)
                        {
                            if (!splitPoints.Contains(i))
                            {
                                splitPoints.Add(i);
                            }
                        }
                    }
                }
            }

            splitPoints.Add(100);
            return splitPoints;
        }

        private VacationDay RepairVacationDayData(VacationDay tempDay)
        {
            //The editing of the Vacation.Startzeit or Vacation.Endzeit changes the date of the entry to the current one. The next lines are to compat this behaviour
            //tempVacationStarTime and tempVacationEndTime combinat the Vacation.Startzeit (or Endzeit) with Vaction.Tag to ensure that the rigth date is used.
            DateTime tempVacatioStartTime = new DateTime(tempDay.Tag.Year, tempDay.Tag.Month, tempDay.Tag.Day, tempDay.Startzeitpunkt.Hour, tempDay.Startzeitpunkt.Minute, 0);
            DateTime tempVacationEndTime = new DateTime(tempDay.Tag.Year, tempDay.Tag.Month, tempDay.Tag.Day, tempDay.Endzeitpunkt.Hour, tempDay.Endzeitpunkt.Minute, 0);
            tempDay.Startzeitpunkt = tempVacatioStartTime;
            tempDay.Endzeitpunkt = tempVacationEndTime;
            return tempDay;
        }

    }
}