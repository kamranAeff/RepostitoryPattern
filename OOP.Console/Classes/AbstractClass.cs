namespace OOP.Console
{
    abstract class AbstractClass
    {
        protected internal abstract void ShowMessage();
        public void SendMessage(string message) {
            System.Console.Write("SendMessage: {0}",message);
        }
        public virtual void SendCourse(string message)
        {
            System.Console.Write("SendCourse: {0}", message);
        }
    }
}
