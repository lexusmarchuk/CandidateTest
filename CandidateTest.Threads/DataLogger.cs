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
        private readonly ConcurrentQueue<string> _dataFileQueue = new ConcurrentQueue<string>();
        
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly UTF8Encoding _encoding = new UTF8Encoding(true);

        private readonly ConcurrentDictionary<string, int> _statisticData = new ConcurrentDictionary<string, int>();

        private TextWriter _dataStream;
        private DateTime _lastStatisticSaved = DateTime.MinValue;

        public DataLogger() {

            _cancellationTokenSource = new CancellationTokenSource();

            Task.Factory.StartNew(
                () => ProcessMessages(_cancellationTokenSource.Token),
                _cancellationTokenSource.Token,
                TaskCreationOptions.DenyChildAttach | TaskCreationOptions.LongRunning,
                TaskScheduler.Default);

            Task.Factory.StartNew(
                () => DataProcess(_cancellationTokenSource.Token),
                _cancellationTokenSource.Token,
                TaskCreationOptions.DenyChildAttach | TaskCreationOptions.LongRunning,
                TaskScheduler.Default);

            Task.Factory.StartNew(
                () => StatisticProcess(_cancellationTokenSource.Token),
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
                if (!_queue.TryDequeue(out var item))
                {
                    Thread.Sleep(DelayMilliseconds);
                    continue;
                }

                _dataFileQueue.Enqueue(item.Value);

                var count = _statisticData.GetOrAdd(item.Key, (_) => 0);
                count++;
                _statisticData[item.Key] = count;
            }
        }

        private void DataProcess(CancellationToken cancellationToken)
        {
            var writeCount = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!_dataFileQueue.TryDequeue(out var content))
                {
                    Thread.Sleep(DelayMilliseconds);
                    continue;
                }

                if (_dataStream == null)
                {
                    SafeExecute.Sync(() => _dataStream = new StreamWriter(DataFilePath, true, Encoding.UTF8, 65536));
                }

                writeCount++;
                if (writeCount > 1000)
                {
                    writeCount = 0;
                    SafeExecute.Sync(() => _dataStream.Flush());
                    SafeExecute.Sync(() => _dataStream.Close());
                    _dataStream = null;
                    GC.Collect();

                    SafeExecute.Sync(() => _dataStream = new StreamWriter(DataFilePath, true, Encoding.UTF8, 65536));
                }

                try
                {
                    _dataStream.Write(content);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);

                    SafeExecute.Sync(() => _dataStream.Close());

                    SafeExecute.Sync(() => _dataStream = new StreamWriter(DataFilePath, true, Encoding.UTF8, 65536));
                }
            }
        }

        private void StatisticProcess(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                if (now - _lastStatisticSaved > StatisticDelay)
                {
                    SaveStatistics();
                    _lastStatisticSaved = now;
                }

                Thread.Sleep(DelayMilliseconds);
            }
        }

        private void SaveStatistics()
        {
            SafeExecute.Sync(() => {
                var stat = _statisticData.OrderBy(x => x.Key);
                var statistics = String.Format("{0,10} | {1,10}", "Process", "Count") + Environment.NewLine;
                statistics += new String('-', 24) + Environment.NewLine;
                foreach (var process in stat)
                {
                    statistics += String.Format("{0,10} | {1,10}", process.Key, process.Value) + Environment.NewLine;
                }

                using (var statisticStream = new StreamWriter(StatisticFilePath, true, Encoding.UTF8, 65536))
                {
                    statisticStream.Write(statistics);
                    statisticStream.Flush();
                }
            });
        }
    }
}
