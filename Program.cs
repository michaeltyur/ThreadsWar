using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace WorkEx
{
    class Program
    {
        public delegate void EventHandler();
        public static event EventHandler StartTasksProcess;
        public static event EventHandler StartThreadsProcess;
        public static event EventHandler PrintResultEvent;
        public static event EventHandler StartNewGameEvent;
        static void Main(string[] args)
        {
            Console.Title = "Welcome to Threads War!!! May the best win...";

            StartNewGame();
            while (true)
            {
                var key = Console.ReadKey().Key;
                if (key != ConsoleKey.Escape)
                {
                    StartNewGame();
                }

                else break;
            }

        }

        static int[] GetRandomNumberArray(int size)
        {
            int[] array = new int[size];
            Random rand = new Random();
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = rand.Next(1, 100);
            }
            return array;
        }

        static void PrintResult(TimeSpan oneThreadTime, TimeSpan tasksTime, TimeSpan multiThreadsTime)
        {
            Console.WriteLine();
            Console.WriteLine("-----------------------RESULTS------------------------------");
            //Single Thread
            Console.Write("Single thread time: ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(oneThreadTime);
            Console.ForegroundColor = ConsoleColor.White;

            //Tasks
            Console.Write("Multi  tasks time : ");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(tasksTime);
            Console.ForegroundColor = ConsoleColor.White;

            //Multi Thread
            Console.Write("Multi thread time : ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(multiThreadsTime);
            Console.ForegroundColor = ConsoleColor.White;

            Console.Write("WINNER : ");
            Console.ForegroundColor = ConsoleColor.DarkRed;
            if (oneThreadTime < tasksTime && oneThreadTime < multiThreadsTime)
            {
                //Winner
                Console.WriteLine("Single Thread");
            }
            else if (tasksTime < oneThreadTime && tasksTime < multiThreadsTime)
            {
                Console.WriteLine("Multi Tasks");
            }
            else if (multiThreadsTime < oneThreadTime && multiThreadsTime < tasksTime)
            {
                Console.WriteLine("Multi Threads");
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("------------------------------------------------------------");

            Console.WriteLine("Press some key for restart the application");
            Console.WriteLine("Or Escape button for exit from application");

        }

        static void StartNewGame()
        {
            StartTasksProcess=null;
            StartThreadsProcess=null;
            PrintResultEvent = null;
            StartNewGameEvent = null;

            var duration = 10;
            var numberOfTreads = 10;
            var input = "";

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("********************* Welcome to Threads War!!! May the best win **************************");
            Console.WriteLine();

            //Number of Interactions
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Enter number of interactions");
            Console.ForegroundColor = ConsoleColor.Yellow;
            input = Console.ReadLine();
            while (!int.TryParse(input, out duration))
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Please enter correct number");
                Console.ForegroundColor = ConsoleColor.Yellow;
                input = Console.ReadLine();
            }

            //Number of Threads
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Enter number of threads");
            Console.ForegroundColor = ConsoleColor.Yellow;
            input = Console.ReadLine();
            while (!int.TryParse(input, out numberOfTreads))
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Please enter correct number");
                Console.ForegroundColor = ConsoleColor.Yellow;
                input = Console.ReadLine();
            }
            Console.ForegroundColor = ConsoleColor.White;

            var dataArray = GetRandomNumberArray(duration);
            IProcess syncProcess = new SyncProcess(dataArray);
            IProcess tasksProcess = new AsyncTaskProcess(dataArray);
            IProcess threadsProcess = new MultyThreadProcess(dataArray, numberOfTreads);

            StartTasksProcess += new EventHandler(tasksProcess.Start);
            StartThreadsProcess += new EventHandler(threadsProcess.Start);
            PrintResultEvent += new EventHandler(() => PrintResult(syncProcess.ResultTime, tasksProcess.ResultTime, threadsProcess.ResultTime));
            StartNewGameEvent += new EventHandler(StartNewGame);
            syncProcess.AfterFinishEvent = StartTasksProcess;
            tasksProcess.AfterFinishEvent = StartThreadsProcess;
            threadsProcess.TimeResultEvent = PrintResultEvent;
            //threadsProcess.StartNewGameEvent = StartNewGameEvent;
            syncProcess.Start();
        }

        internal interface IProcess
        {
            EventHandler AfterFinishEvent { get; set; }
            EventHandler TimeResultEvent { get; set; }
            EventHandler StartNewGameEvent { get; set; }
            TimeSpan ResultTime { get; set; }
            void Start();
        }
        internal class BaseProcess : IProcess
        {
            private static readonly object _balanceLock = new object();
            protected static readonly object balanceLock2 = new object();
            public EventHandler AfterFinishEvent { get; set; }
            public EventHandler TimeResultEvent { get; set; }
            public TimeSpan ResultTime { get; set; }
            public EventHandler StartNewGameEvent { get; set; }

            protected static List<int> finishCounter;
            protected static Stopwatch stopwatch;
            protected int globalIndex;
            protected int[] _dataArray;
            protected int loopNumber;
            protected string finishMsg = "";

            public BaseProcess(int[] dataArray, string finishMsg)
            {
                _dataArray = dataArray;
                globalIndex = 0;
                stopwatch = new Stopwatch();
                finishCounter = new List<int>();
                loopNumber = dataArray.Length;
                this.finishMsg = finishMsg;
            }

            protected int GetSafetyData()
            {
                lock (_balanceLock)
                {
                    if (_dataArray.Length > 0)
                    {
                        var data = _dataArray[globalIndex];
                        globalIndex++;
                        return globalIndex;
                    }
                    else return 0;
                }
            }

            protected virtual void Worker(int currentInd)
            {
                Thread.Sleep(1000);

                Console.Write("worker:" + currentInd + " ");

                lock (balanceLock2)
                {
                    finishCounter.Add(globalIndex);
                    if (finishCounter.Count >= _dataArray.Length)
                    {
                        Console.WriteLine();
                        Console.WriteLine(finishMsg);
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Time elapsed: {stopwatch.Elapsed}");
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine("*********************************************************");
                        ResultTime = stopwatch.Elapsed;
                        stopwatch.Reset();
                        finishCounter.Clear();
                        //globalIndex = 0;
                        if (AfterFinishEvent != null)
                        {
                            AfterFinishEvent.Invoke();
                        }
                        if (TimeResultEvent != null)
                        {
                            TimeResultEvent.Invoke();
                        }

                        //if (StartNewGameEvent != null)
                        //{
                        //    StartNewGameEvent.Invoke();
                        //}

                    }
                }

            }

            public virtual void Start()
            {

            }
        }
        public class SyncProcess : BaseProcess
        {

            public SyncProcess(int[] dataArray) : base(dataArray, "Sync Work is finished")
            {

            }
            public override void Start()
            {
                Console.WriteLine();
                Console.Write("********************");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($" Sync Work starts ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("********************");

                stopwatch.Start();
                for (int i = 0; i < loopNumber; i++)
                {
                    var index = i;
                    GetSafetyData();
                    Worker(index);
                }
            }
            protected override void Worker(int currentInd)
            {
                base.Worker(currentInd);
            }

        }
        public class AsyncTaskProcess : BaseProcess
        {
            public AsyncTaskProcess(int[] dataArray) : base(dataArray, "Tasks Work is finished")
            {

            }

            public override void Start()
            {
                Console.WriteLine();
                Console.Write("********************");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($" Tasks Work starts ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("********************");

                stopwatch.Start();
                List<Task> tasks = new List<Task>();
                for (int i = 0; i < loopNumber; i++)
                {
                    var index = i;
                    Task task = Task.Run(() => Worker(index));
                    tasks.Add(task);
                }
            }

            protected override void Worker(int currentInd)
            {
                base.Worker(currentInd);
            }
        }
        public class MultyThreadProcess : BaseProcess
        {
            private int _numberThreads;
            public MultyThreadProcess(int[] dataArray, int numberThreads) : base(dataArray, "Thread Work is finished")
            {
                _numberThreads = numberThreads;
            }

            public override void Start()
            {
                globalIndex = 0;

                Console.WriteLine();
                Console.Write("********************");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($" Threads({_numberThreads}) Work starts ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("********************");

                stopwatch.Start();
                for (int i = 0; i < _numberThreads; i++)
                {
                    Thread thread = new Thread(() =>
                    {
                        while (globalIndex < loopNumber)
                        {
                            var index = 0;
                            index = GetSafetyData();
                            if (index > 0)
                            {
                                Worker(index);
                            }
                        }
                    });
                    thread.Start();
                }
            }

            protected override void Worker(int currentInd)
            {
                base.Worker(currentInd);
            }


        }
    }
}
