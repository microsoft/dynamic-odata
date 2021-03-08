// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Routing.Conventions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Dynamic.OData;
using Dynamic.OData.Helpers;
using Microsoft.OData.Edm;
using System.Collections.Generic;
using System.Linq;

namespace Dynamic.OData.Samples
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            
            services.AddSingleton<IODataModelSettingsProvider, ODataModelSettingsProvider>();
            services.AddSingleton<IGenericEntityRepository, GenericEntityRepository>();
            //services.AddScoped<IODataRequestHelper, ODataRequestHelper>();
            services.AddDynamicODataQueryServices();

            services.AddOData();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.EnableDependencyInjection();
                endpoints.Filter().Expand().Select().OrderBy().MaxTop(null).Count();

                endpoints.MapODataRoute("odata", "odata/entities/{entityname}", containerBuilder =>
                {
                    containerBuilder.AddService(Microsoft.OData.ServiceLifetime.Scoped, typeof(IEdmModel), sp =>
                    {
                        var serviceScope = sp.GetRequiredService<HttpRequestScope>();
                        var modelSettingsProvider = app.ApplicationServices.GetService<IODataModelSettingsProvider>();
                        var odataRequestHelper = new ODataRequestHelper();
                        return odataRequestHelper.GetEdmModel(serviceScope.HttpRequest
                            , modelSettingsProvider.GetEdmModelSettingsFromRequest(serviceScope.HttpRequest), "Microsoft.Contoso.Models");
                    });

                    containerBuilder.AddService(Microsoft.OData.ServiceLifetime.Scoped, typeof(IEnumerable<IODataRoutingConvention>), sp =>
                    {
                        IList<IODataRoutingConvention> routingConventions = ODataRoutingConventions.CreateDefault();
                        routingConventions.Insert(0, new MatchAllRoutingConvention("MatchAll"));
                        return routingConventions.ToList().AsEnumerable();
                    });
                });
            });
        }
    }
}
