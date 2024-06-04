// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Dynamic.OData.Exceptions;
using Dynamic.OData.Models;
using Dynamic.OData.PredicateParsers.Interface;
using Microsoft.AspNetCore.OData.Formatter.Value;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Dynamic.OData.PredicateParsers
{
    /// <summary>
    /// Select clause parser and data generator. If this clause is present in query, it's always 2nd
    /// The error behavior is boolean. Either we parse the query completely, or not at all. There is no fallback and this is intentional.
    /// </summary>
    public class ODataSelectPredicateParser : BaseODataPredicateParser, IODataPredicateParser
    {
        private const string SelectParser = "Select";
        private const int StepIndex = 10;
        public ParseContext Parse(ODataUriParser parser, ParseContext sourceParseContext)
        {
            //Select implementation
            var targetParseContext = new ParseContext();
            var targetQueryableSourceEntities = new List<Dictionary<string, object>>();
            var sourceEdmSetting = sourceParseContext.EdmEntityTypeSettings.FirstOrDefault();
            var targetEdmSetting = new EdmEntityTypeSettings()
            {
                RouteName = SelectParser,
                Personas = sourceEdmSetting.Personas,
                Properties = new List<EdmEntityTypePropertySetting>()
            };
            var selectExpandClause = parser.ParseSelectAndExpand();
            var edmEntityType = new EdmEntityType(EdmNamespaceName, SelectParser);
            var latestStateDictionary = new Dictionary<string, object>();

            //Construct the types. For now we support non-nested primitive types only. Everything else is an exception for now.
            var propertyList = new List<string>();
            foreach (var item in selectExpandClause.SelectedItems)
            {
                switch (item)
                {
                    case PathSelectItem pathSelectItem:
                        IEnumerable<ODataPathSegment> segments = pathSelectItem.SelectedPath;
                        var firstPropertySegment = segments.FirstOrDefault();
                        if (firstPropertySegment != null)
                        {
                            var typeSetting = sourceEdmSetting.Properties.FirstOrDefault(predicate => predicate.PropertyName == firstPropertySegment.Identifier);

                            propertyList.Add(firstPropertySegment.Identifier);

                            if (typeSetting.GetEdmPrimitiveTypeKind() != EdmPrimitiveTypeKind.None)
                            {
                                var edmPrimitiveType = typeSetting.GetEdmPrimitiveTypeKind();
                                if (typeSetting.IsNullable.HasValue)
                                    edmEntityType.AddStructuralProperty(firstPropertySegment.Identifier, edmPrimitiveType, typeSetting.IsNullable.Value);
                                else
                                    edmEntityType.AddStructuralProperty(firstPropertySegment.Identifier, edmPrimitiveType);
                                targetEdmSetting.Properties.Add(new EdmEntityTypePropertySetting
                                {
                                    PropertyName = typeSetting.PropertyName,
                                    PropertyType = typeSetting.PropertyType,
                                    IsNullable = typeSetting.IsNullable
                                });
                            }
                            else
                            {
                                //We are doing $select on a property which is of type list. Which means
                                if (typeSetting.PropertyType == "List")
                                {
                                    var edmComplexType = GetEdmComplexTypeReference(sourceParseContext);
                                    edmEntityType.AddStructuralProperty(typeSetting.PropertyName, new EdmCollectionTypeReference(new EdmCollectionType(new EdmComplexTypeReference(edmComplexType, true))));
                                    targetEdmSetting.Properties.Add(new EdmEntityTypePropertySetting
                                    {
                                        PropertyName = typeSetting.PropertyName,
                                        PropertyType = typeSetting.PropertyType,
                                        IsNullable = typeSetting.IsNullable
                                    });
                                }
                                else
                                {
                                    throw new FeatureNotSupportedException(SelectParser, $"Invalid Custom Selection-{typeSetting.PropertyName}-{typeSetting.PropertyType}");
                                }
                            }
                        }
                        else
                        {
                            throw new FeatureNotSupportedException(SelectParser, "Empty Path Segments");
                        }
                        break;
                    case WildcardSelectItem wildcardSelectItem: throw new FeatureNotSupportedException(SelectParser, "WildcardSelect");
                    case ExpandedNavigationSelectItem expandedNavigationSelectItem: throw new FeatureNotSupportedException(SelectParser, "ExpandedNavigation");
                    case ExpandedReferenceSelectItem expandedReferenceSelectItem: throw new FeatureNotSupportedException(SelectParser, "ExpandedReference");
                    case NamespaceQualifiedWildcardSelectItem namespaceQualifiedWildcardSelectItem: throw new FeatureNotSupportedException(SelectParser, "NamespaceQualifiedWildcard");
                }
            }

            //Register these dynamic types to model
            sourceParseContext.Model.AddElement(edmEntityType);
            ((EdmEntityContainer)sourceParseContext.Model.EntityContainer).AddEntitySet("Select", edmEntityType);


            //Construct the data
            var entityReferenceType = new EdmEntityTypeReference(edmEntityType, true);
            var collectionRef = new EdmCollectionTypeReference(new EdmCollectionType(entityReferenceType));
            var collection = new EdmEntityObjectCollection(collectionRef);
            var filteredQueryableEntityList = sourceParseContext.QueryableSourceEntities.Select(p => p.Where(p => propertyList.Contains(p.Key)));
            latestStateDictionary.Add(RequestFilterConstants.GetEntityTypeKeyName(SelectParser, StepIndex), entityReferenceType);

            foreach (var entity in filteredQueryableEntityList)
            {
                var entityDictionary = new Dictionary<string, object>();
                var obj = new EdmEntityObject(edmEntityType);
                foreach (var propertyKey in propertyList)
                {
                    var setting = targetEdmSetting.Properties.FirstOrDefault(predicate => predicate.PropertyName.Equals(propertyKey));
                    var data = entity.FirstOrDefault(property => property.Key.Equals(propertyKey));

                    //This condition is when the type of selected property is a primitive type
                    if (setting.GetEdmPrimitiveTypeKind() != EdmPrimitiveTypeKind.None)
                    {
                        var propertyValue = !data.Equals(default(KeyValuePair<string, object>)) ? data.Value : null;
                        obj.TrySetPropertyValue(propertyKey, propertyValue);
                        entityDictionary.Add(propertyKey, propertyValue);
                    }
                    else
                    {
                        switch (setting.PropertyType)
                        {
                            case "List":
                                //TODO: There is scope for perf improvement
                                //We can re-use the previous constructed list instead of constructing one from scratch.
                                var subList = (List<Dictionary<string, object>>)data.Value;
                                var subListContext = GetList(subList, GetEdmComplexTypeReference(sourceParseContext));
                                obj.TrySetPropertyValue(propertyKey, subListContext.Result);
                                entityDictionary.Add(propertyKey, subListContext.QueryAbleResult);
                                break;
                        }
                    }

                }
                collection.Add(obj);
                targetQueryableSourceEntities.Add(entityDictionary);
            }

            targetParseContext.Result = collection;
            targetParseContext.QueryableSourceEntities = targetQueryableSourceEntities;
            targetParseContext.Model = sourceParseContext.Model;
            targetParseContext.EdmEntityTypeSettings = new List<EdmEntityTypeSettings> { targetEdmSetting };
            targetParseContext.LatestStateDictionary = latestStateDictionary;
            return targetParseContext;
        }

        private EdmComplexType GetEdmComplexTypeReference(ParseContext parseContext)
        {
            var complexTypeKey = parseContext.LatestStateDictionary.Keys.FirstOrDefault(predicate => predicate.Contains("complexentitytype"));
            var edmComplexTypeRef = (EdmComplexType)parseContext.LatestStateDictionary[complexTypeKey];
            return edmComplexTypeRef;
        }
    }
}
