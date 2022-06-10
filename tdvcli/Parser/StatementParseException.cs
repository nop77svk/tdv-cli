namespace NoP77svk.TibcoDV.CLI.Parser
{
    using System;

    public class StatementParseException
        : Exception
    {
        public string? FileName { get; private set; }
        public int FileLine { get; private set; }
        public string? FailedCommand { get; private set; }

        public StatementParseException(string? fileName, int fileRowNumber, string? command, string? message)
            : base(message) // 2do!
        {
            FileName = fileName;
            FileLine = FileLine;
            FailedCommand = command;
        }

        public StatementParseException(string? fileName, int fileRowNumber, string? command, Exception? innerException)
            : base(null, innerException)
        {
            FileName = fileName;
            FileLine = FileLine;
            FailedCommand = command;
        }

        public StatementParseException(string? fileName, int fileRowNumber, string? command, string? message, Exception? innerException)
            : base(message, innerException)
        {
            FileName = fileName;
            FileLine = FileLine;
            FailedCommand = command;
        }
    }
}
