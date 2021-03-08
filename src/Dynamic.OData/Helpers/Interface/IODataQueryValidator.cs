// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNet.OData.Query;

namespace Dynamic.OData.Helpers.Interface
{
    public interface IODataQueryValidator
    {
        void ValidateQuery(ODataQueryOptions oDataQueryOptions);
    }
}
