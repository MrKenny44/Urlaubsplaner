using System;
using Outlook = Microsoft.Office.Interop.Outlook;

public class Calendar
{
	public Calendar()
	{
    }

	public void NewEntry(DateTime start, DateTime end, string body, string subject)
    {
        Outlook.Application App = new Outlook.Application();
        Outlook.AppointmentItem newAppointment = (Outlook.AppointmentItem)App.CreateItem(Outlook.OlItemType.olAppointmentItem);
        newAppointment.Start = start;
        newAppointment.End = end;
        newAppointment.Body = body;
        newAppointment.AllDayEvent = false;
        newAppointment.Subject = subject;

        if (CheckForEntry(newAppointment))
        {
            newAppointment.Save();
        }
    }

    public bool CheckForEntry(Outlook.AppointmentItem entry)
    {
        Outlook.Application App = new Outlook.Application();
        Outlook.Folder calFolder = App.Session.GetDefaultFolder(Outlook.OlDefaultFolders.olFolderCalendar) as Outlook.Folder;


        DateTime start = entry.Start;
        DateTime end = entry.End;
        Outlook.Items rangeAppts = GetAppointmentsInRange(calFolder, start, end);
        if (rangeAppts != null)
        {
            foreach (Outlook.AppointmentItem appt in rangeAppts)
            {
                if (appt.Subject == "Urlaub")
                {
                    return false;
                }
            }
        }
        return true;
    }

    private Outlook.Items GetAppointmentsInRange(Outlook.Folder folder, DateTime startTime, DateTime endTime)
    {
        string filter = "[Start] >= '"+ startTime.ToString("g")+ "' AND [End] <= '"+ endTime.ToString("g") + "'";
        try
        {
            Outlook.Items calItems = folder.Items;
            calItems.IncludeRecurrences = true;
            calItems.Sort("[Start]", Type.Missing);
            Outlook.Items restrictItems = calItems.Restrict(filter);
            if (restrictItems.Count > 0)
            {
                return restrictItems;
            }
            else
            {
                return null;
            }
        }
        catch { return null; }
    }
}
