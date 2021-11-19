using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using static Urlaubsplaner.MainWindow;

namespace Urlaubsplaner
{
    /// <summary>
    /// Interaktionslogik für Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        List<DateTime> TimeTable;
        public Window1()
        {
            InitializeComponent();
            TimeTable = new List<DateTime>();
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
                VacationDay vacationDay = null;
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
                        vacationDay.Stunden = CalcHoures(vacationDay.Startzeitpunkt, vacationDay.Endzeitpunkt);
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

        private float CalcHoures(DateTime startTime, DateTime endTime)
        {
            bool breakfastBreak = false;
            bool luchBreak = false;

            DateTime breakfastStart = new DateTime(startTime.Year, startTime.Month, startTime.Day, 9, 0,0);
            DateTime breakfastEnd = new DateTime(startTime.Year, startTime.Month, startTime.Day, 9, 15, 0);
            DateTime lunchStart = new DateTime(startTime.Year, startTime.Month, startTime.Day, 12, 15, 0);
            DateTime lunchEnd = new DateTime(startTime.Year, startTime.Month, startTime.Day, 13, 00, 0);

            if (startTime <= breakfastStart && endTime >= breakfastEnd)
            {
                breakfastBreak = true;
            }
            if (startTime <= lunchStart && endTime >= lunchEnd)
            {
                luchBreak = true;
            }

            float houres = (float)(endTime - startTime).TotalHours;
            if (breakfastBreak)
            {
                houres -= 0.25f;
            }
            if (luchBreak)
            {
                houres -= 0.45f;
            }
            return houres;
        }

        private void DGUrlaubstage_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            //funktioniert nur beim wechseln der Zeile
            DGUrlaubstage.Items.Refresh();
        }
    }
}