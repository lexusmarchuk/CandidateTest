using System.Collections.Concurrent;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CandidateTest.Threads
{
    public class WorkerProcess
    {
        private int _cnt = 0;
        private delegate void RetryHandler(string message);
        private event RetryHandler OnRetry;
        private static ConcurrentDictionary<string, List<string>> _data = new ConcurrentDictionary<string, List<string>>();

        public string ProcessName { get; }
        public int TimeOut { get; }

        private readonly FileLogger _dataLogger;
        private readonly FileLogger _statisticLogger;
        private readonly CancellationToken _cancellationToken;

        public WorkerProcess(string processName, int timeOut, CancellationToken cancellationToken, FileLogger dataLogger, FileLogger statisticLogger)
        {
            ProcessName = string.IsNullOrWhiteSpace(processName) ? "Noname Process" : processName;
            TimeOut = timeOut > 0 ? timeOut : 500;
            _cnt = 0;
            _dataLogger = dataLogger;
            _statisticLogger = statisticLogger;
            _cancellationToken = cancellationToken;
        }

        public void Start()
        {

            OnRetry += Retry;

            var mainThread = new Thread(() =>
            {
                while (!_cancellationToken.IsCancellationRequested)
                {
                    _cnt++;
                    
                    try
                    {
                        var message = new StringBuilder()
                            .Append(DateTime.UtcNow.ToString())
                            .Append(" \t TimeOut : ")
                            .Append(TimeOut.ToString())
                            .Append(" \t\t ")
                            .Append(ProcessName)
                            .Append("(").Append(_cnt).Append(")")
                            .Append(Environment.NewLine)
                            .ToString();

                        var list = _data.GetOrAdd(ProcessName, (_) => new List<string>());
                        list.Add(message);

                        _dataLogger.Write(message);
                    }
                    catch (Exception ex)
                    {
                        OnRetry(ex.Message);
                    }

                    Thread.Sleep(TimeOut);

                    Task.Factory.StartNew(SaveStatistics);
                }
            });

            mainThread.Start();
        }

        private void Retry(string message)
        {
            Console.WriteLine(message);
            string error = message.Trim();
            var list = _data.GetOrAdd("Error", (_) => new List<string>());
            list.Add(message);
        }

        public void SaveStatistics()
        {
            var statistics = new StringBuilder();
            statistics.AppendFormat("{0,10} | {1,10}", "Process", "Count")
                .Append(Environment.NewLine);
            statistics.Append(new String('-', 24))
                .Append(Environment.NewLine);
            foreach (var processName in _data.Keys)
            {
                var list = _data[processName];
                statistics.AppendFormat("{0,10} | {1,10}", processName, list.Count)
                    .Append(Environment.NewLine);
            }

            _statisticLogger.Write(statistics.ToString());
        }
    }
}
