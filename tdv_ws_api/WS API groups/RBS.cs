namespace NoP77svk.TibcoDV.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NoP77svk.Linq;

    public partial class TdvWebServiceClient
    {
        public async Task AssignUnassignRbsPolicy(WSDL.Admin.rbsAssignmentOperationType action, string? policyFunctionPath, string? tablePath)
        {
            if (string.IsNullOrEmpty(policyFunctionPath))
                throw new ArgumentNullException(nameof(policyFunctionPath));

            if (string.IsNullOrWhiteSpace(tablePath))
                throw new ArgumentNullException(nameof(tablePath));

            await _wsClient.EndpointGetStream(
                new TdvSoapWsEndpoint<WSDL.Admin.rbsAssignFilterPolicyRequest>(
                    "rbsAssignFilterPolicy",
                    new WSDL.Admin.rbsAssignFilterPolicyRequest()
                    {
                        name = policyFunctionPath,
                        operation = action,
                        operationSpecified = true,
                        target = tablePath
                    }
                )
            );
        }

        public async Task<int> AssignUnassignRbsPolicy(WSDL.Admin.rbsAssignmentOperationType action, string? policyFunctionPath, IEnumerable<string>? tables, int degreeOfParallelism = 8, Action<int>? totalProcessedFeedback = null)
        {
            if (string.IsNullOrEmpty(policyFunctionPath))
                throw new ArgumentNullException(nameof(policyFunctionPath));

            if (tables is null)
                throw new ArgumentNullException(nameof(tables));

            if (degreeOfParallelism < 1)
                throw new ArgumentOutOfRangeException(nameof(degreeOfParallelism), degreeOfParallelism, "Degree of parallelism must be a positive integer");

            int totalAssignments = 0;
            foreach (ChunkOf<string> chunkOfTables in tables.ChunkByCount(degreeOfParallelism))
            {
                if (chunkOfTables.Chunk is not null)
                {
                    List<Task> assignUnassignTasks = chunkOfTables.Chunk
                        .Select(tableName => AssignUnassignRbsPolicy(action, policyFunctionPath, tableName))
                        .ToList();

                    await Task.WhenAll(assignUnassignTasks);
                    totalAssignments += chunkOfTables.TotalChunkMeasure;
                    totalProcessedFeedback?.Invoke(totalAssignments);
                }
            }

            return totalAssignments;
        }
    }
}
