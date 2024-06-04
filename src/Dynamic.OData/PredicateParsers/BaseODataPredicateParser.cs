// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Dynamic.OData.Models;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.AspNetCore.OData.Formatter.Value;

namespace Dynamic.OData.PredicateParsers
{
    public abstract class BaseODataPredicateParser
    {
        public static string EdmNamespaceName;
        protected const string FeatureNotSupported = "Invalid {0} clause- Current Implementation only supports primitive types";
        protected const string InvalidSelectProperty = "Invalid {0} clause - Property {1} does not exist in model";
        protected string GetStringTypeFromEdmPrimitiveType(EdmPrimitiveTypeKind edmPrimitiveType)
        {
            switch (edmPrimitiveType)
            {
                case EdmPrimitiveTypeKind.DateTimeOffset: return TypeHandlingConstants.DateTime;
                case EdmPrimitiveTypeKind.TimeOfDay: return TypeHandlingConstants.TimeOfDay;
                case EdmPrimitiveTypeKind.Date: return TypeHandlingConstants.Date;
                case EdmPrimitiveTypeKind.Int32: return TypeHandlingConstants.Int32;
                case EdmPrimitiveTypeKind.Int16: return TypeHandlingConstants.Int16;
                case EdmPrimitiveTypeKind.Int64: return TypeHandlingConstants.Int64;
                case EdmPrimitiveTypeKind.Double: return TypeHandlingConstants.Double;
                case EdmPrimitiveTypeKind.Boolean: return TypeHandlingConstants.Boolean;
                case EdmPrimitiveTypeKind.Guid: return TypeHandlingConstants.Guid;
                case EdmPrimitiveTypeKind.String: return TypeHandlingConstants.String;
                case EdmPrimitiveTypeKind.Decimal: return TypeHandlingConstants.Decimal;
                default: return TypeHandlingConstants.None;
            }
        }

        /// <summary>
        /// Gets a List for all entities of a group.
        /// </summary>
        /// <param name="group"></param>
        /// <param name="edmComplexType"></param>
        /// <returns></returns>
        protected SubCollectionContext GetList(IEnumerable<Dictionary<string, object>> group, EdmComplexType edmComplexType, int limit = 0)
        {
            var queryable = new List<Dictionary<string, object>>();
            var collectionTypeReference = new EdmCollectionTypeReference(new EdmCollectionType(new EdmComplexTypeReference(edmComplexType, true)));
            var subCollection = new EdmComplexObjectCollection(collectionTypeReference);
            int count = 0;
            foreach (var entity in group)
            {
                var dict = new Dictionary<string, object>();
                var edmComplexObject = new EdmComplexObject(edmComplexType);
                foreach (var propertyKvp in entity)
                {
                    edmComplexObject.TrySetPropertyValue(propertyKvp.Key, propertyKvp.Value);
                    dict.Add(propertyKvp.Key, propertyKvp.Value);
                }
                subCollection.Add(edmComplexObject);
                queryable.Add(dict);
                if (limit >= 1 && ++count == limit)
                    break;
            }
            return new SubCollectionContext { Result = subCollection, QueryAbleResult = queryable };
        }
        protected IEnumerable<IEdmEntityObject> GetFilteredResult(IEnumerable<Dictionary<string, object>> group, string query, ParseContext parseContext)
        {
            // Create collection from group
            var collectionEntityTypeKey = parseContext.LatestStateDictionary
                .Keys.FirstOrDefault(p => p.Contains("collectionentitytype"));
            var entityRef = (EdmEntityTypeReference)parseContext.LatestStateDictionary[collectionEntityTypeKey];
            var collectionRef = new EdmCollectionTypeReference(new EdmCollectionType(entityRef));
            var collection = new EdmEntityObjectCollection(collectionRef);
            foreach (var entity in group)
            {
                var obj = new EdmEntityObject(entityRef);
                foreach (var kvp in entity)
                    obj.TrySetPropertyValue(kvp.Key, kvp.Value);
                collection.Add(obj);
            }
            // Create filter query using the entity supplied in the lateststatedictionary
            var serviceRoot = new Uri(RequestFilterConstants.ODataServiceRoot);
            var resource = entityRef.Definition.FullTypeName().Split(".").Last();
            var filterQuery = "/" + resource + "?$filter=" + Uri.EscapeDataString(query.Substring(1, query.Length - 2).Replace("''", "'"));
            var oDataUriParser = new ODataUriParser(parseContext.Model, new Uri(filterQuery, UriKind.Relative));

            // Parse filterquery
            var filter = oDataUriParser.ParseFilter();
            var odataFilter = new ODataFilterPredicateParser();
            var filteredResult = odataFilter.ApplyFilter(parseContext.EdmEntityTypeSettings.FirstOrDefault(), collection, filter.Expression);
            return filteredResult;
        }
        protected static string GetAttributeName(SingleValueNode node)
        {
            var attributeName = string.Empty;
            if (node.Kind == QueryNodeKind.SingleValueOpenPropertyAccess)
                attributeName = ((SingleValueOpenPropertyAccessNode)node).Name;
            else if (node.Kind == QueryNodeKind.SingleValuePropertyAccess)
                attributeName = ((SingleValuePropertyAccessNode)node).Property.Name;
            else if (node.Kind == QueryNodeKind.SingleValueFunctionCall)
            {
                foreach (var x in ((SingleValueFunctionCallNode)node).Parameters)
                {
                    attributeName = ((SingleValuePropertyAccessNode)x).Property.Name;
                }

            }
            else if(node.Kind == QueryNodeKind.Convert)
            {
                 if(((ConvertNode)node).Source.Kind == QueryNodeKind.SingleValuePropertyAccess)
                    attributeName = ((SingleValuePropertyAccessNode)((ConvertNode)node).Source).Property.Name;
            }
            return attributeName;
        }

        protected static string GetParamAttributeName(QueryNode node)
        {
            var attributeName = string.Empty;
            if (node.Kind == QueryNodeKind.SingleValueOpenPropertyAccess)
                attributeName = ((SingleValueOpenPropertyAccessNode)node).Name;
            else if (node.Kind == QueryNodeKind.SingleValuePropertyAccess)
                attributeName = ((SingleValuePropertyAccessNode)node).Property.Name;

            return attributeName;
        }

        protected static EdmPrimitiveTypeKind GetDataType(SingleValueNode node)
        {
            var dataType = EdmPrimitiveTypeKind.None;
            if (node.Kind == QueryNodeKind.SingleValueOpenPropertyAccess)
                dataType = ((EdmTypeReference)((SingleValueOpenPropertyAccessNode)node).TypeReference).PrimitiveKind();
            else if (node.Kind == QueryNodeKind.SingleValuePropertyAccess)
                dataType = ((EdmTypeReference)((SingleValuePropertyAccessNode)node).TypeReference).PrimitiveKind();
            else if (node.Kind == QueryNodeKind.Constant)
                dataType = ((EdmTypeReference)((ConstantNode)node).TypeReference).PrimitiveKind();
            else if (node.Kind == QueryNodeKind.Convert)
                dataType = ((EdmTypeReference)((ConvertNode)node).TypeReference).PrimitiveKind();
            else if (node.Kind == QueryNodeKind.SingleValueFunctionCall)
            {
                foreach (var x in ((SingleValueFunctionCallNode)node).Parameters)
                {
                    dataType = ((EdmTypeReference)((SingleValuePropertyAccessNode)x).TypeReference).PrimitiveKind();
                    if (dataType != EdmPrimitiveTypeKind.None)
                        break;
                }
            }

            return dataType;
        }

        protected static EdmPrimitiveTypeKind GetDataType(QueryNode node)
        {
            var dataType = EdmPrimitiveTypeKind.None;
            if (node.Kind == QueryNodeKind.SingleValueOpenPropertyAccess)
                dataType = ((EdmTypeReference)((SingleValueOpenPropertyAccessNode)node).TypeReference).PrimitiveKind();
            else if (node.Kind == QueryNodeKind.SingleValuePropertyAccess)
                dataType = ((EdmTypeReference)((SingleValuePropertyAccessNode)node).TypeReference).PrimitiveKind();
            else if (node.Kind == QueryNodeKind.Constant)
                dataType = ((EdmTypeReference)((ConstantNode)node).TypeReference).PrimitiveKind();
            else if (node.Kind == QueryNodeKind.Convert)
                dataType = ((EdmTypeReference)((ConvertNode)node).TypeReference).PrimitiveKind();


            return dataType;
        }

        protected object GetPropertyValue(IEdmEntityObject p, string attributeName)
        {
            p.TryGetPropertyValue(attributeName, out object value);
            return value != null ? value : null;
        }
        protected object GetPropertyValue(IEdmEntityObject p, string attributeName, object attributeValue)
        {
            if (attributeValue != null)
                //specific null handling coz Equals and other aspects would need this 
                return attributeValue != null ? attributeValue : "--NULL--";
            else
            {
                p.TryGetPropertyValue(attributeName, out object value);
                //specific null handling coz Equals and other aspects would need this 
                return value != null ? value : "--NULL--";
            }
        }

        protected object GetPropertyValue(SingleValueNode node)
        {
            object value = null;
            if (node.Kind == QueryNodeKind.Constant)
                value = ((ConstantNode)node).Value;
            else if (node.Kind == QueryNodeKind.Convert && ((ConvertNode)node).Source.Kind == QueryNodeKind.Constant)
                value = ((ConstantNode)((ConvertNode)node).Source).Value;

            return value;
        }
        protected object GetPropertyValue(QueryNode node)
        {
            object value = null;
            if (node.Kind == QueryNodeKind.Constant)
                value = ((ConstantNode)node).Value;
            else if (node.Kind == QueryNodeKind.Convert && ((ConvertNode)node).Source.Kind == QueryNodeKind.Constant)
                value = ((ConstantNode)((ConvertNode)node).Source).Value;

            return value;
        }
    }
}
