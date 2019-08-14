using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CandidateTest.Threads
{
    public class DataLogger : IDisposable
    {
        private const string DataFilePath = "..\\..\\Output\\data.txt";
        private const string StatisticFilePath = "..\\..\\Output\\Statistics.txt";
        private const int DelayMilliseconds = 500;
        private static readonly TimeSpan StatisticDelay = TimeSpan.FromMilliseconds(500);

        private readonly ConcurrentQueue<KeyValuePair<string, string>> _queue = new ConcurrentQueue<KeyValuePair<string, string>>();
        
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly UTF8Encoding _encoding = new UTF8Encoding(true);

        private readonly List<KeyValuePair<string, string>> _data = new List<KeyValuePair<string, string>>();

        private FileStream _dataStream;
        private DateTime _lastStatisticSaved = DateTime.MinValue;

        public DataLogger() {

            _cancellationTokenSource = new CancellationTokenSource();
            Task.Factory.StartNew(
                () => ProcessMessages(_cancellationTokenSource.Token),
                _cancellationTokenSource.Token,
                TaskCreationOptions.DenyChildAttach | TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        public void Write(string key, string content)
        {
            _queue.Enqueue(new KeyValuePair<string, string>(key, content));
        }

        public void Dispose()
        {
            if (_cancellationTokenSource.IsCancellationRequested)
            {
                return;
            }

            _cancellationTokenSource.Cancel();
            if (_dataStream == null)
            {
                return;
            }

            SafeExecute.Sync(() => {
                _dataStream.Flush();
                _dataStream.Close();
            }, false);
        }

        private void ProcessMessages(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (_queue.TryDequeue(out var item))
                {
                    _data.Add(item);
                    AppendData(item.Value);
                    continue;
                }

                var now = DateTime.Now;
                if (now - _lastStatisticSaved > StatisticDelay)
                {
                    SaveStatistics();
                    _lastStatisticSaved = now;
                }

                Thread.Sleep(DelayMilliseconds);
            }
        }

        private void AppendData(string content)
        {
            if (_dataStream == null)
            {
                SafeExecute.Sync(() => _dataStream = new FileStream(DataFilePath, FileMode.Append));
            }

            try
            {
                var bytes = _encoding.GetBytes(content);
                _dataStream.Write(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                SafeExecute.Sync(() => _dataStream.Close());

                SafeExecute.Sync(() => _dataStream = new FileStream(DataFilePath, FileMode.Append));
            }
        }

        private void SaveStatistics()
        {
            SafeExecute.Sync(() => {
                using (var fs = File.Open(StatisticFilePath, FileMode.Open))
                {
                    var stat = _data.GroupBy(x => x.Key).OrderBy(x => x.Key);
                    var statistics = String.Format("{0,10} | {1,10}", "Process", "Count") + Environment.NewLine;
                    statistics += new String('-', 24) + Environment.NewLine;
                    foreach (var process in stat)
                    {
                        statistics += String.Format("{0,10} | {1,10}", process.Key, process.Count()) + Environment.NewLine;
                    }

                    var toSave = new UTF8Encoding(true).GetBytes(statistics);
                    fs.Write(toSave, 0, toSave.Length);
                }
            });
        }
    }
}
