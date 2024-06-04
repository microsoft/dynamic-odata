// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Dynamic.OData.Exceptions;
using Dynamic.OData.PredicateParsers.Interface;
using Microsoft.AspNetCore.OData.Formatter.Value;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using System.Linq;

namespace Dynamic.OData.PredicateParsers
{
    public class ODataTopPredicateParser : BaseODataPredicateParser, IODataPredicateParser
    {
        private const string TopParser = "Top";
        private const int StepIndex = 8;
        public ParseContext Parse(ODataUriParser parser, ParseContext parseContext)
        {
            var top = parser.ParseTop();
            if (top.Value > 0)
            {
                bool isParsed = int.TryParse(top.Value.ToString(), out int count);

                var collectionEntityTypeKey = parseContext.LatestStateDictionary
                    .Keys.FirstOrDefault(p => p.Contains("collectionentitytype"));

                var entityRef = (EdmEntityTypeReference)parseContext.LatestStateDictionary[collectionEntityTypeKey];
                var collectionRef = new EdmCollectionTypeReference(new EdmCollectionType(entityRef));
                var collection = new EdmEntityObjectCollection(collectionRef);

                var filteredResults = parseContext.Result.Take(count);

                foreach (var entity in filteredResults)
                    collection.Add(entity);

                var targetParseContext = new ParseContext
                {
                    Result = collection,
                    QueryableSourceEntities = parseContext.QueryableSourceEntities.Take(count),
                    Model = parseContext.Model,
                    EdmEntityTypeSettings = parseContext.EdmEntityTypeSettings,
                    LatestStateDictionary = parseContext.LatestStateDictionary
                };
                return targetParseContext;

            }
            else
            {
                throw new InvalidPropertyException("Top", string.Empty);
            }
        }
    }
}
