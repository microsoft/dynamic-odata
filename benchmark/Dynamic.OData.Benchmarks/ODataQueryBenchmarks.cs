// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Formatter.Value;
using Microsoft.OData.Edm;
using System;
using System.Collections.Generic;

namespace Dynamic.OData.Benchmarks
{
    public class ODataQueryBenchmarks : BaseBenchmark
    {
        private static EdmEntityObjectCollection edmEntityObjectCollection;
        private static List<Dictionary<string, object>> queryCollection;
        private static string _url = string.Empty;
        private static ODataFilterManager _oDataFilterManager;
        private const int recordCount = 10000;
        public ODataQueryBenchmarks()
        {
            BeforeEachBenchmark(recordCount);
            _url = "https://localhost:44312/odata/entities/user?$apply=groupby((Title),aggregate(Salary with max as MaxSalary))";
            SetRequestHost(new Uri(_url));
            var request = _httpContext.Request;
            var entityType = _oDataRequestHelper.GetEdmEntityTypeReference(request);
            var collectionType = _oDataRequestHelper.GetEdmCollectionType(request);

            edmEntityObjectCollection = new EdmEntityObjectCollection(new EdmCollectionTypeReference(collectionType));
            queryCollection = new List<Dictionary<string, object>>();
            var entities = _genericEntityRepository.GetEntities("user");
            foreach (var entity in entities)
            {
                var dynamicEntityDictionary = entity.PropertyList;
                edmEntityObjectCollection.Add(GetEdmEntityObject(dynamicEntityDictionary, entityType));
                queryCollection.Add(dynamicEntityDictionary);
            }
            _oDataFilterManager = GetODataFilterManager();          
        }

        [Benchmark]
        public void ODataGroupByAndAggregate()
        {
            _url = "https://localhost:44312/odata/entities/user?$apply=groupby((Title),aggregate(Salary with max as MaxSalary))";
            _httpContext = new DefaultHttpContext();
            _oDataRequestHelper.GetEdmModel(_httpContext.Request, _edmEntityTypeSettings, EdmNamespaceName);
            SetRequestHost(new Uri(_url));
            _oDataFilterManager.ApplyFilter(edmEntityObjectCollection, queryCollection, _httpContext.Request);
        }

        private EdmEntityObject GetEdmEntityObject(Dictionary<string, object> keyValuePairs, IEdmEntityTypeReference edmEntityType)
        {
            var obj = new EdmEntityObject(edmEntityType);
            foreach (var kvp in keyValuePairs)
                obj.TrySetPropertyValue(kvp.Key, kvp.Value);
            return obj;
        }
    }
}
