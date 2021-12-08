namespace NoP77svk.TibcoDV.CLI.AST
{
    internal class ClientConnectionTimeout
    {
        internal TimeSpan TimeSpan { get; }

        internal ClientConnectionTimeout(TimeSpan timeSpan)
        {
            TimeSpan = timeSpan;
        }
    }
}
