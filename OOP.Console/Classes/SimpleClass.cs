using System;

namespace OOP.Console
{
    class SimpleClass : AbstractClass
    {
        public SimpleClass()
        {

        }

        int counter;

        public int Id { get; set; }

        private string StringDate()
        {
            return string.Format("{0:dd.MM.yyyy}",DateTime.Now);
        }

        protected internal override void ShowMessage()
        {
            throw new NotImplementedException();
        }
    }
}
