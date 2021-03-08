// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Dynamic.OData
{
    public class RequestFilterConstants
    {
        public static string GetEntityTypeKeyName(string step, int stepIndex)
        {
            return $"{step}_collectionentitytype_{stepIndex}".ToLowerInvariant();
        }

        public static string GetComplexTypeKeyName(string step, int stepIndex)
        {
            return $"{step}_complexentitytype_{stepIndex}".ToLowerInvariant();
        }

        public const string ContextSuffixKey = "contextsuffixkey";

        public const string ODataResponseMiddleware = "ODataResponseMiddleware";
        public const string ODataContextParsingMiddleware = "ODataContextParsingMiddleware";
        public const string ODataServiceRoot = "https://services.odata.org/V4/OData/OData.svc";
    }
}
