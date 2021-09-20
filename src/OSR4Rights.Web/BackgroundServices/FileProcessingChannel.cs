using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Serilog;

namespace OSR4Rights.Web.BackgroundServices
{
    public class FileProcessingChannel
    {
        private const int MaxMessagesInChannel = 100;

        private readonly Channel<string> _channel;

        public FileProcessingChannel()
        {
            // BoundedChannel limits how big it can get eg 100 messages max
            var options = new BoundedChannelOptions(MaxMessagesInChannel)
            {
                SingleWriter = false, // multiple users can upload to the channel at the same time
                SingleReader = true
            };

            // accept strings in this Channel
            // which will be temp filename the required processing
            _channel = Channel.CreateBounded<string>(options);
        }

        public async Task<bool> AddFileAsync(string fileName, CancellationToken ct = default)
        {
            while (await _channel.Writer.WaitToWriteAsync(ct) && !ct.IsCancellationRequested)
            {
                if (_channel.Writer.TryWrite(fileName))
                {
                    //Log.ChannelMessageWritten(_logger, fileName);
                    Log.Information($"Added {fileName} to FileProcessingChannel");

                    return true;
                }
            }

            return false;
        }

        public int CountOfFileProcessingChannel()
        {
            var foo = _channel.Reader.Count;
            return foo;
        }

        public IAsyncEnumerable<string> ReadAllAsync(CancellationToken ct = default) =>
            _channel.Reader.ReadAllAsync(ct);

        public bool TryCompleteWriter(Exception ex = null) => _channel.Writer.TryComplete(ex);

        internal static class EventIds
        {
            public static readonly EventId ChannelMessageWritten = new EventId(100, "ChannelMessageWritten");
        }

        //private static class Log
        //{
        //    private static readonly Action<ILogger, string, Exception> _channelMessageWritten = LoggerMessage.Define<string>(
        //        LogLevel.Information,
        //        EventIds.ChannelMessageWritten,
        //        "Filename {FileName} was written to the channel.");

        //    public static void ChannelMessageWritten(ILogger<> logger, string fileName)
        //    {
        //        _channelMessageWritten(logger, fileName, null);
        //    }
        //}
    }
}
