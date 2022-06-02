#pragma warning disable SA1313
namespace NoP77svk.TibcoDV.CLI.AST.Infra
{
    using System.Text.RegularExpressions;
    using NoP77svk.Text.RegularExpressions;

    internal record MatchByRegExp(string Value) : MatchBy(Value)
    {
        private Regex? _regexp = null;

        internal Regex RegExp
        {
            get
            {
                if (_regexp == null)
                    _regexp = RegexExt.ParseSlashedRegexp(Value);

                return _regexp;
            }
        }
    }
}
