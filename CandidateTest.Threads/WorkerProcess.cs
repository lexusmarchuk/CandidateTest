using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace CandidateTest.Threads
{
    public class WorkerProcess
    {
        private static ReaderWriterLockSlim _readWriteLock = new ReaderWriterLockSlim();
        private static ReaderWriterLockSlim _statisticWriteLock = new ReaderWriterLockSlim();
        //private bool _isCompleted;
        private int _cnt = 0;
        //private static string _statistics;
        private CancellationTokenSource _cts;
        //private delegate void RetryHandler(string message);
        //private event RetryHandler OnRetry;
        //private byte[] _passedData;

        // If we store data produced by those threads it will get bigger and bigger
        // Instead of store Data to get statistics we can store the statistics themself
        //private static List<KeyValuePair<string, string>> _data { get; set; }
        //public static List<KeyValuePair<string, string>> Data
        //{
        //    get
        //    {
        //        if (_data == null)
        //        {
        //            _data = new List<KeyValuePair<string, string>>();
        //        }
        //        return _data;
        //    }
        //    set
        //    {
        //        _data = value;
        //    }
        //}
        private static Dictionary<string, int> statistics = new Dictionary<string, int>();

        public string ProcessName { get; }
        public int TimeOut { get; }

        public WorkerProcess(string processName, int timeOut, CancellationTokenSource cts)
        {
            ProcessName = string.IsNullOrWhiteSpace(processName) ? "Noname Process" : processName;
            TimeOut = timeOut > 0 ? timeOut : 500;
            _cts = cts;
            //_statistics = string.Empty;
            _cnt = 0;
        }
        
        public void Start()
        {
            //var mainThread = new Thread(() =>
            //{
                // This should be put out of the loop
                //_cts.Token.Register(() =>
                //{
                //    _isCompleted = true;
                //});

            var encoding = new UTF8Encoding(true);
            while (!_cts.Token.IsCancellationRequested) // Using IsCancellationRequested is shorter way
            {
                _cnt++;
                //_cts.Token.Register(() =>
                //{
                //    isCompleted = true;
                //});
                try
                {
                    // These operations should not be locked
                    string description = $"{DateTime.UtcNow}\t TimeOut : {TimeOut} \t\t {ProcessName}({_cnt}){Environment.NewLine}";
                    byte[] passedData = encoding.GetBytes(description); // passedData should be a local variable not a field of the class

                    _readWriteLock.EnterWriteLock();
                    //var fs = File.Open("..\\..\\Output\\data.txt", FileMode.Append);
                    //Data.Add(new KeyValuePair<string, string>(ProcessName, DateTime.UtcNow.ToString() + " \t TimeOut : " + TimeOut.ToString() + " \t\t " + ProcessName + "(" + _cnt + ")" + Environment.NewLine));
                    //_passedData = new UTF8Encoding(true).GetBytes(Data.LastOrDefault(x => x.Key == ProcessName).Value);
                    //byte[] bytes = _passedData;
                    //fs.Write(bytes, 0, bytes.Length);
                    using (var fs = File.Open("..\\..\\Output\\data.txt", FileMode.Append))
                    {
                        fs.Write(passedData, 0, passedData.Length);
                    }

                    statistics[ProcessName] = _cnt;
                }
                catch (Exception ex)
                {
                    // We don't need a delegate here, just call private method Retry
                    //OnRetry += Retry;
                    //OnRetry(ex.Message);
                    Retry(ex.Message);
                }
                finally
                {
                    _readWriteLock.ExitWriteLock();
                }

                Thread.Sleep(TimeOut);

                // Don't need a new thread to save statistics, just use the current one
                //var statisticsThread = new Thread(() =>
                //{
                //    SaveStatistics();
                //});
                //statisticsThread.Start();
                SaveStatistics();
            }
            //});

            //mainThread.Start();
        }

        private static void Retry(string message)
        {
            Console.WriteLine(message);
            //string error = message.Trim();
            //Data.Add(new KeyValuePair<string, string>("Error", error));
            int numberOfErrors;
            statistics.TryGetValue("Error", out numberOfErrors);
            statistics["Error"] = numberOfErrors + 1;
        }

        //public static void SaveStatistics()
        private static void SaveStatistics()
        {
            //try
            //{
            //    var fs = File.Open("..\\..\\Output\\Statistics.txt", FileMode.Open);
            //    var stat = Data.GroupBy(x => x.Key).OrderBy(x => x.Key);
            //    _statistics = String.Format("{0,10} | {1,10}", "Process", "Count") + Environment.NewLine;
            //    _statistics += new String('-', 24) + Environment.NewLine;
            //    foreach (var process in stat)
            //    {
            //        _statistics += String.Format("{0,10} | {1,10}", process.Key, process.Count()) + Environment.NewLine;
            //    }

            //    var toSave = new UTF8Encoding(true).GetBytes(_statistics);
            //    fs.Write(toSave, 0, toSave.Length);
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex.Message);
            //}

            // Build statistic content to output
            // We should use StringBuilder since there are many string concatnations
            StringBuilder statisticBuilder = new StringBuilder();
            statisticBuilder.AppendFormat("{0,10} | {1,10}", "Process", "Count").AppendLine().AppendLine(new string('-', 24));
            try
            {
                _readWriteLock.EnterReadLock();
                foreach (string processName in statistics.Keys)
                {
                    statisticBuilder.AppendFormat("{0,10} | {1,10}", processName, statistics[processName]).AppendLine();
                }
            }
            finally
            {
                _readWriteLock.ExitReadLock();
            }

            // Write the content to statistic file
            // We have to lock before writing to the file
            var toSave = new UTF8Encoding(true).GetBytes(statisticBuilder.ToString());
            try
            {
                _statisticWriteLock.EnterWriteLock();
                using (var fs = File.Open("..\\..\\Output\\Statistics.txt", FileMode.OpenOrCreate))
                {
                    fs.Write(toSave, 0, toSave.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                _statisticWriteLock.ExitWriteLock();
            }
        }
    }
}
