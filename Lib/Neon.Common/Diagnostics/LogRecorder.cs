﻿//-----------------------------------------------------------------------------
// FILE:	    LogRecorder.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2018 by neonFORGE, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Neon.Common;

namespace Neon.Diagnostics
{
    /// <summary>
    /// Simple class that can be used to capture log entries while also passing them
    /// through to a base <see cref="INeonLogger"/> implementation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Instantiate by passing a base <see cref="INeonLogger"/> implementation to the constructor.
    /// All of the logging properties and methods calls to this class will pass through
    /// to the base implementation and enabled log methods will also be collected by the
    /// instance.  Use <see cref="ToString"/> to return the captured text and <see cref="Clear"/>
    /// to clear it.
    /// </para>
    /// </remarks>
    public class LogRecorder : INeonLogger
    {
        private INeonLogger     log;
        private StringBuilder   capture;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="log">The inderlying <see cref="INeonLogger"/> implementation.</param>
        public LogRecorder(INeonLogger log)
        {
            Covenant.Requires<ArgumentNullException>(log != null);

            this.log     = log;
            this.capture = new StringBuilder();
        }

        /// <summary>
        /// Clears the captured text.
        /// </summary>
        public void Clear()
        {
            lock (capture)
            {
                capture.Clear();
            }
        }

        /// <summary>
        /// Returns the captured log text.
        /// </summary>
        /// <returns>The text.</returns>
        public override string ToString()
        {
            lock (capture)
            {
                return capture.ToString();
            }
        }

        /// <inheritdoc/>
        public bool IsDebugEnabled => log.IsDebugEnabled;

        /// <inheritdoc/>
        public bool IsSInfoEnabled => log.IsSInfoEnabled;

        /// <inheritdoc/>
        public bool IsInfoEnabled => log.IsInfoEnabled;

        /// <inheritdoc/>
        public bool IsWarnEnabled => log.IsWarnEnabled;

        /// <inheritdoc/>
        public bool IsErrorEnabled => log.IsErrorEnabled;

        /// <inheritdoc/>
        public bool IsSErrorEnabled => log.IsSErrorEnabled;

        /// <inheritdoc/>
        public bool IsCriticalEnabled => log.IsCriticalEnabled;

        /// <inheritdoc/>
        public bool IsEnabled(LogLevel logLevel)
        {
            return log.IsEnabled(logLevel);
        }

        /// <inheritdoc/>
        public void LogDebug(object message, string activityId = null)
        {
            log.LogDebug(message, activityId);
            capture.AppendLine($"[DEBUG] {message}");
        }

        /// <inheritdoc/>
        public void LogSInfo(object message, string activityId = null)
        {
            log.LogSInfo(message, activityId);
            capture.AppendLine($"[SINFO] {message}");
        }

        /// <inheritdoc/>
        public void LogInfo(object message, string activityId = null)
        {
            log.LogInfo(message, activityId);
            capture.AppendLine($"[INFO] {message}");
        }

        /// <inheritdoc/>
        public void LogWarn(object message, string activityId = null)
        {
            log.LogWarn(message, activityId);
            capture.AppendLine($"[WARN]: {message}");
        }

        /// <inheritdoc/>
        public void LogError(object message, string activityId = null)
        {
            log.LogError(message, activityId);
            capture.AppendLine($"[ERROR] {message}");
        }

        /// <inheritdoc/>
        public void LogSError(object message, string activityId = null)
        {
            log.LogSError(message, activityId);
            capture.AppendLine($"[SERROR] {message}");
        }

        /// <inheritdoc/>
        public void LogCritical(object message, string activityId = null)
        {
            log.LogCritical(message, activityId);
            capture.AppendLine($"[CRITICAL] {message}");
        }

        /// <inheritdoc/>
        public void LogDebug(object message, Exception e, string activityId = null)
        {
            log.LogDebug(message, e, activityId);
            capture.AppendLine($"[DEBUG] {message} {NeonHelper.ExceptionError(e)}");
        }

        /// <inheritdoc/>
        public void LogSInfo(object message, Exception e, string activityId = null)
        {
            log.LogSInfo(message, e, activityId);
            capture.AppendLine($"[SINFO] {message} {NeonHelper.ExceptionError(e)}");
        }

        /// <inheritdoc/>
        public void LogInfo(object message, Exception e, string activityId = null)
        {
            log.LogInfo(message, e, activityId);
            capture.AppendLine($"[INFO] {message} {NeonHelper.ExceptionError(e)}");
        }

        /// <inheritdoc/>
        public void LogWarn(object message, Exception e, string activityId = null)
        {
            log.LogWarn(message, e, activityId);
            capture.AppendLine($"[WARN] {message} {NeonHelper.ExceptionError(e)}");
        }

        /// <inheritdoc/>
        public void LogError(object message, Exception e, string activityId = null)
        {
            log.LogError(message, e, activityId);
            capture.AppendLine($"[ERROR] {message} {NeonHelper.ExceptionError(e)}");
        }

        /// <inheritdoc/>
        public void LogSError(object message, Exception e, string activityId = null)
        {
            log.LogSError(message, e, activityId);
            capture.AppendLine($"[SERROR] {message} {NeonHelper.ExceptionError(e)}");
        }

        /// <inheritdoc/>
        public void LogCritical(object message, Exception e, string activityId = null)
        {
            log.LogCritical(message, e, activityId);
            capture.AppendLine($"[CRITICAL] {message} {NeonHelper.ExceptionError(e)}");
        }

        /// <inheritdoc/>
        public void LogMetrics(LogLevel level, IEnumerable<string> txtFields, IEnumerable<double> numFields)
        {
            log.LogMetrics(level, txtFields, numFields);
            capture.AppendLine($"[{level.ToString().ToUpperInvariant()}] {NeonLogger.FormatMetrics(txtFields, numFields)}");
        }

        /// <inheritdoc/>
        public void LogMetrics(LogLevel level, params string[] txtFields)
        {
            log.LogMetrics(level, txtFields, null);
        }

        /// <inheritdoc/>
        public void LogMetrics(LogLevel level, params double[] numFields)
        {
            log.LogMetrics(level, null, numFields);
        }

        /// <summary>
        /// Logs a line of text directly to the log recorder without also logging it to
        /// to the underlying <see cref="INeonLogger"/> implementation.
        /// </summary>
        /// <param name="message">The optional message to be recorded.</param>
        public void Record(string message = null)
        {
            capture.AppendLine(message ?? string.Empty);
        }
    }
}
