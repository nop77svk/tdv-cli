namespace NoP77svk.TibcoDV.CLI
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    internal class ScriptFileParser
    {
        private Func<string> getCommandDelimiter;

        public string CommandDelimiter
        {
            get
            {
                string result = getCommandDelimiter();
                if (result != ";")
                    throw new ArgumentOutOfRangeException(nameof(result), result, "Invalid command delimiter");
                return result;
            }
        }

        public ScriptFileParser(Func<string> commandDelimiterGetter)
        {
            getCommandDelimiter = commandDelimiterGetter;
        }

        internal IEnumerable<ScriptFileParserOutPOCO> SplitScriptsToStatements(IEnumerable<string?>? scriptFiles)
        {
            if (scriptFiles is not null)
            {
                foreach (string? scriptFile in scriptFiles)
                {
                    if (scriptFile is not null)
                    {
                        using StreamReader reader = new StreamReader(scriptFile);

                        StringBuilder command = new StringBuilder();
                        bool isStartOfCommand = true;
                        string? scriptLine;
                        int lineNo = 0;

                        while ((scriptLine = reader.ReadLine()) is not null)
                        {
                            lineNo++;

                            if (isStartOfCommand)
                            {
                                int leftWhiteSpaceAmount = LeftWhiteSpaceAmount(scriptLine);
                                if (leftWhiteSpaceAmount == scriptLine.Length)
                                    continue;
                                else
                                    scriptLine = scriptLine[leftWhiteSpaceAmount..];
                            }

                            isStartOfCommand = false;
                            (bool isEndOfCommand, int endingSlackLength) = TrimmedStringEndsWith(scriptLine, CommandDelimiter);

                            if (isEndOfCommand)
                            {
                                command.AppendLine(scriptLine[0..^endingSlackLength]);
                                yield return new ScriptFileParserOutPOCO(scriptFile, lineNo, command.ToString());
                                command = new StringBuilder();
                                isStartOfCommand = true;
                            }
                            else
                            {
                                command.AppendLine(scriptLine);
                            }
                        }

                        if (command.Length > 0)
                            throw new PrematureEndOfScriptFile(scriptFile, lineNo++, (scriptLine?.Length + 1) ?? 1, CommandDelimiter);
                    }
                }
            }
        }

        private static int LeftWhiteSpaceAmount(string scriptLine)
        {
            int ixNonWhiteSpaceChar = 0;
            while (ixNonWhiteSpaceChar < scriptLine.Length && char.IsWhiteSpace(scriptLine[ixNonWhiteSpaceChar]))
                ixNonWhiteSpaceChar++;

            return ixNonWhiteSpaceChar;
        }

        private static ValueTuple<bool, int> TrimmedStringEndsWith(string? value, string endingSequence)
        {
            if (string.IsNullOrEmpty(value))
                return new (false, 0);

            int ixEnding = value.Length - 1;
            while (ixEnding >= 0 && char.IsWhiteSpace(value[ixEnding]))
                ixEnding--;

            int endingSequenceLength = endingSequence.Length;
            ReadOnlySpan<char> endingSequenceSpan = endingSequence.AsSpan();

            if (ixEnding < 0)
                return new (false, 0);
            else if (value.AsSpan(ixEnding, endingSequenceLength).SequenceEqual(endingSequenceSpan))
                return new (true, value.Length - ixEnding);
            else
                return new (false, 0);
        }
    }
}
