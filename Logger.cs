﻿using System;
using System.Collections.Generic;
using System.Text;

namespace RunData
{
    class Logger
    {
        public delegate void LogAppender(string msg);
        private LogAppender appender;

        public static Logger Instance;

        public static void Init(LogAppender appender)
        {
            Instance = new Logger();
            Instance.appender = appender;
        }

        public static void Info(string msg)
        {
            Instance.appender(msg);
        }
    }
}
