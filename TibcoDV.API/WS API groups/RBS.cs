﻿namespace NoP77svk.TibcoDV.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public partial class TdvWebServiceClient
    {
        public async Task AssignUnassignRbsPolicy(WSDL.Admin.rbsAssignmentOperationType action, string? policyFunctionPath, string? tablePath)
        {
            if (string.IsNullOrEmpty(policyFunctionPath))
                throw new ArgumentNullException(nameof(policyFunctionPath));

            if (string.IsNullOrWhiteSpace(tablePath))
                throw new ArgumentNullException(nameof(tablePath));

            await _wsClient.EndpointGetObject<WSDL.Admin.rbsAssignFilterPolicyResponse>(
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
            ).FirstAsync();
        }

        public async IAsyncEnumerable<string> GetRbsPolicyAssignmentList(string? policyFunctionPath)
        {
            await foreach (WSDL.Admin.rbsGetFilterPolicyResponse resp in GetRbsPolicyInfo(policyFunctionPath))
            {
                foreach (string ass in resp.policy.assignmentList)
                    yield return ass;
            }
        }

        public async IAsyncEnumerable<WSDL.Admin.rbsGetFilterPolicyResponse> GetRbsPolicyInfo(string? policyFunctionPath)
        {
            if (string.IsNullOrEmpty(policyFunctionPath))
                throw new ArgumentNullException(nameof(policyFunctionPath));

            await foreach (WSDL.Admin.rbsGetFilterPolicyResponse res in _wsClient.EndpointGetObject<WSDL.Admin.rbsGetFilterPolicyResponse>(
                new TdvSoapWsEndpoint<WSDL.Admin.rbsGetFilterPolicyRequest>(
                    "rbsGetFilterPolicy",
                    new WSDL.Admin.rbsGetFilterPolicyRequest()
                    {
                        name = policyFunctionPath
                    }
                )
            ))
            {
                yield return res;
            }
        }

        public async Task<WSDL.Admin.rbsWriteFilterPolicyResponse> WriteRbsPolicy(WSDL.Admin.rbsWriteFilterPolicyRequest req)
        {
            return await _wsClient.EndpointGetObject<WSDL.Admin.rbsWriteFilterPolicyResponse>(new TdvSoapWsEndpoint<WSDL.Admin.rbsWriteFilterPolicyRequest>("rbsWriteFilterPolicy", req))
                .FirstAsync();
        }
    }
}
