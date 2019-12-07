namespace NoP77svk.TibcoDV.CLI
{
    using System;

    internal class ScriptReaderException
        : Exception
    {
        public ScriptReaderException(string file, int row, int column)
            : base($"Error in script file \"{file}\", line {row}, column {column}")
        {
        }

        public ScriptReaderException(string file, int row, int column, string message)
            : base($"Error in script file \"{file}\", line {row}, column {column}: {message}")
        {
        }
    }
}
