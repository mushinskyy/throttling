public class ThrottlingWindow
{
    public DateTime startTime { get; set; }
    public int counter;

    public ThrottlingWindow()
    {
        Reset();
    }

    public void Increase()
    {
        counter++;
    }

    public void Reset()
    {
        startTime = DateTime.Now;
        counter = 0;
    }

    public int Counter => counter;
}