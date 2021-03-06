﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Newtonsoft.Json;

namespace Hangfire.HttpJob.Agent.RedisConsole
{
    internal class RedisConsole : IHangfireConsole, IHangfireConsoleInit
    {
        private double _lastTimeOffset;
        private const int ValueFieldLimit = 256;
        protected IHangfireStorage Storage;
        protected ConsoleInfo ConsoleInfo;
        private int _nextProgressBarId;
        public RedisConsole(IHangfireStorage storage)
        {
            if (storage == null) throw new ArgumentNullException(nameof(IHangfireStorage));
            Storage = storage;
            _lastTimeOffset = 0;
        }


        internal void WriteBar(ConsoleLine line)
        {
            if (line == null)
                throw new ArgumentNullException(nameof(line));

            lock (this)
            {
                string value;

                line.TimeOffset = Math.Round((DateTime.UtcNow - ConsoleInfo.StartTime).TotalSeconds, 3);

                if (_lastTimeOffset >= line.TimeOffset)
                {
                    // prevent duplicate lines collapsing
                    line.TimeOffset = _lastTimeOffset + 0.0001;
                }

                _lastTimeOffset = line.TimeOffset;

                if (line.Message.Length > ValueFieldLimit - 36)
                {
                    // pretty sure it won't fit
                    // (36 is an upper bound for JSON formatting, TimeOffset and TextColor)
                    value = null;
                }
                else
                {
                    // try to encode and see if it fits
                    value = JsonConvert.SerializeObject(line);

                    if (value.Length > ValueFieldLimit)
                    {
                        value = null;
                    }
                }

                if (value == null)
                {
                    var referenceKey = Guid.NewGuid().ToString("N");

                    Storage.SetRangeInHash(ConsoleInfo.HashKey, new[] { new KeyValuePair<string, string>(referenceKey, line.Message) });

                    line.Message = referenceKey;
                    line.IsReference = true;

                    value = JsonConvert.SerializeObject(line);
                }

                Storage.AddToSet(ConsoleInfo.SetKey, value, line.TimeOffset);
            }
        }

        public void WriteLine(string message,ConsoleFontColor fontColor = null)
        {
            if (this.ConsoleInfo == null || string.IsNullOrEmpty(message)) return;
            if (string.IsNullOrEmpty(this.ConsoleInfo.HashKey) || string.IsNullOrEmpty(this.ConsoleInfo.SetKey)) return;
            message = "【JobAgent】" + message;
            lock (this)
            {
                string value;

                ConsoleLine line = new ConsoleLine
                {
                    Message = message
                };

                if (fontColor != null)
                {
                    line.TextColor = fontColor.ToString();
                }

                line.TimeOffset = Math.Round((DateTime.UtcNow - ConsoleInfo.StartTime).TotalSeconds, 3);
                if (_lastTimeOffset >= line.TimeOffset)
                {
                    // prevent duplicate lines collapsing
                    line.TimeOffset = _lastTimeOffset + 0.0001;
                }

                _lastTimeOffset = line.TimeOffset;

                if (line.Message.Length > ValueFieldLimit - 36)
                {
                    // pretty sure it won't fit
                    // (36 is an upper bound for JSON formatting, TimeOffset and TextColor)
                    value = null;
                }
                else
                {
                    // try to encode and see if it fits
                    value = JsonConvert.SerializeObject(line);

                    if (value.Length > ValueFieldLimit)
                    {
                        value = null;
                    }
                }

                if (value == null)
                {
                    var referenceKey = Guid.NewGuid().ToString("N");

                    Storage.SetRangeInHash(ConsoleInfo.HashKey, new[] { new KeyValuePair<string, string>(referenceKey, line.Message) });

                    line.Message = referenceKey;
                    line.IsReference = true;

                    value = JsonConvert.SerializeObject(line);
                }

                Storage.AddToSet(ConsoleInfo.SetKey, value, line.TimeOffset);
            }
        }

        public IProgressBar WriteProgressBar(string name, double value=1, ConsoleFontColor color = null)
        {
            var progressBarId = Interlocked.Increment(ref _nextProgressBarId);

            var progressBar = new RedisProgressBar(this, progressBarId.ToString(CultureInfo.InvariantCulture), name, color);
            // set initial value
            progressBar.SetValue(value);

            return progressBar;
        }

        public void Init(ConsoleInfo consoleInfo)
        {
            if (consoleInfo == null) throw new ArgumentNullException(nameof(ConsoleInfo));
            ConsoleInfo = consoleInfo;
            _nextProgressBarId = consoleInfo.ProgressBarId;//初始化

            if (consoleInfo != null && consoleInfo.StartTime == DateTime.MinValue)
            {
                consoleInfo.StartTime = DateTime.UtcNow;
            }
        }
    }

    internal class ConsoleLine
    {
        /// <summary>
        /// Time offset since console timestamp in fractional seconds
        /// </summary>
        [JsonProperty("t", Required = Required.Always)]
        public double TimeOffset { get; set; }

        /// <summary>
        /// True if <see cref="Message"/> is a Hash reference.
        /// </summary>
        [JsonProperty("r", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsReference { get; set; }

        /// <summary>
        /// Message text, or message reference, or progress bar id
        /// </summary>
        [JsonProperty("s", Required = Required.Always)]
        public string Message { get; set; }

        /// <summary>
        /// Text color for this message
        /// </summary>
        [JsonProperty("c", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string TextColor { get; set; }

        /// <summary>
        /// Value update for a progress bar
        /// </summary>
        [JsonProperty("p", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public double? ProgressValue { get; set; }

        /// <summary>
        /// Optional name for a progress bar
        /// </summary>
        [JsonProperty("n", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string ProgressName { get; set; }
    }
}
