﻿using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace ZeroLog
{
    public class Log
    {
        private readonly ConcurrentQueue<LogEvent> _queue = new ConcurrentQueue<LogEvent>();
        private readonly ObjectPool<LogEvent> _pool;
        private readonly IAppender[] _appenders;
        private Encoding _encoding;

        public Log()
        {
            _encoding = Encoding.Default;
            _pool = new ObjectPool<LogEvent>(() => new LogEvent(this), 100);
            Task.Run(() => WriteToAppenders());
        }

        private void WriteToAppenders()
        {
            var stringBuffer = new StringBuffer();
            byte[] destination = new byte[1024];
            while (true)
            {
                LogEvent logEvent;
                if (_queue.TryDequeue(out logEvent))
                {
                    logEvent.WriteToStringBuffer(stringBuffer);
                    var bytesWritten = stringBuffer.CopyTo(destination, 0, stringBuffer.Count, _encoding);
                    _pool.Free(logEvent);

                    // Write to appenders
                    foreach (var appender in _appenders)
                    {
                        var stream = appender.GetStream();
                        stream.Write(destination, 0, bytesWritten);
                    }
                }
            }
        }

        public LogEvent Info()
        {
            var logEvent = _pool.Allocate();
            logEvent.SetLevel(Level.Info);
            return logEvent;
        }

        public void Enqueue(LogEvent logEvent)
        {
            _queue.Enqueue(logEvent);
        }
    }
}