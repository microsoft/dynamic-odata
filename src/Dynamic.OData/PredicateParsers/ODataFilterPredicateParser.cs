// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Dynamic.OData.Models;
using Dynamic.OData.PredicateParsers.Interface;
using Microsoft.AspNetCore.OData.Formatter.Value;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Dynamic.OData.PredicateParsers
{
    public class ODataFilterPredicateParser : BaseODataPredicateParser, IODataPredicateParser
    {
        private const string FilterParser = "Filter";
        private const string IdProperty = "id";

        public ParseContext Parse(ODataUriParser parser, ParseContext parseContext)
        {
            var rootfilter = parser.ParseFilter();
            var sourceEdmSetting = parseContext.EdmEntityTypeSettings.FirstOrDefault();


            var collectionEntityTypeKey = parseContext.LatestStateDictionary
                .Keys.FirstOrDefault(p => p.Contains("collectionentitytype"));

            var entityRef = (EdmEntityTypeReference)parseContext.LatestStateDictionary[collectionEntityTypeKey];
            var collectionRef = new EdmCollectionTypeReference(new EdmCollectionType(entityRef));
            var collection = new EdmEntityObjectCollection(collectionRef);


            var filterdata = ApplyFilter(sourceEdmSetting, parseContext.Result, rootfilter.Expression);
            var filteredResults = parseContext.Result.Intersect(filterdata);



            foreach (var entity in filteredResults)
                collection.Add(entity);

            var idList = filteredResults
              .Select(p => p.TryGetPropertyValue(IdProperty, out object id) ? (Guid)id : Guid.Empty)
              .ToDictionary(p => p, q => !q.Equals(Guid.Empty));

            var targetParseContext = new ParseContext
            {
                Result = collection,
                QueryableSourceEntities = parseContext.QueryableSourceEntities.Where(p => idList.ContainsKey((Guid)p[IdProperty])).ToList(),
                Model = parseContext.Model,
                EdmEntityTypeSettings = parseContext.EdmEntityTypeSettings,
                LatestStateDictionary = parseContext.LatestStateDictionary
            };
            return targetParseContext;



        }

        public IEnumerable<IEdmEntityObject> ApplyFilter(EdmEntityTypeSettings sourceEdmSetting, IEnumerable<IEdmEntityObject> items, SingleValueNode filter, string commandPrefix = "")
        {

            //var typeSetting = sourceEdmSetting.Properties.FirstOrDefault(predicate => predicate.PropertyName == attributeName);
            //if (typeSetting == null)
            //    throw new InvalidPropertyException("Filter", attributeName);

            SingleValueNode right = null;
            SingleValueNode left = null;
            BinaryOperatorKind opr = BinaryOperatorKind.Equal;
            QueryNodeKind kind = QueryNodeKind.None;

            QueryNode param1 = null;
            QueryNode param2 = null;
            string function = string.Empty;
            if (filter.Kind == QueryNodeKind.UnaryOperator)
            {
                if (((UnaryOperatorNode)filter).OperatorKind == UnaryOperatorKind.Not)
                {
                    commandPrefix = "not";
                    kind = ((UnaryOperatorNode)filter).Kind;
                    right = ((UnaryOperatorNode)filter).Operand;
                }
            }
            else if (filter.Kind == QueryNodeKind.Convert && ((ConvertNode)filter).Source.Kind == QueryNodeKind.UnaryOperator)
            {
                if (((UnaryOperatorNode)((ConvertNode)filter).Source).OperatorKind == UnaryOperatorKind.Not)
                {
                    commandPrefix = "not";
                    kind = ((UnaryOperatorNode)((ConvertNode)filter).Source).Kind;
                    right = ((UnaryOperatorNode)((ConvertNode)filter).Source).Operand;
                }
            }
            else if (filter.Kind == QueryNodeKind.BinaryOperator)
            {
                right = ((BinaryOperatorNode)filter).Right;
                left = ((BinaryOperatorNode)filter).Left;
                opr = ((BinaryOperatorNode)filter).OperatorKind;
                kind = ((BinaryOperatorNode)filter).Kind;
            }
            else if (filter.Kind == QueryNodeKind.Convert && ((ConvertNode)filter).Source.Kind == QueryNodeKind.BinaryOperator)
            {
                right = ((BinaryOperatorNode)((ConvertNode)filter).Source).Right;
                left = ((BinaryOperatorNode)((ConvertNode)filter).Source).Left;
                opr = ((BinaryOperatorNode)((ConvertNode)filter).Source).OperatorKind;
                kind = ((ConvertNode)filter).Source.Kind;
            }
            else if (filter.Kind == QueryNodeKind.SingleValueFunctionCall)
            {
                kind = filter.Kind;
                param1 = ((SingleValueFunctionCallNode)filter).Parameters.ElementAt(0);
                param2 = ((SingleValueFunctionCallNode)filter).Parameters.ElementAt(1);
                function = ((SingleValueFunctionCallNode)filter).Name;
            }
            else if (filter.Kind == QueryNodeKind.Convert)
            {
                if (((ConvertNode)filter).Source.Kind == QueryNodeKind.SingleValueFunctionCall)
                {
                    kind = ((ConvertNode)filter).Source.Kind;
                    param1 = ((SingleValueFunctionCallNode)((ConvertNode)filter).Source).Parameters.ElementAt(0);
                    param2 = ((SingleValueFunctionCallNode)((ConvertNode)filter).Source).Parameters.ElementAt(1);
                    function = ((SingleValueFunctionCallNode)((ConvertNode)filter).Source).Name;
                }
            }

            if (kind == QueryNodeKind.BinaryOperator || kind == QueryNodeKind.UnaryOperator || kind == QueryNodeKind.SingleValueFunctionCall)
            {
                if (opr == BinaryOperatorKind.And)
                {
                    items = ApplyFilter(sourceEdmSetting, items, left).Intersect(ApplyFilter(sourceEdmSetting, items, right));

                }
                else if (opr == BinaryOperatorKind.Or)
                {
                    items = ApplyFilter(sourceEdmSetting, items, left).Union(ApplyFilter(sourceEdmSetting, items, right));

                }
                else if (function == "startswith" || function == "endswith" || function == "contains")
                {
                    var param1AttributeName = GetParamAttributeName(param1);
                    var param2AttributeName = GetParamAttributeName(param2);
                    var param1AttributeValue = GetPropertyValue(param1);
                    var param2AttributeValue = GetPropertyValue(param2);
                    var param1AttributeType = GetDataType(param1);
                    var param2AttributeType = GetDataType(param2);

                    switch (commandPrefix + function)
                    {
                        case "startswith":
                            items = items.Where(x => GetPropertyValue(x, param1AttributeName, param1AttributeValue).ToString().StartsWith(GetPropertyValue(x, param2AttributeName, param2AttributeValue).ToString()));
                            break;
                        case "endswith":
                            items = items.Where(x => GetPropertyValue(x, param1AttributeName, param1AttributeValue).ToString().EndsWith(GetPropertyValue(x, param2AttributeName, param2AttributeValue).ToString()));
                            break;
                        case "contains":
                            items = items.Where(x => GetPropertyValue(x, param1AttributeName, param1AttributeValue).ToString().Contains(GetPropertyValue(x, param2AttributeName, param2AttributeValue).ToString()));
                            break;
                        case "notstartswith":
                            items = items.Where(x => !GetPropertyValue(x, param1AttributeName, param1AttributeValue).ToString().StartsWith(GetPropertyValue(x, param2AttributeName, param2AttributeValue).ToString()));
                            break;
                        case "notendswith":
                            items = items.Where(x => !GetPropertyValue(x, param1AttributeName, param1AttributeValue).ToString().EndsWith(GetPropertyValue(x, param2AttributeName, param2AttributeValue).ToString()));
                            break;
                        case "notcontains":
                            items = items.Where(x => !GetPropertyValue(x, param1AttributeName, param1AttributeValue).ToString().Contains(GetPropertyValue(x, param2AttributeName, param2AttributeValue).ToString()));
                            break;
                    }
                }
                else if (commandPrefix == "not")
                {
                    items = ApplyFilter(sourceEdmSetting, items, right, commandPrefix);

                }
                else if (opr == BinaryOperatorKind.Equal || opr == BinaryOperatorKind.NotEqual || opr == BinaryOperatorKind.GreaterThan || opr == BinaryOperatorKind.GreaterThanOrEqual || opr == BinaryOperatorKind.LessThan || opr == BinaryOperatorKind.LessThanOrEqual)
                {
                    var leftAttributeName = GetAttributeName(left);
                    var rightAttributeName = GetAttributeName(right);
                    var leftAttributeValue = GetPropertyValue(left);
                    var rightAttributeValue = GetPropertyValue(right);
                    var leftAttributeType = GetDataType(left);
                    var rightAttributeType = GetDataType(right);

                    switch (opr)
                    {
                        case BinaryOperatorKind.Equal:
                            if (leftAttributeType == EdmPrimitiveTypeKind.DateTimeOffset)
                                items = DateEquals(items, leftAttributeName, rightAttributeName, leftAttributeValue, rightAttributeValue, leftAttributeType, left);
                            else
                                items = items.Where(x => GetPropertyValue(x, leftAttributeName, leftAttributeValue).Equals(GetPropertyValue(x, rightAttributeName, rightAttributeValue)));
                            break;
                        case BinaryOperatorKind.NotEqual:
                            if (leftAttributeType == EdmPrimitiveTypeKind.DateTimeOffset)
                                items = DateNotEquals(items, leftAttributeName, rightAttributeName, leftAttributeValue, rightAttributeValue, leftAttributeType, left);
                            else
                                items = items.Where(x => !(GetPropertyValue(x, leftAttributeName, leftAttributeValue).Equals(GetPropertyValue(x, rightAttributeName, rightAttributeValue))));
                            break;
                        case BinaryOperatorKind.GreaterThan:
                            items = GreaterThan(items, leftAttributeName, rightAttributeName, leftAttributeValue, rightAttributeValue, leftAttributeType);
                            break;
                        case BinaryOperatorKind.GreaterThanOrEqual:
                            items = GreaterThanOrEqual(items, leftAttributeName, rightAttributeName, leftAttributeValue, rightAttributeValue, leftAttributeType);
                            break;
                        case BinaryOperatorKind.LessThan:
                            items = LesserThan(items, leftAttributeName, rightAttributeName, leftAttributeValue, rightAttributeValue, leftAttributeType);
                            break;
                        case BinaryOperatorKind.LessThanOrEqual:
                            items = LesserThanOrEqual(items, leftAttributeName, rightAttributeName, leftAttributeValue, rightAttributeValue, leftAttributeType);
                            break;
                    }

                }


            }

            return items;
        }

        private IEnumerable<IEdmEntityObject> GreaterThan(IEnumerable<IEdmEntityObject> items, string leftAttributeName, string rightAttributeName, object leftAttributeValue, object rightAttributeValue, EdmPrimitiveTypeKind leftAttributeType)
        {
            switch (leftAttributeType)
            {
                case EdmPrimitiveTypeKind.Int32:
                    items = items.Where(x => Convert.ToInt32(GetPropertyValue(x, leftAttributeName, leftAttributeValue)) > Convert.ToInt32(GetPropertyValue(x, rightAttributeName, rightAttributeValue)));
                    break;
                case EdmPrimitiveTypeKind.Int64:
                    items = items.Where(x => Convert.ToInt64(GetPropertyValue(x, leftAttributeName, leftAttributeValue)) > Convert.ToInt64(GetPropertyValue(x, rightAttributeName, rightAttributeValue)));
                    break;
                case EdmPrimitiveTypeKind.Double:
                    items = items.Where(x => Convert.ToDouble(GetPropertyValue(x, leftAttributeName, leftAttributeValue)) > Convert.ToDouble(GetPropertyValue(x, rightAttributeName, rightAttributeValue)));
                    break;
                case EdmPrimitiveTypeKind.Decimal:
                    items = items.Where(x => Convert.ToDecimal(GetPropertyValue(x, leftAttributeName, leftAttributeValue)) > Convert.ToDecimal(GetPropertyValue(x, rightAttributeName, rightAttributeValue)));
                    break;
                case EdmPrimitiveTypeKind.DateTimeOffset:
                    items = items.Where((x) =>
                    {
                        var leftAttribute = Convert.ToDateTime(GetPropertyValue(x, leftAttributeName, leftAttributeValue).ToString());
                        var rightAttribute = Convert.ToDateTime(GetPropertyValue(x, rightAttributeName, rightAttributeValue).ToString());
                        return leftAttribute > rightAttribute;
                    });
                    break;
            }

            return items;
        }

        private IEnumerable<IEdmEntityObject> GreaterThanOrEqual(IEnumerable<IEdmEntityObject> items, string leftAttributeName, string rightAttributeName, object leftAttributeValue, object rightAttributeValue, EdmPrimitiveTypeKind leftAttributeType)
        {
            switch (leftAttributeType)
            {
                case EdmPrimitiveTypeKind.Int32:
                    items = items.Where(x => Convert.ToInt32(GetPropertyValue(x, leftAttributeName, leftAttributeValue)) >= Convert.ToInt32(GetPropertyValue(x, rightAttributeName, rightAttributeValue)));
                    break;
                case EdmPrimitiveTypeKind.Int64:
                    items = items.Where(x => Convert.ToInt64(GetPropertyValue(x, leftAttributeName, leftAttributeValue)) >= Convert.ToInt64(GetPropertyValue(x, rightAttributeName, rightAttributeValue)));
                    break;
                case EdmPrimitiveTypeKind.Double:
                    items = items.Where(x => Convert.ToDouble(GetPropertyValue(x, leftAttributeName, leftAttributeValue)) >= Convert.ToDouble(GetPropertyValue(x, rightAttributeName, rightAttributeValue)));
                    break;
                case EdmPrimitiveTypeKind.Decimal:
                    items = items.Where(x => Convert.ToDecimal(GetPropertyValue(x, leftAttributeName, leftAttributeValue)) >= Convert.ToDecimal(GetPropertyValue(x, rightAttributeName, rightAttributeValue)));
                    break;
                case EdmPrimitiveTypeKind.DateTimeOffset:
                    items = items.Where((x) =>
                    {
                        var leftAttribute = Convert.ToDateTime(GetPropertyValue(x, leftAttributeName, leftAttributeValue).ToString());
                        var rightAttribute = Convert.ToDateTime(GetPropertyValue(x, rightAttributeName, rightAttributeValue).ToString());
                        return leftAttribute >= rightAttribute;
                    });
                    break;
            }

            return items;
        }



        private IEnumerable<IEdmEntityObject> LesserThan(IEnumerable<IEdmEntityObject> items, string leftAttributeName, string rightAttributeName, object leftAttributeValue, object rightAttributeValue, EdmPrimitiveTypeKind leftAttributeType)
        {
            switch (leftAttributeType)
            {
                case EdmPrimitiveTypeKind.Int32:
                    items = items.Where(x => Convert.ToInt32(GetPropertyValue(x, leftAttributeName, leftAttributeValue)) < Convert.ToInt32(GetPropertyValue(x, rightAttributeName, rightAttributeValue)));
                    break;
                case EdmPrimitiveTypeKind.Int64:
                    items = items.Where(x => Convert.ToInt64(GetPropertyValue(x, leftAttributeName, leftAttributeValue)) < Convert.ToInt64(GetPropertyValue(x, rightAttributeName, rightAttributeValue)));
                    break;
                case EdmPrimitiveTypeKind.Double:
                    items = items.Where(x => Convert.ToDouble(GetPropertyValue(x, leftAttributeName, leftAttributeValue)) < Convert.ToDouble(GetPropertyValue(x, rightAttributeName, rightAttributeValue)));
                    break;
                case EdmPrimitiveTypeKind.Decimal:
                    items = items.Where(x => Convert.ToDecimal(GetPropertyValue(x, leftAttributeName, leftAttributeValue)) < Convert.ToDecimal(GetPropertyValue(x, rightAttributeName, rightAttributeValue)));
                    break;
                case EdmPrimitiveTypeKind.DateTimeOffset:
                    items = items.Where((x) =>
                    {
                        var leftAttribute = Convert.ToDateTime(GetPropertyValue(x, leftAttributeName, leftAttributeValue).ToString());
                        var rightAttribute = Convert.ToDateTime(GetPropertyValue(x, rightAttributeName, rightAttributeValue).ToString());
                        return leftAttribute < rightAttribute;
                    }
                    );
                    break;
            }

            return items;
        }
        private IEnumerable<IEdmEntityObject> LesserThanOrEqual(IEnumerable<IEdmEntityObject> items, string leftAttributeName, string rightAttributeName, object leftAttributeValue, object rightAttributeValue, EdmPrimitiveTypeKind leftAttributeType)
        {
            switch (leftAttributeType)
            {
                case EdmPrimitiveTypeKind.Int32:
                    items = items.Where(x => Convert.ToInt32(GetPropertyValue(x, leftAttributeName, leftAttributeValue)) <= Convert.ToInt32(GetPropertyValue(x, rightAttributeName, rightAttributeValue)));
                    break;
                case EdmPrimitiveTypeKind.Int64:
                    items = items.Where(x => Convert.ToInt64(GetPropertyValue(x, leftAttributeName, leftAttributeValue)) <= Convert.ToInt64(GetPropertyValue(x, rightAttributeName, rightAttributeValue)));
                    break;
                case EdmPrimitiveTypeKind.Double:
                    items = items.Where(x => Convert.ToDouble(GetPropertyValue(x, leftAttributeName, leftAttributeValue)) <= Convert.ToDouble(GetPropertyValue(x, rightAttributeName, rightAttributeValue)));
                    break;
                case EdmPrimitiveTypeKind.Decimal:
                    items = items.Where(x => Convert.ToDecimal(GetPropertyValue(x, leftAttributeName, leftAttributeValue)) <= Convert.ToDecimal(GetPropertyValue(x, rightAttributeName, rightAttributeValue)));
                    break;
                case EdmPrimitiveTypeKind.DateTimeOffset:
                    items = items.Where((x) =>
                    {
                        var leftAttribute = Convert.ToDateTime(GetPropertyValue(x, leftAttributeName, leftAttributeValue).ToString());
                        var rightAttribute = Convert.ToDateTime(GetPropertyValue(x, rightAttributeName, rightAttributeValue).ToString());
                        return leftAttribute <= rightAttribute;
                    }
                    );
                    break;

            }

            return items;
        }

        private IEnumerable<IEdmEntityObject> DateEquals(IEnumerable<IEdmEntityObject> items, string leftAttributeName, string rightAttributeName, object leftAttributeValue, object rightAttributeValue, EdmPrimitiveTypeKind leftAttributeType, SingleValueNode left)
        {
            var typename = "";
            if (left.GetType().Name == "SingleValueFunctionCallNode")
            {
                typename = ((SingleValueFunctionCallNode)left).Name.ToString();
            }
            else
            {
                typename = "default";
            }
            switch (leftAttributeType)
            {
                case EdmPrimitiveTypeKind.DateTimeOffset:
                    if (typename == "year")
                    {
                        items = items.Where((x) =>
                        {
                            var leftAttribute = Convert.ToDateTime(GetPropertyValue(x, leftAttributeName, leftAttributeValue));
                            var rightAttribute = GetPropertyValue(x, rightAttributeName, rightAttributeValue);
                            return string.Equals(leftAttribute.Year.ToString(), rightAttribute.ToString());
                        });
                    }
                    else if (typename == "month")
                    {
                        items = items.Where((x) =>
                        {
                            var leftAttribute = Convert.ToDateTime(GetPropertyValue(x, leftAttributeName, leftAttributeValue));
                            var rightAttribute = GetPropertyValue(x, rightAttributeName, rightAttributeValue);
                            return string.Equals(leftAttribute.Month.ToString(), rightAttribute.ToString());
                        });
                    }
                    else if (typename == "day")
                    {
                        items = items.Where((x) =>
                        {
                            var leftAttribute = Convert.ToDateTime(GetPropertyValue(x, leftAttributeName, leftAttributeValue));
                            var rightAttribute = GetPropertyValue(x, rightAttributeName, rightAttributeValue);
                            return string.Equals(leftAttribute.Day.ToString(), rightAttribute.ToString());
                        });
                    }
                    else if (typename == "default")
                    {
                        items = items.Where((x) =>
                        {
                            var leftAttribute = Convert.ToDateTime(GetPropertyValue(x, leftAttributeName, leftAttributeValue).ToString(), new CultureInfo("en-US"));
                            var rightAttribute = Convert.ToDateTime(GetPropertyValue(x, rightAttributeName, rightAttributeValue).ToString(), new CultureInfo("en-US"));
                            return string.Equals(leftAttribute.AddMilliseconds(-leftAttribute.Millisecond), rightAttribute.AddMilliseconds(-rightAttribute.Millisecond));
                        });
                    }
                    break;

            }

            return items;
        }

        private IEnumerable<IEdmEntityObject> DateNotEquals(IEnumerable<IEdmEntityObject> items, string leftAttributeName, string rightAttributeName, object leftAttributeValue, object rightAttributeValue, EdmPrimitiveTypeKind leftAttributeType, SingleValueNode left)
        {
            var typename = "";
            if (left.GetType().Name == "SingleValueFunctionCallNode")
            {
                typename = ((SingleValueFunctionCallNode)left).Name.ToString();
            }
            else
            {
                typename = "default";
            }
            switch (leftAttributeType)
            {
                case EdmPrimitiveTypeKind.DateTimeOffset:
                    if (typename == "year")
                    {
                        items = items.Where((x) =>
                        {
                            var leftAttribute = Convert.ToDateTime(GetPropertyValue(x, leftAttributeName, leftAttributeValue));
                            var rightAttribute = GetPropertyValue(x, rightAttributeName, rightAttributeValue);
                            return !string.Equals(leftAttribute.Year.ToString(), rightAttribute.ToString());
                        });
                    }
                    else if (typename == "month")
                    {
                        items = items.Where((x) =>
                        {
                            var leftAttribute = Convert.ToDateTime(GetPropertyValue(x, leftAttributeName, leftAttributeValue));
                            var rightAttribute = GetPropertyValue(x, rightAttributeName, rightAttributeValue);
                            return !string.Equals(leftAttribute.Month.ToString(), rightAttribute.ToString());
                        });
                    }
                    else if (typename == "day")
                    {
                        items = items.Where((x) =>
                        {
                            var leftAttribute = Convert.ToDateTime(GetPropertyValue(x, leftAttributeName, leftAttributeValue));
                            var rightAttribute = GetPropertyValue(x, rightAttributeName, rightAttributeValue);
                            return !string.Equals(leftAttribute.Day.ToString(), rightAttribute.ToString());
                        });
                    }
                    else if (typename == "default")
                    {
                        items = items.Where((x) =>
                        {
                            var leftAttribute = Convert.ToDateTime(GetPropertyValue(x, leftAttributeName, leftAttributeValue).ToString(), new CultureInfo("en-US"));
                            var rightAttribute = Convert.ToDateTime(GetPropertyValue(x, rightAttributeName, rightAttributeValue).ToString(), new CultureInfo("en-US"));
                            return !string.Equals(leftAttribute.AddMilliseconds(-leftAttribute.Millisecond), rightAttribute.AddMilliseconds(-rightAttribute.Millisecond));

                        });
                    }
                    break;

            }

            return items;
        }

    }
}
