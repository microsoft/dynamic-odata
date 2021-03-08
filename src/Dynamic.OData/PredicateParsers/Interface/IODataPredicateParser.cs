// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.OData.UriParser;

namespace Dynamic.OData.PredicateParsers.Interface
{
    public interface IODataPredicateParser
    {
        ParseContext Parse(ODataUriParser parser, ParseContext parseContext);
    }
}
