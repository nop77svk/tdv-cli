﻿namespace NoP77svk.TibcoDV.CLI
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    internal class ScriptFileParser
    {
        private Func<char> commandDelimiterGetter;

        public char CommandDelimiter
        {
            get
            {
                char result = commandDelimiterGetter();
                if (result != ';')
                    throw new ArgumentOutOfRangeException(nameof(result), result, "Invalid command delimiter");
                return result;
            }
        }

        public ScriptFileParser(Func<char> commandDelimiterGetter)
        {
            this.commandDelimiterGetter = commandDelimiterGetter;
        }

        internal IEnumerable<ScriptFileParserOutPOCO> SplitScriptsToStatements(IEnumerable<string?>? scriptFiles)
        {
            if (scriptFiles is not null)
            {
                foreach (string? scriptFile in scriptFiles)
                {
                    if (scriptFile is not null)
                    {
                        StreamReader reader = new StreamReader(scriptFile);

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

        private static ValueTuple<bool, int> TrimmedStringEndsWith(string? value, char endingChar)
        {
            if (string.IsNullOrEmpty(value))
                return new (false, 0);

            int ixEnding = value.Length - 1;
            while (ixEnding >= 0 && char.IsWhiteSpace(value[ixEnding]))
                ixEnding--;

            if (ixEnding < 0)
                return new (false, 0);
            else if (value[ixEnding] == endingChar)
                return new (true, value.Length - ixEnding);
            else
                return new (false, 0);
        }
    }
}
