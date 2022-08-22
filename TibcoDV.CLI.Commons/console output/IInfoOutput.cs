namespace NoP77svk.TibcoDV.CLI.Commons
{
    using System;

    public interface IInfoOutput
    {
        void Info(string message);

        void InfoNoEoln(string message);

        void InfoCR(string message);

        void EndCR();

        void Error(Exception e);

        void Error(string message, Exception? e = null);
    }
}
