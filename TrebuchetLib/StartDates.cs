namespace TrebuchetLib;

public struct StartDates(int instance, DateTime date)
{
    public int Instance { get; set; } = instance;
    public DateTime Date { get; set; } = date;
}