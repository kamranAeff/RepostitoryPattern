namespace OOP.Console
{
    public struct Person
    {
        int a, b;
        public Person(int a,int b)
        {
            this.a = a;
            this.b = b;
        }
    }

    public struct Student : Person
    {
        int a, b;
        public Student(int a, int b)
        {
            this.a = a;
            this.b = b;
        }
    }
}
