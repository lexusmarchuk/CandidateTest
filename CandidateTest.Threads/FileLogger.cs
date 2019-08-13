using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CandidateTest.Threads
{
    public class FileLogger : IDisposable
    {
        private const int DelayMilliseconds = 500;

        private readonly ConcurrentQueue<string> _queue = new ConcurrentQueue<string>();
        private readonly Lazy<FileStream> _fileStream;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly UTF8Encoding _encoding = new UTF8Encoding(true);

        public FileLogger(string filePath, FileMode fileMode = FileMode.Append) {
            _fileStream = new Lazy<FileStream>(() => File.Open(filePath, fileMode));
            _cancellationTokenSource = new CancellationTokenSource();
            Task.Factory.StartNew(
                () => ProcessMessages(_cancellationTokenSource.Token), 
                default, 
                TaskCreationOptions.DenyChildAttach | TaskCreationOptions.LongRunning, 
                TaskScheduler.Default);
        }

        public void Write(string content)
        {
            _queue.Enqueue(content);
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            if (!_fileStream.IsValueCreated)
            {
                return;
            }

            var stream = _fileStream.Value;

            try
            {
                stream.Flush();
                stream.Close();
            }
            catch { }
        }

        private void ProcessMessages(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!_queue.TryDequeue(out var message))
                {
                    Task.Delay(DelayMilliseconds);
                    continue;
                }

                var bytes = _encoding.GetBytes(message);
                _fileStream.Value.Write(bytes, 0, bytes.Length);
            }
        }
    }
}
