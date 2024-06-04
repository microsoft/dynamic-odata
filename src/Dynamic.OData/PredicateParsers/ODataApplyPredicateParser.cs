// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Dynamic.OData.Exceptions;
using Dynamic.OData.Models;
using Dynamic.OData.PredicateParsers.Interface;
using Microsoft.AspNetCore.OData.Formatter.Value;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Microsoft.OData.UriParser.Aggregation;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Dynamic.OData.PredicateParsers
{
    /// <summary>
    /// Apply clause parser and data generator. If this clause is present in query, it's always evaluated first.
    /// The error behavior is boolean. Either we parse the query completely, or not at all. There is no fallback and this is intentional.
    /// </summary>
    public class ODataApplyPredicateParser : BaseODataPredicateParser, IODataPredicateParser
    {
        private const string ApplyParser = "Apply";
        private const int StepIndex = 1;
        private ParseContext SourceParseContext;
        public ParseContext Parse(ODataUriParser parser, ParseContext sourceParseContext)
        {
            SourceParseContext = sourceParseContext;
            var targetParseContext = new ParseContext();
            var targetQueryableSourceEntities = new List<Dictionary<string, object>>();
            var sourceEdmSetting = sourceParseContext.EdmEntityTypeSettings.FirstOrDefault();
            var targetEdmSetting = new EdmEntityTypeSettings()
            {
                RouteName = "Groups",
                Personas = sourceEdmSetting.Personas,
                Properties = new List<EdmEntityTypePropertySetting>()
            };
            var latestStateDictionary = new Dictionary<string, object>();

            var edmEntityType = new EdmEntityType(EdmNamespaceName, "Groups");
            //This may only be used if we client uses Custom.List as aggregation
            var edmComplexType = new EdmComplexType(EdmNamespaceName, "List");
            var aggregatePropList = new Dictionary<string, AggregateExpression>();
            var applyClause = parser.ParseApply();

            //We support only single transformation
            if (applyClause.Transformations.Count() > 1)
                throw new FeatureNotSupportedException(ApplyParser, "Multiple Transformations");

            if (applyClause.Transformations.Count() == 0)
                throw new FeatureNotSupportedException(ApplyParser, "Zero Transformations");

            foreach (var transformation in applyClause.Transformations)
            {
                if (transformation.Kind == TransformationNodeKind.GroupBy)
                {
                    var transform = (GroupByTransformationNode)transformation;

                    ///Add all the grouping properties
                    foreach (var groupingProperty in transform.GroupingProperties)
                    {
                        var sourceProperty = sourceEdmSetting.Properties.FirstOrDefault(predicate => predicate.PropertyName.Equals(groupingProperty.Name));

                        edmEntityType.AddStructuralProperty(groupingProperty.Name, groupingProperty.TypeReference.PrimitiveKind());

                        targetEdmSetting.Properties.Add(new EdmEntityTypePropertySetting
                        {
                            PropertyName = sourceProperty.PropertyName,
                            PropertyType = sourceProperty.PropertyType,
                            IsNullable = sourceProperty.IsNullable
                        });
                    }


                    //Add all the aggregate properties
                    if (transform.ChildTransformations != null)
                    {
                        var aggregationProperties = (AggregateTransformationNode)transform.ChildTransformations;
                        AddAggregationPropertiesToModel(aggregationProperties
                            , sourceEdmSetting, edmEntityType, aggregatePropList, targetEdmSetting
                            , edmComplexType, latestStateDictionary);
                    }

                    //Register these dynamic types to model
                    sourceParseContext.Model.AddElement(edmEntityType);
                    sourceParseContext.Model.AddElement(edmComplexType);
                    ((EdmEntityContainer)sourceParseContext.Model.EntityContainer).AddEntitySet("Groups", edmEntityType);

                    var fields = transform.GroupingProperties.Select(p => p.Name).ToList();
                    var groups = sourceParseContext.QueryableSourceEntities
                       .GroupBy(r => fields.ToDictionary(c => c, c => r[c]), new CustomEqualityComparer());

                    var entityRef = new EdmEntityTypeReference(edmEntityType, true);
                    var collectionRef = new EdmCollectionTypeReference(new EdmCollectionType(entityRef));
                    var collection = new EdmEntityObjectCollection(collectionRef);
                    latestStateDictionary.Add(RequestFilterConstants.GetEntityTypeKeyName(ApplyParser, StepIndex), entityRef);
                    foreach (var group in groups)
                    {
                        var targetQueryableDictionary = new Dictionary<string, object>();
                        var obj = new EdmEntityObject(edmEntityType);
                        foreach (var prop in fields)
                        {
                            var value = group.Key[prop];
                            obj.TrySetPropertyValue(prop, value);
                            targetQueryableDictionary.Add(prop, value);
                        }
                        AddAggregationPropertyValuesToModel(targetQueryableDictionary, obj, group, edmComplexType, aggregatePropList);
                        collection.Add(obj);
                        targetQueryableSourceEntities.Add(targetQueryableDictionary);
                    }
                    targetParseContext.Result = collection;
                    targetParseContext.Model = sourceParseContext.Model;
                    targetParseContext.QueryableSourceEntities = targetQueryableSourceEntities;
                    targetParseContext.EdmEntityTypeSettings = new List<EdmEntityTypeSettings> { targetEdmSetting };
                    targetParseContext.LatestStateDictionary = latestStateDictionary;
                    return targetParseContext;
                }
                else if (transformation.Kind == TransformationNodeKind.Aggregate)
                {
                    var targetQueryableDictionary = new Dictionary<string, object>();
                    var obj = new EdmEntityObject(edmEntityType);
                    var aggregationProperties = (AggregateTransformationNode)transformation;
                    AddAggregationPropertiesToModel(aggregationProperties
                            , sourceEdmSetting, edmEntityType, aggregatePropList, targetEdmSetting
                            , edmComplexType, latestStateDictionary);
                    //Register these dynamic types to model
                    sourceParseContext.Model.AddElement(edmEntityType);
                    sourceParseContext.Model.AddElement(edmComplexType);
                    var entityRef = new EdmEntityTypeReference(edmEntityType, true);
                    var collectionRef = new EdmCollectionTypeReference(new EdmCollectionType(entityRef));
                    var collection = new EdmEntityObjectCollection(collectionRef);
                    latestStateDictionary.Add(RequestFilterConstants.GetEntityTypeKeyName(ApplyParser, StepIndex), entityRef);
                    AddAggregationPropertyValuesToModel(targetQueryableDictionary, obj, sourceParseContext.QueryableSourceEntities, edmComplexType, aggregatePropList);
                    collection.Add(obj);
                    targetQueryableSourceEntities.Add(targetQueryableDictionary);
                    targetParseContext.Result = collection;
                    targetParseContext.Model = sourceParseContext.Model;
                    targetParseContext.QueryableSourceEntities = targetQueryableSourceEntities;
                    targetParseContext.EdmEntityTypeSettings = new List<EdmEntityTypeSettings> { targetEdmSetting };
                    targetParseContext.LatestStateDictionary = latestStateDictionary;
                    return targetParseContext;
                }
                else
                {
                    throw new FeatureNotSupportedException(ApplyParser, $"Transformation Kind {transformation.Kind} is not supported");
                }
            }
            throw new FeatureNotSupportedException(ApplyParser, "Invalid Apply Clause");
        }

        private void AddAggregationPropertyValuesToModel(Dictionary<string, object> targetQueryableDictionary
            , EdmEntityObject obj
            , IEnumerable<Dictionary<string, object>> group
            , EdmComplexType edmComplexType
            , Dictionary<string, AggregateExpression> aggregatePropList)
        {
            foreach (var property in aggregatePropList)
            {
                var val = aggregatePropList[property.Key];
                var primitiveKind = val.Expression.TypeReference.PrimitiveKind();
                if (val.Method != AggregationMethod.Custom)
                {
                    var sourcePropertyName = val.Method != AggregationMethod.VirtualPropertyCount ?
                            ((SingleValuePropertyAccessNode)val.Expression).Property.Name : null;
                    var value = GetAggregatedValue(sourcePropertyName, val, group, primitiveKind);
                    obj.TrySetPropertyValue(val.Alias, value);
                    targetQueryableDictionary.Add(val.Alias, value);
                }
                else
                {
                    object value;
                    if (val.MethodDefinition.MethodLabel.Contains(ODataFilterConstants.AggregationMethod_Custom_List, StringComparison.OrdinalIgnoreCase))
                    {
                        value = GetAggregatedValue(property.Key, val, group, EdmPrimitiveTypeKind.None, edmComplexType);
                        var subcollectionContext = (SubCollectionContext)value;
                        obj.TrySetPropertyValue(property.Key, subcollectionContext.Result);
                        targetQueryableDictionary.Add(property.Key, subcollectionContext.QueryAbleResult);
                    }
                    else if (val.MethodDefinition.MethodLabel.Contains(ODataFilterConstants.AggregationMethod_Custom_CountDistinct, StringComparison.OrdinalIgnoreCase)
                          || val.MethodDefinition.MethodLabel.Contains(ODataFilterConstants.AggregationMethod_Custom_Count, StringComparison.OrdinalIgnoreCase))
                    {
                        var sourcePropertyName = ((SingleValuePropertyAccessNode)val.Expression).Property.Name;
                        value = GetAggregatedValue(sourcePropertyName, val, group, EdmPrimitiveTypeKind.None);
                        obj.TrySetPropertyValue(val.Alias, value);
                        targetQueryableDictionary.Add(val.Alias, value);
                    }
                }
            }
        }

        private void AddAggregationPropertiesToModel(AggregateTransformationNode aggregationProperties
            , EdmEntityTypeSettings sourceEdmSetting
            , EdmEntityType edmEntityType
            , Dictionary<string, AggregateExpression> aggregatePropList
            , EdmEntityTypeSettings targetEdmSetting
            , EdmComplexType edmComplexType
            , Dictionary<string, object> latestStateDictionary)
        {
            foreach (var aggregationExpression in aggregationProperties.AggregateExpressions)
            {
                var expr = (AggregateExpression)aggregationExpression;
                if (expr.Method != AggregationMethod.Custom)
                {
                    bool? isNullable = null;
                    string propertyAlias = "";
                    var primitiveKind = expr.Expression.TypeReference.PrimitiveKind();
                    if (expr.Method == AggregationMethod.VirtualPropertyCount)
                    {
                        var sourceProperty = (CountVirtualPropertyNode)expr.Expression;
                        isNullable = sourceProperty.TypeReference.IsNullable;
                        propertyAlias = !string.IsNullOrWhiteSpace(expr.Alias) ? expr.Alias : sourceProperty.Kind.ToString();
                        primitiveKind = EdmPrimitiveTypeKind.Int32;
                    }
                    else
                    {
                        var sourceProperty = (SingleValuePropertyAccessNode)expr.Expression;
                        var sourceEdmProperty = sourceEdmSetting.Properties.FirstOrDefault(predicate => predicate.PropertyName.Equals(sourceProperty.Property.Name));
                        isNullable = sourceEdmProperty.IsNullable;
                        propertyAlias = !string.IsNullOrWhiteSpace(expr.Alias) ? expr.Alias : sourceProperty.Property.Name;
                        if (expr.Method == AggregationMethod.Average)
                            primitiveKind = EdmPrimitiveTypeKind.Double;
                        if (expr.Method == AggregationMethod.CountDistinct)
                            primitiveKind = EdmPrimitiveTypeKind.Int32;
                    }
                    edmEntityType.AddStructuralProperty(propertyAlias, primitiveKind);
                    aggregatePropList.Add(propertyAlias, expr);
                    targetEdmSetting.Properties.Add(new EdmEntityTypePropertySetting
                    {
                        PropertyName = propertyAlias,
                        PropertyType = GetStringTypeFromEdmPrimitiveType(primitiveKind),
                        IsNullable = isNullable
                    });
                }
                else
                {
                    //Create a list of source type
                    if (expr.MethodDefinition.MethodLabel.Contains(ODataFilterConstants.AggregationMethod_Custom_List, StringComparison.OrdinalIgnoreCase))
                    {

                        foreach (var property in sourceEdmSetting.Properties)
                        {
                            edmComplexType.AddStructuralProperty(property.PropertyName, property.GetEdmPrimitiveTypeKind());
                        }
                        var groupItemsPropertyName = !string.IsNullOrWhiteSpace(expr.Alias) ? expr.Alias : "Items";
                        var complexTypeReference = new EdmComplexTypeReference(edmComplexType, true);
                        edmEntityType.AddStructuralProperty(groupItemsPropertyName, new EdmCollectionTypeReference(new EdmCollectionType(complexTypeReference)));
                        aggregatePropList.Add(groupItemsPropertyName, expr);
                        targetEdmSetting.Properties.Add(new EdmEntityTypePropertySetting
                        {
                            PropertyName = groupItemsPropertyName,
                            PropertyType = "List",
                            IsNullable = null
                        });
                        latestStateDictionary.Add(RequestFilterConstants.GetComplexTypeKeyName(ApplyParser, StepIndex), edmComplexType);

                    }
                    else if (expr.MethodDefinition.MethodLabel.Contains(ODataFilterConstants.AggregationMethod_Custom_CountDistinct, StringComparison.OrdinalIgnoreCase)
                           || expr.MethodDefinition.MethodLabel.Contains(ODataFilterConstants.AggregationMethod_Custom_Count, StringComparison.OrdinalIgnoreCase))
                    {
                        var sourceProperty = (SingleValuePropertyAccessNode)expr.Expression;
                        var primitiveKind = EdmPrimitiveTypeKind.Int32;
                        var countDistinctPropName = !string.IsNullOrWhiteSpace(expr.Alias) ? expr.Alias : sourceProperty.Property.Name;
                        edmEntityType.AddStructuralProperty(countDistinctPropName, primitiveKind);
                        aggregatePropList.Add(countDistinctPropName, expr);
                        targetEdmSetting.Properties.Add(new EdmEntityTypePropertySetting
                        {
                            PropertyName = countDistinctPropName,
                            PropertyType = GetStringTypeFromEdmPrimitiveType(primitiveKind),
                            IsNullable = null
                        });
                    }
                    else
                    {
                        throw new FeatureNotSupportedException($"{ApplyParser}-Custom Aggregation-{expr.MethodDefinition.MethodLabel}", "Invalid Custom Aggregation");
                    }
                }
            }
        }


        private object GetAggregatedValue(string key, AggregateExpression expression, IEnumerable<Dictionary<string, object>> group, EdmPrimitiveTypeKind edmPrimitiveType = EdmPrimitiveTypeKind.None, EdmComplexType edmComplexType = null)
        {
            var clrType = GetCLRTypeFromEdmType(edmPrimitiveType);
            var method = expression.Method;
            switch (method)
            {
                case AggregationMethod.Max: return Convert.ChangeType(group.Max(r => r.GetValueOrDefault(key)), clrType);
                case AggregationMethod.Min: return Convert.ChangeType(group.Min(r => r.GetValueOrDefault(key)), clrType);
                case AggregationMethod.Average:
                    switch (edmPrimitiveType)
                    {
                        case EdmPrimitiveTypeKind.Double: return group.Average(r => (double)r.GetValueOrDefault(key));
                        case EdmPrimitiveTypeKind.Int16: return group.Average(r => (short)r.GetValueOrDefault(key));
                        case EdmPrimitiveTypeKind.Int32: return group.Average(r => (int)r.GetValueOrDefault(key));
                        case EdmPrimitiveTypeKind.Int64: return group.Average(r => (long)r.GetValueOrDefault(key));
                        case EdmPrimitiveTypeKind.Decimal: return group.Average(r => (decimal)r.GetValueOrDefault(key));
                        default: return group.Average(r => (int)r.GetValueOrDefault(key));
                    }
                case AggregationMethod.CountDistinct: return group.Select(r => r.GetValueOrDefault(key)).Distinct().Count();
                case AggregationMethod.VirtualPropertyCount: return group.Select(r => r).Count();
                case AggregationMethod.Sum:
                    switch (edmPrimitiveType)
                    {
                        case EdmPrimitiveTypeKind.Double: return group.Sum(r => (double)r.GetValueOrDefault(key));
                        case EdmPrimitiveTypeKind.Int16: return group.Sum(r => (short)r.GetValueOrDefault(key));
                        case EdmPrimitiveTypeKind.Int32: return group.Sum(r => (int)r.GetValueOrDefault(key));
                        case EdmPrimitiveTypeKind.Int64: return group.Sum(r => (long)r.GetValueOrDefault(key));
                        case EdmPrimitiveTypeKind.Decimal: return group.Sum(r => (decimal)r.GetValueOrDefault(key));
                        default: return Convert.ChangeType(group.Sum(r => (int)r.GetValueOrDefault(key)), clrType);
                    }
                case AggregationMethod.Custom: return ProcessCustomAggregationValue(expression.MethodDefinition.MethodLabel, group, edmComplexType, key);
            }
            return null;
        }

        private object ProcessCustomAggregationValue(string customAggregationMethod, IEnumerable<Dictionary<string, object>> group, EdmComplexType edmComplexType, string key)
        {
            var customMethodName = ParseMethodName(customAggregationMethod);
            switch (customMethodName.Name)
            {
                case ODataFilterConstants.AggregationMethod_Custom_List: return GetList(group, edmComplexType, customMethodName.Count);
                case ODataFilterConstants.AggregationMethod_Custom_Count: return GetFilteredResult(group, customMethodName.FilterQuery, SourceParseContext).Count();
                case ODataFilterConstants.AggregationMethod_Custom_CountDistinct: return GetFilteredResult(group, customMethodName.FilterQuery, SourceParseContext).Select(p => GetPropertyValue(p, key)).Distinct().Count(); ;
            }
            return null;
        }

        /// <summary>
        /// Gets CLR Primitive Type from an EDM Primitive Type
        /// </summary>
        /// <param name="edmPrimitiveType"></param>
        /// <returns></returns>
        private Type GetCLRTypeFromEdmType(EdmPrimitiveTypeKind edmPrimitiveType)
        {
            switch (edmPrimitiveType)
            {
                case EdmPrimitiveTypeKind.DateTimeOffset: return typeof(DateTime);
                case EdmPrimitiveTypeKind.Date: return typeof(Date);
                case EdmPrimitiveTypeKind.TimeOfDay: return typeof(TimeOfDay);
                case EdmPrimitiveTypeKind.Int16: return typeof(short);
                case EdmPrimitiveTypeKind.Int32: return typeof(int);
                case EdmPrimitiveTypeKind.Int64: return typeof(long);
                case EdmPrimitiveTypeKind.Double: return typeof(double);
                case EdmPrimitiveTypeKind.Boolean: return typeof(bool);
                case EdmPrimitiveTypeKind.Guid: return typeof(Guid);
                case EdmPrimitiveTypeKind.Decimal: return typeof(decimal);
                default: return typeof(string);
            }
        }
        private CustomMethodName ParseMethodName(string methodName)
        {
            CustomMethodName cus = new CustomMethodName();
            var name = methodName.Split('_');
            cus.Name = name[0];
            if (name.Length > 1)
            {
                bool isInt = int.TryParse(name[1], out int value);
                if (isInt)
                    cus.Count = value;
                else
                    cus.FilterQuery = name[1];
            }
            return cus;
        }
    }

    class CustomMethodName
    {
        public string Name { get; set; }
        public int Count { get; set; }
        public string FilterQuery { get; set; }
    }

    public class SubCollectionContext
    {
        public object Result { get; set; }
        public List<Dictionary<string, object>> QueryAbleResult { get; set; }
    }
}
