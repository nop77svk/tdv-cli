namespace NoP77svk.TibcoDV.CLI
{
    using System;

    internal class PrematureEndOfScriptFile
        : ScriptReaderException
    {
        public PrematureEndOfScriptFile(string file, int row, int column, string commandSeparator)
            : base(file, row, column, $"Unterminated command read, command separator {commandSeparator} missing")
        {
        }

        public PrematureEndOfScriptFile(string file, int row, int column, string commandSeparator, string commandThusFar)
            : base(file, row, column, $"Unterminated command read, command separator {commandSeparator} missing...\n{commandThusFar}")
        {
        }
    }
}
