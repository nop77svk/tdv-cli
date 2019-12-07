namespace NoP77svk.TibcoDV.CLI
{
    using System;

    public class StatementParseException
        : Exception
    {
        public string? FileName { get; private set; }
        public int FileRowNumber { get; private set; }
        public string? FailedCommand { get; private set; }

        public StatementParseException(string? fileName, int fileRowNumber, string? command, string? message)
            : base(message) // 2do!
        {
            FileName = fileName;
            FileRowNumber = FileRowNumber;
            FailedCommand = command;
        }

        public StatementParseException(string? fileName, int fileRowNumber, string? command, Exception? innerException)
            : base(null, innerException)
        {
            FileName = fileName;
            FileRowNumber = FileRowNumber;
            FailedCommand = command;
        }

        public StatementParseException(string? fileName, int fileRowNumber, string? command, string? message, Exception? innerException)
            : base(message, innerException)
        {
            FileName = fileName;
            FileRowNumber = FileRowNumber;
            FailedCommand = command;
        }
    }
}
