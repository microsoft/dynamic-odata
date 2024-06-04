// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Dynamic.OData.Exceptions;
using Dynamic.OData.Helpers.Interface;
using Dynamic.OData.Interface;
using Dynamic.OData.Models;
using Dynamic.OData.PredicateParsers.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Formatter.Value;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Query.Validator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dynamic.OData
{
    public class ODataFilterManager : IODataFilterManager
    {
        private List<EdmEntityTypeSettings> _settings;
        private readonly IODataRequestHelper _oDataRequestHelper;
        private readonly IODataQueryValidator _oDataQueryValidator;
        private readonly IODataPredicateParser _odataApplyPredicateParser;
        private readonly IODataPredicateParser _odataSelectPredicateParser;
        private readonly IODataPredicateParser _odataTopPredicateParser;
        private readonly IODataPredicateParser _odataSkipPredicateParser;
        private readonly IODataPredicateParser _odataOrderByPredicateParser;
        private readonly IODataPredicateParser _odataFilterPredicateParser;

        public ODataFilterManager(IODataRequestHelper oDataRequestHelper
            , IODataQueryValidator oDataQueryValidator
            , IODataPredicateParser odataApplyPredicateParser
            , IODataPredicateParser odataSelectPredicateParser
            , IODataPredicateParser odataTopPredicateParser
            , IODataPredicateParser odataSkipPredicateParser
            , IODataPredicateParser odataOrderByPredicateParser
            , IODataPredicateParser odataFilterPredicateParser)
        {
            _oDataRequestHelper = oDataRequestHelper ?? throw new ArgumentNullException(nameof(oDataRequestHelper));
            _oDataQueryValidator = oDataQueryValidator ?? throw new ArgumentNullException(nameof(oDataQueryValidator));
            _odataApplyPredicateParser = odataApplyPredicateParser ?? throw new ArgumentNullException(nameof(odataApplyPredicateParser));
            _odataSelectPredicateParser = odataSelectPredicateParser ?? throw new ArgumentNullException(nameof(odataSelectPredicateParser));
            _odataTopPredicateParser = odataTopPredicateParser ?? throw new ArgumentNullException(nameof(odataTopPredicateParser));
            _odataSkipPredicateParser = odataSkipPredicateParser ?? throw new ArgumentNullException(nameof(odataSkipPredicateParser));
            _odataOrderByPredicateParser = odataOrderByPredicateParser ?? throw new ArgumentNullException(nameof(odataOrderByPredicateParser));
            _odataFilterPredicateParser = odataFilterPredicateParser ?? throw new ArgumentNullException(nameof(odataFilterPredicateParser));
        }

        private ODataQueryOptions GetODataQueryOptions(HttpRequest httpRequest)
        {
            var path = _oDataRequestHelper.GetODataPath(httpRequest);
            IEdmEntityTypeReference entityType = _oDataRequestHelper.GetEdmEntityTypeReference(httpRequest);
            var model = _oDataRequestHelper.GetEdmModel(httpRequest);
            var queryOptions = new ODataQueryOptions(new ODataQueryContext(model, entityType.Definition, path), httpRequest);
            return queryOptions;
        }

        /// <summary>
        /// Applies OData Filters in the following order
        /// 1. Apply
        /// 2. Compute
        /// 3. Search
        /// 4. Filter
        /// 5. Count
        /// 6. OrderBy
        /// 7. Skip
        /// 8. Top
        /// 9. Expand
        /// 10. Select
        /// 11. Format
        /// </summary>
        /// <param name="sourceEntities">The base Source Entities</param>
        /// <param name="queryableSourceEntities">The base source entities in a queryable form</param>
        /// <param name="request">The Http Request</param>
        /// <returns></returns>
        public IEnumerable<IEdmEntityObject> ApplyFilter(IEnumerable<IEdmEntityObject> sourceEntities, IEnumerable<Dictionary<string, object>> queryableSourceEntities, HttpRequest request)
        {
            _settings = _oDataRequestHelper.GetEdmEntityTypeSettings(request);
            var model = _oDataRequestHelper.GetEdmModel(request);
            // Escape nested single quote
            request.QueryString = _oDataRequestHelper.EscapeQueryString(request.QueryString);
            var queryOptions = GetODataQueryOptions(request);
            string baseRouteName = _oDataRequestHelper.GetRouteName(request);

            //Append the dynamic route name
            if (!string.IsNullOrWhiteSpace(queryOptions.RawValues.Select))
                baseRouteName = $"{baseRouteName}({queryOptions.RawValues.Select})";
            request.HttpContext.Items.Add(RequestFilterConstants.ContextSuffixKey, baseRouteName);

            //We simply return the source collection if it's null or has zero records. Or if we are unable to map settings / query options.
            if (queryOptions == null || sourceEntities == null || _settings == null)
                return sourceEntities;
            if (_settings.Count != 1)
                return sourceEntities;
            if (sourceEntities.Count() == 0)
                return sourceEntities;

            ODataValidationSettings validateSettings = request.HttpContext.RequestServices.GetService<ODataValidationSettings>();
            if (validateSettings == null)
            {
                // if no setting, use the default one
                validateSettings = new ODataValidationSettings
                {
                    AllowedQueryOptions = AllowedQueryOptions.All,
                    AllowedLogicalOperators = AllowedLogicalOperators.All,
                    AllowedArithmeticOperators = AllowedArithmeticOperators.All,
                    AllowedFunctions = AllowedFunctions.AllFunctions,
                };
            }

            //Validate the query from OData perspective.
            _oDataQueryValidator.Validate(queryOptions, validateSettings);

            var serviceRoot = new Uri(RequestFilterConstants.ODataServiceRoot);
            var parser = new ODataUriParser(model, serviceRoot, _oDataRequestHelper.GetODataRelativeUri(request));
            var latestStateDictionary = new Dictionary<string, object>();
            latestStateDictionary.Add(RequestFilterConstants.GetEntityTypeKeyName("base", 0), _oDataRequestHelper.GetEdmEntityTypeReference(request));
            //Initial Context
            var parseContext = new ParseContext
            {
                Model = model,
                Result = sourceEntities,
                LatestStateDictionary = latestStateDictionary,
                EdmEntityTypeSettings = _settings,
                QueryableSourceEntities = queryableSourceEntities
            };

            //The clauses are ordered as per OData Spec.
            if (!string.IsNullOrWhiteSpace(queryOptions.RawValues.Apply))
            {
                parseContext = _odataApplyPredicateParser.Parse(parser, parseContext);
            }

            if (!string.IsNullOrWhiteSpace(queryOptions.RawValues.Filter))
                parseContext = _odataFilterPredicateParser.Parse(parser, parseContext);

            if (!string.IsNullOrWhiteSpace(queryOptions.RawValues.Count))
                _oDataRequestHelper.SetRequestCount(parser, request, parseContext.Result.Count());

            if (!string.IsNullOrWhiteSpace(queryOptions.RawValues.OrderBy))
            {
                parseContext = _odataOrderByPredicateParser.Parse(parser, parseContext);
            }

            if (!string.IsNullOrWhiteSpace(queryOptions.RawValues.Skip))
            {
                parseContext = _odataSkipPredicateParser.Parse(parser, parseContext);
            }

            if (!string.IsNullOrWhiteSpace(queryOptions.RawValues.Top))
            {
                parseContext = _odataTopPredicateParser.Parse(parser, parseContext);
            }

            if (!string.IsNullOrWhiteSpace(queryOptions.RawValues.Expand))
                throw new FeatureNotSupportedException("Expand", string.Empty);

            if (!string.IsNullOrWhiteSpace(queryOptions.RawValues.Select))
            {
                parseContext = _odataSelectPredicateParser.Parse(parser, parseContext);
            }

            if (!string.IsNullOrWhiteSpace(queryOptions.RawValues.Format))
                throw new FeatureNotSupportedException("Format", string.Empty);



            return parseContext.Result;
        }

        public void SetPropertyValue(Dictionary<string, string> attributes, IEnumerable<KeyValuePair<string, string>> entityTypeDictionary, Dictionary<string, object> keyValues = null)
        {
            foreach (var entityMap in entityTypeDictionary)
            {
                var targetAttributeKey = entityMap.Key;
                var propertyType = entityMap.Value;
                var attribute = attributes.FirstOrDefault(predicate => Equals(predicate.Key, targetAttributeKey));
                if (!attribute.Equals(default(KeyValuePair<string, string>)))
                {
                    CastAndSetValue(propertyType, targetAttributeKey, attribute.Value, keyValues);
                }
                else
                {
                    SetValue<string>(targetAttributeKey, null, keyValues);
                }
            }
        }

        public void SetActionInfoValue(Dictionary<string, string> actionInfo, IEnumerable<KeyValuePair<string, string>> entityTypeDictionary, Dictionary<string, object> keyValues)
        {
            foreach (var entityMap in entityTypeDictionary)
            {
                var targetKey = entityMap.Key;
                var propertyType = entityMap.Value;
                var item = actionInfo.Where(x => x.Key.Equals(targetKey)).FirstOrDefault();

                if (item.Key != null)
                {
                    CastAndSetValue(propertyType, targetKey, item.Value, keyValues);
                }
                else
                {
                    SetValue<string>(targetKey, null, keyValues);
                }
            }
        }

        private void SetValue<T>(string targetAttributeKey, T result, Dictionary<string, object> keyValues)
        {
            if (keyValues != null)
                keyValues[targetAttributeKey] = result;
        }

        private void CastAndSetValue(string propertyType, string targetKey, string value, Dictionary<string, object> keyValues)
        {

            if (Equals(propertyType, TypeHandlingConstants.String))
            {
                SetValue(targetKey, value, keyValues);
            }
            else if (Equals(propertyType, TypeHandlingConstants.Guid))
            {
                _ = Guid.TryParse(value, out Guid result);
                SetValue(targetKey, result, keyValues);
            }
            else if (Equals(propertyType, TypeHandlingConstants.Int16))
            {
                _ = short.TryParse(value, out short result);
                SetValue(targetKey, result, keyValues);
            }
            else if (Equals(propertyType, TypeHandlingConstants.Int32))
            {
                _ = int.TryParse(value, out int result);
                SetValue(targetKey, result, keyValues);
            }
            else if (Equals(propertyType, TypeHandlingConstants.Int64))
            {
                _ = long.TryParse(value, out long result);
                SetValue(targetKey, result, keyValues);
            }
            else if (Equals(propertyType, TypeHandlingConstants.Double))
            {
                _ = double.TryParse(value, out double result);
                SetValue(targetKey, result, keyValues);
            }
            else if (Equals(propertyType, TypeHandlingConstants.Boolean))
            {
                _ = bool.TryParse(value, out bool result);
                SetValue(targetKey, result, keyValues);
            }
            else if (Equals(propertyType, TypeHandlingConstants.Date))
            {
                _ = Date.TryParse(value, out Date result);
                SetValue(targetKey, result, keyValues);
            }
            else if (Equals(propertyType, TypeHandlingConstants.DateTime))
            {
                _ = DateTime.TryParse(value, out DateTime result);
                SetValue(targetKey, result, keyValues);
            }
            else if (Equals(propertyType, TypeHandlingConstants.TimeOfDay))
            {
                _ = TimeOfDay.TryParse(value, out TimeOfDay result);
                SetValue(targetKey, result, keyValues);
            }
            else if (Equals(propertyType, TypeHandlingConstants.Decimal))
            {
                _ = decimal.TryParse(value, out decimal result);
                SetValue(targetKey, result, keyValues);
            }
        }

        private bool Equals(string source, string target)
        {
            return string.Equals(source, target, StringComparison.OrdinalIgnoreCase);
        }
    }
}
