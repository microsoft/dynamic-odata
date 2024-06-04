// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Dynamic.OData.Helpers;
using Dynamic.OData.Helpers.Interface;
using Dynamic.OData.Interface;
using Dynamic.OData.PredicateParsers;
using Dynamic.OData.PredicateParsers.Interface;
using Microsoft.AspNetCore.OData.Query.Validator;
using Microsoft.Extensions.DependencyInjection;

namespace Dynamic.OData
{
    public static class ODataServiceCollectionExtensions
    {
        public static void AddDynamicODataQueryServices(this IServiceCollection services, string edmNamespaceName = null)
        {
            BaseODataPredicateParser.EdmNamespaceName = !string.IsNullOrWhiteSpace(edmNamespaceName) ? edmNamespaceName : "Dynamic.OData.Model";
            
            services.AddScoped<IODataRequestHelper, ODataRequestHelper>();
            services.AddScoped<IODataQueryValidator, ODataQueryValidator>();
            services.AddSingleton<ODataApplyPredicateParser>();
            services.AddSingleton<ODataSelectPredicateParser>();
            services.AddSingleton<ODataTopPredicateParser>();
            services.AddSingleton<ODataSkipPredicateParser>();
            services.AddSingleton<ODataOrderByPredicateParser>();
            services.AddSingleton<ODataFilterPredicateParser>();

            services.AddScoped<IODataFilterManager, ODataFilterManager>(serviceProvider =>
            {
                return new ODataFilterManager(
                    (IODataRequestHelper)serviceProvider.GetService(typeof(IODataRequestHelper)),
                    (IODataQueryValidator)serviceProvider.GetService(typeof(IODataQueryValidator)),
                    (IODataPredicateParser)serviceProvider.GetService(typeof(ODataApplyPredicateParser)),
                    (IODataPredicateParser)serviceProvider.GetService(typeof(ODataSelectPredicateParser)),
                    (IODataPredicateParser)serviceProvider.GetService(typeof(ODataTopPredicateParser)),
                    (IODataPredicateParser)serviceProvider.GetService(typeof(ODataSkipPredicateParser)),
                    (IODataPredicateParser)serviceProvider.GetService(typeof(ODataOrderByPredicateParser)),
                    (IODataPredicateParser)serviceProvider.GetService(typeof(ODataFilterPredicateParser))
                    );
            });
        }
    }
}
