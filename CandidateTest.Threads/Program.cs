using System;
using System.Diagnostics;
using System.Threading;
using System.Timers;

namespace CandidateTest.Threads
{
    class Program
    {
        static CancellationTokenSource cts = new CancellationTokenSource();
        static DateTime timeToBeCompleted;
        static bool completedByTime;
        static System.Timers.Timer aTimer;
        static void Main(string[] args)
        {
            aTimer = new System.Timers.Timer();
            aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            aTimer.Interval = 100;
            aTimer.Enabled = true;

            timeToBeCompleted = DateTime.Now.AddMinutes(1);
            Console.WriteLine("Should be automatically stopped at " + timeToBeCompleted.ToShortTimeString());
            Process currentProcess = Process.GetCurrentProcess();

            Console.WriteLine("Press ESCAPE to stop the process.");
            Console.WriteLine(string.Format("RAM used: {0} MB", currentProcess.WorkingSet64 / 1024 / 1024));

            // if that cycle present some parallel initializer of threads
            // and we cant adjust number of parallel cals started so 
            // we can set max number of threads in that process through ThreadPool.SetMaxThreads(int, int);
            // then run WorkrProcess start method as ThreadPool.QueueUserWorkItem(p.Start) 
            // but inside start method we must refuse to start a new thread 

            ThreadPool.SetMaxThreads(5, 5);
            for (int i = 1; i <= 200; i++)
            {
                WorkerProcess p = new WorkerProcess("P#" + i.ToString(), 200 + (200 / i * 2), cts);
                p.Start();
            }

            while (Console.ReadKey(true).Key == ConsoleKey.Escape && !completedByTime)
            {
                Process currentProcessExit = Process.GetCurrentProcess();
                Console.WriteLine("----------------------Terminating Process ESCAPE...");
                Console.WriteLine(string.Format("\r\nRAM used: {0} MB", currentProcessExit.WorkingSet64 / 1024 / 1024));

                cts.Cancel();
            }
            Console.WriteLine("Press ANY KEY");
            Console.ReadKey();
        }

        private static void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            if (timeToBeCompleted <= DateTime.Now)
            {
                Process currentProcessExit = Process.GetCurrentProcess();
                Console.WriteLine("----------------------Terminating Process by time...");
                Console.WriteLine(string.Format("\r\nRAM used: {0} MB", currentProcessExit.WorkingSet64 / 1024 / 1024));
                cts.Cancel();
                aTimer.Enabled = false;
                completedByTime = true;
            }
        }
    }
}
