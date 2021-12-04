namespace NoP77svk.TibcoDV.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NoP77svk.Linq;

    public partial class TdvWebServiceClient
    {
        public async Task<int> AssignUnassignRbsPolicyBulk(WSDL.Admin.rbsAssignmentOperationType action, string? policyFunctionPath, IEnumerable<string>? tables, int degreeOfParallelism = 8)
        {
            if (string.IsNullOrEmpty(policyFunctionPath))
                throw new ArgumentNullException(nameof(policyFunctionPath));

            if (tables is null)
                throw new ArgumentNullException(nameof(tables));

            if (!tables.Any())
                throw new ArgumentOutOfRangeException(nameof(tables), "Empty collection of tables");

            if (degreeOfParallelism < 1)
                throw new ArgumentOutOfRangeException(nameof(degreeOfParallelism), degreeOfParallelism, "Degree of parallelism must be a positive integer");

            int totalAssignments = 0;
            foreach (ChunkOf<string> chunkOfTables in tables.ChunkByCount(degreeOfParallelism))
            {
                if (chunkOfTables.Chunk is not null)
                {
                    IEnumerable<Task<List<WSDL.Admin.rbsAssignFilterPolicyResponse>>> assignUnassignTasks = chunkOfTables.Chunk
                        .Select(tableName => _wsClient.EndpointGetObject<WSDL.Admin.rbsAssignFilterPolicyResponse>(
                            new TdvSoapWsEndpoint<WSDL.Admin.rbsAssignFilterPolicyRequest>(
                                "rbsAssignFilterPolicy",
                                new WSDL.Admin.rbsAssignFilterPolicyRequest()
                                {
                                    name = policyFunctionPath,
                                    operation = action,
                                    operationSpecified = true,
                                    target = tableName
                                }
                            )
                        ))
                        .Select(asyncEnumerable => asyncEnumerable.ToListAsync().AsTask());

                    await Task.WhenAll(assignUnassignTasks);
                    totalAssignments += chunkOfTables.TotalChunkMeasure;
                }
            }

            return totalAssignments;
        }
    }
}
