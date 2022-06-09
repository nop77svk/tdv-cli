﻿#pragma warning disable SA1313
namespace NoP77svk.TibcoDV.CLI.Commons
{
    using System.Threading.Tasks;

    public record NamedTask<TTaskResult>(string Name, Task<TTaskResult> Task)
    {
    }
}
