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
        public DataGridCellInfo currentCell;
        List<DateTime> TimeTable;
        public Window1()
        {
            InitializeComponent();
            TimeTable = new List<DateTime>();
            AddTime();
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

        private void OnCellChange(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Column.Header.ToString() == "Startzeitpunkt" || e.Column.Header.ToString() == "Endzeitpunkt")
            {
                float hours;
                DateTime startTime, endTime, day;
                //MessageBox.Show(e.Row.Item.ToString());
                VacationDay vacationDay = currentCell.Item as VacationDay;
                if (vacationDay != null)
                {
                    endTime = vacationDay.Endzeitpunkt;
                    startTime = vacationDay.Startzeitpunkt;
                    hours = vacationDay.Stunden;
                    day = vacationDay.Tag;
                }
            }
        }

        private void OnCellChangeBegin(object sender, DataGridBeginningEditEventArgs e)
        {
            if (e.Column.Header.ToString() == "Startzeitpunkt" || e.Column.Header.ToString() == "Endzeitpunkt")
            {
                currentCell = DGUrlaubstage.CurrentCell;
            }
        }

        private void AddTime()
        {
            TimeTable.Add(DateTime.Now);
            TimeTable.Add(DateTime.Now.AddHours(1));
        }
    }
}
