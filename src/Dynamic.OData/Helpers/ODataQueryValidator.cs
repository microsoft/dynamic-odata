// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Query.Validator;

namespace Dynamic.OData.Helpers
{
    public class ODataQueryValidator2 : IODataQueryValidator
    {
        public void Validate(ODataQueryOptions options, ODataValidationSettings validationSettings)
        {
            options.Validate(new ODataValidationSettings());
        }
    }
}
