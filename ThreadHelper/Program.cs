using System;
using System.Threading;

namespace ThreadHelper
{
    class Program
    {
        static private int _num = 10;
        static private ManualResetEvent finish = new ManualResetEvent(false);
        static private void ThreadWork(object state)
        {
            int i = (int)state;
            for (int j = 0; j < 3; j++)
            {
                Thread.Sleep(1000);
                Console.WriteLine($"current thread {Thread.CurrentThread.ManagedThreadId} wait for {i * 10 + j + 1} seconds");
                if (Interlocked.Decrement(ref _num) == 0)
                    finish.Set();
            }
        }
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            #region 普通多线程
            Ticket ticket = new Ticket();
            Person[] person = new Person[10] {
                new Person("Nicholas",21,"0000000000"),
                new Person("Nate",38,"1111111111"),
                new Person("Vincent",21,"2222222222"),
                new Person("Nike",51,"3333333333"),
                new Person("Gary",21,"0000000000"),
                new Person("Charles",38,"1111111111"),
                new Person("Karl",21,"2222222222"),
                new Person("Katharine",51,"3333333333"),
                new Person("Lee",21,"0000000000"),
                new Person("Ann",38,"1111111111"),
            };
            ThreadMessage("MainThread start");
            Console.WriteLine();
            //定义一个线程组
            Thread[] t = new Thread[person.Length];
            //启动多个线程，每个线程执行相同的操作
            for (int i = 0; i < person.Length; i++)
            {
                t[i] = new Thread(new ParameterizedThreadStart(BuyTicket));
                t[i].Start(ticket);
            }
            for (int i = 0; i < 3; i++)
            {
                Console.WriteLine("Main thread do work!");
                Thread.Sleep(200);
            }
            #endregion

            #region 线程池
            Ticket poolticket = new Ticket();
            Person[] poolperson = new Person[10]{
                    new Person("Nicholas", 21, "000000000000000000"),
                    new Person("Nate", 38, "111111111111111111"),
                    new Person("Vincent", 21, "222222222222222222"),
                    new Person("Niki", 51, "333333333333333333"),
                    new Person("Gary", 28, "444444444444444444"),
                    new Person("Charles", 49, "555555555555555555"),
                    new Person("Karl ", 55, "666666666666666666"),
                    new Person("Katharine", 19, "777777777777777777"),
                    new Person("Lee", 25, "888888888888888888"),
                    new Person("Ann", 34, "99999999999999999"),
            };
            ThreadPool.SetMaxThreads(1000, 1000);
            ThreadPool.SetMinThreads(2, 2);

            ThreadMessage("MainThread start");
            Console.WriteLine();
            foreach (Person p in poolperson)
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(BuyTicket), ticket);
            }
            for (int i = 0; i < 3; i++)
            {
                Console.WriteLine("Main thread do work!");
                Thread.Sleep(200);
            }
            #endregion

            #region 通过事件或者其他的内核对象来实现同步机制，返回线程状态；并检测最后一个完成的线程，来提高效率
            for (int i = 0; i < _num; i++)
            {
                ThreadPool.QueueUserWorkItem(ThreadWork, i);
            }
            finish.WaitOne();
            finish.Close();
            #endregion
            Console.ReadKey();
        }
        static void BuyTicket(object state)
        {
            Ticket ticket = (Ticket)state;
            BuyTicket(ticket);
        }
        static string BuyTicket(Ticket ticket)
        {
            lock (ticket)
            {
                ThreadMessage("Async Thread start:");
                Console.WriteLine("Async thread do work!");
                string message = ticket.GetTicket();
                Console.WriteLine(message + "\n");
                return message;
            }
        }
        static void ThreadMessage(string data)
        {
            string message = string.Format("{0}\nCurrentThread is {1}", data, Thread.CurrentThread.ManagedThreadId);
            Console.WriteLine(message);
        }
    }
    class Ticket
    {
        private int count = 100;
        public int Count
        {
            get
            {
                return this.count;
            }
        }
        public string GetTicket()
        {
            this.count++;
            Thread.Sleep(50);
            this.count--;
            return "G" + this.count--;
        }
    }
    class Person
    {
        private string name, id;
        private int age;
        public string Name
        {
            get
            {
                return this.name;
            }
            set
            {
                if (value.Length > 0 && value.Length < 8)
                {
                    this.name = value;
                }
                else
                {
                    throw new IndexOutOfRangeException("Length of name is out of 0-8");
                }
            }
        }
        public int Age
        {
            get
            {
                return this.age;
            }
            set
            {
                if (value > 0)
                {
                    this.age = value;
                }
                else
                {
                    throw new IndexOutOfRangeException("Age must be more than 0.");
                }
            }
        }
        public string ID
        {
            get
            {
                return this.id;
            }
            set
            {
                if (value.Length == 18)
                    this.id = value;
                else
                    throw new IndexOutOfRangeException("Length of ID must be 16.");
            }
        }

        public Person(string nameOfPerson, int ageOfPerson, string idOfPerson)
        {
            this.name = nameOfPerson;
            this.age = ageOfPerson;
        }
    }
}
