// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.OData.Query;

namespace Dynamic.OData.Helpers.Interface
{
    public interface IODataQueryValidator2
    {
        void ValidateQuery(ODataQueryOptions oDataQueryOptions);
    }
}
