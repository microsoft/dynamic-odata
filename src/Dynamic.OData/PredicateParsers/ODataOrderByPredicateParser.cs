// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Dynamic.OData.Exceptions;
using Dynamic.OData.PredicateParsers.Interface;
using Microsoft.AspNetCore.OData.Formatter.Value;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dynamic.OData.PredicateParsers
{
    public class ODataOrderByPredicateParser : BaseODataPredicateParser, IODataPredicateParser
    {
        private const string OrderByParser = "OrderBy";
        public ParseContext Parse(ODataUriParser parser, ParseContext parseContext)
        {
            var sourceEdmSetting = parseContext.EdmEntityTypeSettings.FirstOrDefault();

            // Get primary collection type and setup collection
            var collectionEntityTypeKey = parseContext.LatestStateDictionary
                    .Keys.FirstOrDefault(p => p.Contains("collectionentitytype"));
            var entityRef = (EdmEntityTypeReference)parseContext.LatestStateDictionary[collectionEntityTypeKey];
            var collectionRef = new EdmCollectionTypeReference(new EdmCollectionType(entityRef));
            var collection = new EdmEntityObjectCollection(collectionRef);

            // Get orderby clause
            var orderByClause = parser.ParseOrderBy();

            bool isFirstIter = true;
            IEnumerable<IEdmEntityObject> resultStart = parseContext.Result;
            IEnumerable<Dictionary<string, object>> queryableStart = parseContext.QueryableSourceEntities;
            IOrderedEnumerable<IEdmEntityObject> result = null;
            IOrderedEnumerable<Dictionary<string, object>> queryable = null;
            while (orderByClause != null)
            {
                // Get attribute name
                string attributeName = "";
                var kind = orderByClause.Expression.Kind;
                if (kind == QueryNodeKind.SingleValueOpenPropertyAccess)
                    attributeName = ((SingleValueOpenPropertyAccessNode)orderByClause.Expression).Name;
                else if (kind == QueryNodeKind.SingleValuePropertyAccess)
                    attributeName = ((SingleValuePropertyAccessNode)orderByClause.Expression).Property.Name;
                else
                    throw new FeatureNotSupportedException(OrderByParser, $"QueryNodeKind: {kind} Not Supported");
                // Check if attribute name in model
                var typeSetting = sourceEdmSetting.Properties.FirstOrDefault(predicate => predicate.PropertyName == attributeName);
                // Get direction
                var direction = orderByClause.Direction.ToString();
                // Perform ordering
                Func<IEdmEntityObject, object> resultExpression = item => GetPropertyValue(item, attributeName);
                Func<Dictionary<string, object>, object> queryableExpression = item => item[attributeName];
                if (string.Compare(direction, "Ascending") == 0)
                {
                    result = (isFirstIter) ? resultStart.OrderBy(resultExpression) :
                        result.ThenBy(resultExpression);
                    queryable = (isFirstIter) ? queryableStart.OrderBy(queryableExpression) :
                        queryable.ThenBy(queryableExpression);
                }
                else
                {
                    result = (isFirstIter) ? resultStart.OrderByDescending(resultExpression) :
                        result.ThenByDescending(resultExpression);
                    queryable = (isFirstIter) ? queryableStart.OrderByDescending(queryableExpression) :
                        queryable.ThenByDescending(queryableExpression);
                }
                isFirstIter = false;
                // Go to next ordering clause
                orderByClause = orderByClause.ThenBy;
            }
            // Create collection
            foreach (var entity in result)
                collection.Add(entity);

            var targetParseContext = new ParseContext
            {
                Result = collection,
                QueryableSourceEntities = queryable,
                Model = parseContext.Model,
                EdmEntityTypeSettings = parseContext.EdmEntityTypeSettings,
                LatestStateDictionary = parseContext.LatestStateDictionary
            };
            return targetParseContext;
        }

        private object GetPropertyValue(IEdmEntityObject p, string attributeName)
        {
            p.TryGetPropertyValue(attributeName, out object value);
            return value;
        }
    }
}
