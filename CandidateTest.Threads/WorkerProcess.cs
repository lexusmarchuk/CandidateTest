using System;
using System.Threading;

namespace CandidateTest.Threads
{
    delegate void RetryHandler(string message);

    public class WorkerProcess
    {
        private int _cnt = 0;
        private event RetryHandler OnRetry;

        public string ProcessName { get; }
        public int TimeOut { get; }

        private readonly DataLogger _dataLogger;
        private readonly CancellationToken _cancellationToken;

        public WorkerProcess(
            string processName, 
            int timeOut, 
            CancellationToken cancellationToken, 
            DataLogger dataLogger)
        {
            ProcessName = string.IsNullOrWhiteSpace(processName) ? "Noname Process" : processName;
            TimeOut = timeOut > 0 ? timeOut : 500;
            _cnt = 0;
            _dataLogger = dataLogger;
            _cancellationToken = cancellationToken;
        }

        public void Start()
        {

            OnRetry += Retry;

            while (!_cancellationToken.IsCancellationRequested)
            {
                _cnt++;

                try
                {
                    var message = DateTime.UtcNow.ToString() + " \t TimeOut : " + TimeOut.ToString() + " \t\t " + ProcessName + "(" + _cnt + ")" + Environment.NewLine;

                    _dataLogger.Write(ProcessName, message);
                }
                catch (Exception ex)
                {
                    OnRetry(ex.Message);
                }

                Thread.Sleep(TimeOut);
            }
        }

        private void Retry(string message)
        {
            Console.WriteLine(message);
            string error = message.Trim();
            _dataLogger.Write("Error", error);
        }
    }
}
