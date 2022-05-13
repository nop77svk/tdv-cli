﻿namespace NoP77svk.TibcoDV.CLI.AST
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using NoP77svk.TibcoDV.API;
    using NoP77svk.TibcoDV.CLI.Commons;

    internal class CommandIntrospect : IAsyncStatement
    {
        public IList<IntrospectTargetDataSource> DataSources { get; }

        public CommandIntrospect(IList<IntrospectTargetDataSource> dataSources)
        {
            DataSources = dataSources;
        }

        public async Task Execute(TdvWebServiceClient tdvClient, IInfoOutput output)
        {
            throw new NotImplementedException();
        }
    }
}