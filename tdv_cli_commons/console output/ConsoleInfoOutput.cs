namespace NoP77svk.TibcoDV.CLI.Commons
{
    using System;
    using System.IO;
    using log4net;

    public class ConsoleInfoOutput : IInfoOutput
    {
        public ConsoleColor InfoForegroundColor { get; init; } = ConsoleColor.Gray;
        public ConsoleColor InfoBackgroundColor { get; init; } = ConsoleColor.Black;

        public ConsoleColor ErrorForegroundColor { get; init; } = ConsoleColor.Red;
        public ConsoleColor ErrorBackgroundColor { get; init; } = ConsoleColor.Black;

        public TextWriter OutputWriter { get; }

        public ConsoleInfoOutput(bool writeToStdErr = true, ILog? log = null)
        {
            if (writeToStdErr)
                OutputWriter = Console.Error;
            else
                OutputWriter = Console.Out;
        }

        private bool _thereWasEoln = true;

        public void Error(Exception e)
        {
            Error(e.Message);
            _thereWasEoln = true;
        }

        public void Error(string message, Exception? e = null)
        {
            Console.ForegroundColor = ErrorForegroundColor;
            Console.BackgroundColor = ErrorBackgroundColor;

            OptionalDisplayTimestamp();
            OutputWriter.WriteLine(message);

            if (e != null)
            {
                OptionalDisplayTimestamp();
                OutputWriter.WriteLine(e.Message);
            }

            Console.ForegroundColor = InfoForegroundColor;
            Console.BackgroundColor = InfoBackgroundColor;

            _thereWasEoln = true;
        }

        public void Info(string message)
        {
            Console.ForegroundColor = InfoForegroundColor;
            Console.BackgroundColor = InfoBackgroundColor;

            OptionalDisplayTimestamp();
            OutputWriter.WriteLine(message);

            _thereWasEoln = true;
        }

        public void InfoNoEoln(string message)
        {
            Console.ForegroundColor = InfoForegroundColor;
            Console.BackgroundColor = InfoBackgroundColor;

            OptionalDisplayTimestamp();
            OutputWriter.Write(message);

            _thereWasEoln = false;
        }

        private string FormattedTimestamp()
        {
            return DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss.fff]");
        }

        private void OptionalDisplayTimestamp()
        {
            if (_thereWasEoln)
            {
                OutputWriter.Write(FormattedTimestamp());
                OutputWriter.Write(' ');
            }
        }

        private void OptionalNewLine()
        {
            if (!_thereWasEoln)
                OutputWriter.WriteLine();
        }
    }
}
