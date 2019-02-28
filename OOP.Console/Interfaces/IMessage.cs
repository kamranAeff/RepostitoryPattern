namespace OOP.Console
{
    public interface IMessage
    {
        int ValueProvider { get; set; }
        void Print(string message);
    }
}
