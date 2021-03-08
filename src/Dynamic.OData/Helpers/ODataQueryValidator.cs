// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNet.OData.Query;
using Dynamic.OData.Helpers.Interface;

namespace Dynamic.OData.Helpers
{
    public class ODataQueryValidator : IODataQueryValidator
    {
        public void ValidateQuery(ODataQueryOptions oDataQueryOptions)
        {
            oDataQueryOptions.Validate(new ODataValidationSettings());
        }
    }
}
