// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Dynamic.OData.Helpers;
using Dynamic.OData.Helpers.Interface;
using Dynamic.OData.Models;
using Dynamic.OData.PredicateParsers;
using Dynamic.OData.Samples;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Query.Validator;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using System;
using System.IO;

namespace Dynamic.OData.Benchmarks
{
    public class BaseBenchmark
    {
        protected IODataRequestHelper _oDataRequestHelper;
        protected IGenericEntityRepository _genericEntityRepository;
        protected const string EdmNamespaceName = "Contoso.Model";
        private ServiceProvider _provider;
        protected EdmEntityTypeSettings _edmEntityTypeSettings;
        protected HttpContext _httpContext;

        protected void BeforeEachBenchmark(int recordCount)
        {
            var collection = new ServiceCollection();
            collection.AddControllers().AddOData();
            collection.AddODataQueryFilter();
            _provider = collection.BuildServiceProvider();
            var routeBuilder = new RouteBuilder(Mock.Of<IApplicationBuilder>(x => x.ApplicationServices == _provider));
            _oDataRequestHelper = new ODataRequestHelper();
            _edmEntityTypeSettings = GetEdmEntityTypeSettings();
            _httpContext = new DefaultHttpContext();
            _genericEntityRepository = new GenericEntityRepository(recordCount);
            _oDataRequestHelper.GetEdmModel(_httpContext.Request, _edmEntityTypeSettings, EdmNamespaceName);
        }
        private EdmEntityTypeSettings GetEdmEntityTypeSettings()
        {
            var data = File.ReadAllText(Directory.GetCurrentDirectory() + @"\Data\EntityTypeSettings.json");
            data = data.Replace("//Copyright (c) Microsoft Corporation.  All rights reserved.// Licensed under the MIT License.  See License.txt in the project root for license information.\r\n", string.Empty).Trim();
            var settings = JsonConvert.DeserializeObject<EdmEntityTypeSettings>(data);
            return settings;
        }

        protected void SetRequestHost(Uri uri)
        {
            _httpContext.Request.Host = new HostString("localhost", 44357);
            if (uri != null)
            {
                _httpContext.Request.Path = uri.LocalPath;
                _httpContext.Request.Scheme = "HTTPS";
                _httpContext.Request.QueryString = new QueryString(uri.Query);
            }
            _httpContext.RequestServices = _provider;
        }

        protected ODataFilterManager GetODataFilterManager()
        {
            var queryValidator = new Mock<IODataQueryValidator>();
            BaseODataPredicateParser.EdmNamespaceName = "Dynamic.OData.Model";
            queryValidator.Setup(p => p.Validate(It.IsAny<ODataQueryOptions>(), It.IsAny<ODataValidationSettings>())).Verifiable();
            return new ODataFilterManager(
                    _oDataRequestHelper,
                  queryValidator.Object,
                   new ODataApplyPredicateParser(),
                    new ODataSelectPredicateParser(),
                    new ODataTopPredicateParser(),
                    new ODataSkipPredicateParser(),
                    new ODataOrderByPredicateParser(),
                    new ODataFilterPredicateParser()
                    );
        }
    }
}
