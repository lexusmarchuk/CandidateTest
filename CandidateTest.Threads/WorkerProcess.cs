using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace CandidateTest.Threads
{
    public class WorkerProcess
    {
        private static ReaderWriterLockSlim _readWriteLock = new ReaderWriterLockSlim();
        private bool isCompleted;
        private int _cnt = 0;
        private static string _statistics;
        private CancellationTokenSource _cts;
        private delegate void RetryHandler(string message);
        private event RetryHandler OnRetry;
        private byte[] _passedData;
        //private static KeyValuePair<string, string> _data { get; set; }

        private string separator = Path.DirectorySeparatorChar.ToString();
        private static object _lock1 = new object();
        private static object _lock2 = new object();

        public ConcurrentDictionary<string, string> ConcurentData { get; set; } = new ConcurrentDictionary<string, string>();
        
        public string ProcessName { get; }
        public int TimeOut { get; }

        public WorkerProcess(string processName, int timeOut, CancellationTokenSource cts)
        {
            ProcessName = string.IsNullOrWhiteSpace(processName) ? "Noname Process" : processName;
            TimeOut = timeOut > 0 ? timeOut : 500;
            _cts = cts;
            _statistics = string.Empty;
            _cnt = 0;
        }

        public void Start()
        {
            var mainThread = new Thread(() =>
            {
                while (!isCompleted)
                {
                    _cnt++;
                    _cts.Token.Register(() =>
                    {
                        isCompleted = true;
                    });
                    lock (_lock1)
                    {
                        try
                        {
                            using (var fs = File.Open($"..{separator}..{separator}Output{separator}data.txt",
                                FileMode.Append))
                            {

                                while (ConcurentData.TryAdd(ProcessName,
                                    DateTime.UtcNow.ToString() + " \t TimeOut : " + TimeOut.ToString() + " \t\t " +
                                    ProcessName + "(" + _cnt + ")" + Environment.NewLine)) { }
                                _passedData =
                                    new UTF8Encoding(true).GetBytes(ConcurentData.LastOrDefault(x => x.Key == ProcessName).Value);
                                byte[] bytes = _passedData;
                                fs.Write(bytes, 0, bytes.Length);
                            }

                        }
                        catch (Exception ex)
                        {
                            OnRetry += Retry;
                            OnRetry(ex.Message);
                        }

                        Thread.Sleep(TimeOut);
                    }
                }
            });

            mainThread.Start();

            var statisticsThread = new Thread(() =>
            {
                SaveStatistics();
            });

            statisticsThread.Start();
        }

        private void Retry(string message)
        {
            Console.WriteLine(message);
            string error = message.Trim();
            while (ConcurentData.TryAdd("Error", error))
            { }
        }

        public void SaveStatistics()
        {
            lock (_lock2)
            {
                try
                {
                    using (var fs = File.Open($"..{separator}..{separator}Output{separator}Statistics.txt",
                        FileMode.Open))
                    {
                        var stat = ConcurentData.GroupBy(x => x.Key).OrderBy(x => x.Key);
                        _statistics = String.Format("{0,10} | {1,10}", "Process", "Count") + Environment.NewLine;
                        _statistics += new String('-', 24) + Environment.NewLine;
                        foreach (var process in stat)
                        {
                            _statistics += String.Format("{0,10} | {1,10}", process.Key, process.Count()) + Environment.NewLine;
                        }

                        var toSave = new UTF8Encoding(true).GetBytes(_statistics);
                        fs.Write(toSave, 0, toSave.Length);
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }
}
