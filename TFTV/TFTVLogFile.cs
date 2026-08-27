using System;
using System.IO;
using System.Text;

namespace TFTV
{
    /// <summary>
    /// The single writer behind <see cref="TFTVLogger"/> and PRMBetterClasses.PRMLogger, which both
    /// log to the same file.
    ///
    /// Both loggers used to do <c>new StreamWriter(path, append: true)</c> per line, i.e. a CreateFile
    /// plus seek-to-end for every message. There are thousands of call sites and hundreds of them sit
    /// inside loops, so that open/close dominated the cost of logging.
    ///
    /// The handle is now opened once and kept open with AutoFlush, which hands every line to the OS
    /// immediately - the same crash-durability the old open/write/close had, without the syscalls.
    /// </summary>
    internal static class TFTVLogFile
    {
        internal const string Separator = "----------------------------------------------------------------------------------------------------";

        private static readonly object _lock = new object();
        private static string _path;
        private static StreamWriter _writer;

        internal static void SetPath(string path)
        {
            lock (_lock)
            {
                if (string.Equals(_path, path, StringComparison.Ordinal))
                {
                    return;
                }

                Close();
                _path = path;
            }
        }

        /// <summary>
        /// Empties the log and reopens it. Kept so the loggers' Cleanup() behaviour is unchanged.
        /// </summary>
        internal static void Truncate()
        {
            lock (_lock)
            {
                Close();
                Open(append: false);
            }
        }

        /// <summary>
        /// Writes "<paramref name="prefix"/><paramref name="line"/>" followed by a newline.
        /// <paramref name="prefix"/> may be null.
        /// </summary>
        internal static void WriteLine(string prefix, string line)
        {
            lock (_lock)
            {
                StreamWriter writer = _writer ?? Open(append: true);
                if (writer == null)
                {
                    return;
                }

                try
                {
                    if (prefix != null)
                    {
                        writer.Write(prefix);
                    }

                    writer.WriteLine(line);
                }
                catch (Exception)
                {
                    // Logging must never take the game down. Drop the handle so the next call retries.
                    Close();
                }
            }
        }

        private static StreamWriter Open(bool append)
        {
            if (string.IsNullOrEmpty(_path))
            {
                return null;
            }

            try
            {
                // FileShare.ReadWrite so players can still open or copy the log while the game runs.
                FileStream stream = new FileStream(
                    _path,
                    append ? FileMode.Append : FileMode.Create,
                    FileAccess.Write,
                    FileShare.ReadWrite,
                    bufferSize: 4096);

                _writer = new StreamWriter(stream, new UTF8Encoding(false))
                {
                    AutoFlush = true
                };
            }
            catch (Exception)
            {
                _writer = null;
            }

            return _writer;
        }

        private static void Close()
        {
            if (_writer == null)
            {
                return;
            }

            try
            {
                _writer.Dispose();
            }
            catch (Exception)
            {
                // Nothing useful to do - the writer is being discarded either way.
            }

            _writer = null;
        }
    }

    /// <summary>
    /// Builds the "[modName @ timestamp] " prefix, rebuilding it only when the second changes.
    /// The default DateTime.ToString() only has second resolution, so a line written within the
    /// same second gets a byte-identical prefix to the one the old per-line formatting produced.
    /// </summary>
    internal sealed class TFTVLogPrefix
    {
        private string _modName;
        private long _second = -1;
        private string _cached;

        internal void SetModName(string modName)
        {
            _modName = modName;
            _second = -1;
            _cached = null;
        }

        internal string Get()
        {
            DateTime now = DateTime.Now;
            long second = now.Ticks / TimeSpan.TicksPerSecond;

            if (second != _second || _cached == null)
            {
                _second = second;
                _cached = $"[{_modName} @ {now}] ";
            }

            return _cached;
        }
    }
}
