using System;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;

namespace GreenStock.Logging
{
    public class UILogger : ILogger
    {
        private readonly RichTextBox _textBox;
        private readonly string _categoryName;

        public UILogger(string categoryName, RichTextBox textBox)
        {
            _categoryName = categoryName;
            _textBox = textBox;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            string message = formatter(state, exception);
            string time = DateTime.Now.ToString("HH:mm:ss");
            string logLine = $"[{time}] [{logLevel}] {message}";

            _textBox.Invoke(new Action(() =>
            {
                _textBox.AppendText(logLine + Environment.NewLine);
            }));

            try
            {
                System.IO.File.AppendAllText("logs.txt", logLine + Environment.NewLine);
            }
            catch { }
        }
    }
}