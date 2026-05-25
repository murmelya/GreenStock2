using System;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;

namespace GreenStock.Logging
{
    public class UILoggerProvider : ILoggerProvider
    {
        private readonly RichTextBox _textBox;

        public UILoggerProvider(RichTextBox textBox)
        {
            _textBox = textBox;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new UILogger(categoryName, _textBox);
        }

        public void Dispose()
        {
        }
    }
}
