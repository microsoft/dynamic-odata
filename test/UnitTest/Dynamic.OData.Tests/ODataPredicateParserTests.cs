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
using Microsoft.AspNetCore.OData.Formatter.Value;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Query.Validator;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Dynamic.OData.Tests
{
    //SAM XU TODO: I just make the complier happy and didn't find time to make the following tests to pass. Please do it yourself
    public class ODataPredicateParserTests
    {
        private IODataRequestHelper _oDataRequestHelper;
        private IGenericEntityRepository _genericEntityRepository;
        const string EdmNamespaceName = "Contoso.Model";
        private ServiceProvider _provider;
        private EdmEntityTypeSettings _edmEntityTypeSettings;
        private HttpContext _httpContext;
        public void BeforeEachTest()
        {
            var collection = new ServiceCollection();
            collection.AddControllers().AddOData();
            collection.AddODataQueryFilter();
            _provider = collection.BuildServiceProvider();
            var routeBuilder = new RouteBuilder(Mock.Of<IApplicationBuilder>(x => x.ApplicationServices == _provider));
            _oDataRequestHelper = new ODataRequestHelper();
            _edmEntityTypeSettings = GetEdmEntityTypeSettings();
            _httpContext = new DefaultHttpContext();
            _genericEntityRepository = new GenericEntityRepository();
            _oDataRequestHelper.GetEdmModel(_httpContext.Request, _edmEntityTypeSettings, EdmNamespaceName);
        }

        private EdmEntityTypeSettings GetEdmEntityTypeSettings()
        {
            var data = File.ReadAllText(Directory.GetCurrentDirectory() + @"/Data/EntityTypeSettings.json");
            data = data.Replace("//Copyright (c) Microsoft Corporation.  All rights reserved.// Licensed under the MIT License.  See License.txt in the project root for license information.\r\n", string.Empty).Trim();
            var settings = JsonConvert.DeserializeObject<EdmEntityTypeSettings>(data);
            return settings;
        }

        private void SetRequestHost(Uri uri)
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


        [Fact]
        public void TestSelect()
        {
            var edmEntities = ApplyODataFilterAndGetData("https://localhost:44312/odata/entities/user?$select=id,Title,Salary");
            var parseableEntities = GetDataFromEdmEntities(edmEntities);
            var distinctColumns = parseableEntities.SelectMany(p => p.Keys).Distinct().OrderBy(p => p).ToList();
            Assert.Equal(3, distinctColumns.Count);
            Assert.Equal("id", distinctColumns[0]);
            Assert.Equal("Salary", distinctColumns[1]);
            Assert.Equal("Title", distinctColumns[2]);
        }


        [Fact]
        public void TestFilter_Contains()
        {
            var edmEntities = ApplyODataFilterAndGetData(@"https://localhost:44312/odata/entities/user?$filter=contains(Title,'Engineer')");
            var parseableEntities = GetDataFromEdmEntities(edmEntities);
            var rawData = _genericEntityRepository.GetEntities("user");
            var rawDataEngineerCount = rawData.Where(p => p.PropertyList["Title"].ToString().Contains("Engineer")).Count();
            var odataCount = parseableEntities.Count();
            Assert.True(odataCount == rawDataEngineerCount);
        }


        [Fact]
        public void TestFilter_Startswith()
        {
            var edmEntities = ApplyODataFilterAndGetData(@"https://localhost:44312/odata/entities/user?$filter=startswith(Title,'Project')");
            var parseableEntities = GetDataFromEdmEntities(edmEntities);
            var rawData = _genericEntityRepository.GetEntities("user");
            var rawDataCount = rawData.Where(p => p.PropertyList["Title"].ToString().StartsWith("Project")).Count();
            var odataCount = parseableEntities.Count();
            Assert.True(odataCount == rawDataCount);
        }

        [Fact]
        public void TestFilter_Endswith()
        {
            var edmEntities = ApplyODataFilterAndGetData(@"https://localhost:44312/odata/entities/user?$filter=endswith(Title,'Manager')");
            var parseableEntities = GetDataFromEdmEntities(edmEntities);
            var rawData = _genericEntityRepository.GetEntities("user");
            var rawDataCount = rawData.Where(p => p.PropertyList["Title"].ToString().EndsWith("Manager")).Count();
            var odataCount = parseableEntities.Count();
            Assert.True(odataCount == rawDataCount);
        }

        [Fact]
        public void TestFilter_NotStartsWith()
        {
            var edmEntities = ApplyODataFilterAndGetData(@"https://localhost:44312/odata/entities/user?$filter=not startswith(Title,'Software')");
            var parseableEntities = GetDataFromEdmEntities(edmEntities);
            var rawData = _genericEntityRepository.GetEntities("user");
            var rawDataCount = rawData.Where(p => !p.PropertyList["Title"].ToString().StartsWith("Software")).Count();
            var odataCount = parseableEntities.Count();
            Assert.True(odataCount == rawDataCount);
        }

        [Fact]
        public void TestFilter_NotEndssWith()
        {
            var edmEntities = ApplyODataFilterAndGetData(@"https://localhost:44312/odata/entities/user?$filter=not endswith(Title,'Manager')");
            var parseableEntities = GetDataFromEdmEntities(edmEntities);
            var rawData = _genericEntityRepository.GetEntities("user");
            var rawDataCount = rawData.Where(p => !p.PropertyList["Title"].ToString().EndsWith("Manager")).Count();
            var odataCount = parseableEntities.Count();
            Assert.True(odataCount == rawDataCount);
        }

        [Fact]
        public void TestFilter_NotContains()
        {
            var edmEntities = ApplyODataFilterAndGetData(@"https://localhost:44312/odata/entities/user?$filter=not contains(Title,'General')");
            var parseableEntities = GetDataFromEdmEntities(edmEntities);
            var rawData = _genericEntityRepository.GetEntities("user");
            var rawDataCount = rawData.Where(p => !p.PropertyList["Title"].ToString().Contains("General")).Count();
            var odataCount = parseableEntities.Count();
            Assert.True(odataCount == rawDataCount);
        }

        [Fact]
        public void TestFilter_BinaryCondition()
        {
            var edmEntities = ApplyODataFilterAndGetData(@"https://localhost:44312/odata/entities/user?$filter=contains(Title,'Engineer') and Salary gt 200000");
            var parseableEntities = GetDataFromEdmEntities(edmEntities);
            var rawData = _genericEntityRepository.GetEntities("user");
            var rawDataCount = rawData.Where(p => p.PropertyList["Title"].ToString().Contains("Engineer") && (decimal)p.PropertyList["Salary"] > 200000).Count();
            var odataCount = parseableEntities.Count();
            Assert.True(odataCount == rawDataCount);
        }

        [Fact]
        public void TestFilter_Equals()
        {
            var edmEntities = ApplyODataFilterAndGetData(@"https://localhost:44312/odata/entities/user?$filter=id eq null");
            var parseableEntities = GetDataFromEdmEntities(edmEntities);
            var rawData = _genericEntityRepository.GetEntities("user");
            var rawDataCount = rawData.Where(p => p.PropertyList["id"] == null).Count();
            var odataCount = parseableEntities.Count();
            Assert.True(odataCount == rawDataCount);
        }

        [Fact]
        public void TestFilter_Equals_DateFunction_Year()
        {
            int queryYear = DateTime.UtcNow.AddYears(-28).Year;
            var edmEntities = ApplyODataFilterAndGetData($"https://localhost:44312/odata/entities/user?$filter=year(BornOn) eq {queryYear}");
            var parseableEntities = GetDataFromEdmEntities(edmEntities);
            var rawData = _genericEntityRepository.GetEntities("user");
            var rawDataCount = rawData.Where(p => ((DateTime)p.PropertyList["BornOn"]).Year == queryYear).Count();
            var odataCount = parseableEntities.Count();
            Assert.True(odataCount == rawDataCount);
        }

        [Fact]
        public void TestFilter_Equals_DateFunction_Month()
        {
            int queryMonth = DateTime.UtcNow.AddYears(-28).Month;
            var edmEntities = ApplyODataFilterAndGetData($"https://localhost:44312/odata/entities/user?$filter=month(BornOn) eq {queryMonth}");
            var parseableEntities = GetDataFromEdmEntities(edmEntities);
            var rawData = _genericEntityRepository.GetEntities("user");
            var rawDataCount = rawData.Where(p => ((DateTime)p.PropertyList["BornOn"]).Month == queryMonth).Count();
            var odataCount = parseableEntities.Count();
            Assert.True(odataCount == rawDataCount);
        }

        [Fact]
        public void TestFilter_Equals_DateFunction_Day()
        {
            int queryDay = DateTime.UtcNow.AddYears(-28).Day;
            var edmEntities = ApplyODataFilterAndGetData($"https://localhost:44312/odata/entities/user?$filter=day(BornOn) eq {queryDay}");
            var parseableEntities = GetDataFromEdmEntities(edmEntities);
            var rawData = _genericEntityRepository.GetEntities("user");
            var rawDataCount = rawData.Where(p => ((DateTime)p.PropertyList["BornOn"]).Day == queryDay).Count();
            var odataCount = parseableEntities.Count();
            Assert.True(odataCount == rawDataCount);
        }


        [Fact]
        public void TestFilter_NotEquals()
        {
            var edmEntities = ApplyODataFilterAndGetData(@"https://localhost:44312/odata/entities/user?$filter=Title ne 'Software Engineer'");
            var parseableEntities = GetDataFromEdmEntities(edmEntities);
            var rawData = _genericEntityRepository.GetEntities("user");
            var rawDataCount = rawData.Where(p => p.PropertyList["Title"] != "Software Engineer").Count();
            var odataCount = parseableEntities.Count();
            Assert.True(odataCount == rawDataCount);
        }

        [Fact]
        public void TestFilter_NotEquals_DateFunction_Year()
        {
            int queryYear = DateTime.UtcNow.AddYears(-28).Year;
            var edmEntities = ApplyODataFilterAndGetData($"https://localhost:44312/odata/entities/user?$filter=year(BornOn) ne {queryYear}");
            var parseableEntities = GetDataFromEdmEntities(edmEntities);
            var rawData = _genericEntityRepository.GetEntities("user");
            var rawDataCount = rawData.Where(p => ((DateTime)p.PropertyList["BornOn"]).Year != queryYear).Count();
            var odataCount = parseableEntities.Count();
            Assert.True(odataCount == rawDataCount);
        }

        [Fact]
        public void TestFilter_NotEquals_DateFunction_Month()
        {
            int queryMonth = DateTime.UtcNow.AddYears(-28).Month;
            var edmEntities = ApplyODataFilterAndGetData($"https://localhost:44312/odata/entities/user?$filter=month(BornOn) ne {queryMonth}");
            var parseableEntities = GetDataFromEdmEntities(edmEntities);
            var rawData = _genericEntityRepository.GetEntities("user");
            var rawDataCount = rawData.Where(p => ((DateTime)p.PropertyList["BornOn"]).Month != queryMonth).Count();
            var odataCount = parseableEntities.Count();
            Assert.True(odataCount == rawDataCount);
        }

        [Fact]
        public void TestFilter_NotEquals_DateFunction_Day()
        {
            int queryDay = DateTime.UtcNow.AddYears(-28).Day;
            var edmEntities = ApplyODataFilterAndGetData($"https://localhost:44312/odata/entities/user?$filter=day(BornOn) ne {queryDay}");
            var parseableEntities = GetDataFromEdmEntities(edmEntities);
            var rawData = _genericEntityRepository.GetEntities("user");
            var rawDataCount = rawData.Where(p => ((DateTime)p.PropertyList["BornOn"]).Day != queryDay).Count();
            var odataCount = parseableEntities.Count();
            Assert.True(odataCount == rawDataCount);
        }

        [Fact]
        public void TestFilter_GreaterThan()
        {
            var edmEntities = ApplyODataFilterAndGetData(@"https://localhost:44312/odata/entities/user?$filter=Age gt 30 or VacationDaysInHours gt 25 or Salary gt 200000 or UniversalId gt 22335679003");
            var parseableEntities = GetDataFromEdmEntities(edmEntities);
            var rawData = _genericEntityRepository.GetEntities("user");
            var rawDataCount = rawData.Where(p => (int)p.PropertyList["Age"] > 30
                                               || (double)p.PropertyList["VacationDaysInHours"] > 25
                                               || (decimal)p.PropertyList["Salary"] > 200000
                                               || (long)p.PropertyList["UniversalId"] > 22335679003).Count();
            var odataCount = parseableEntities.Count();
            Assert.True(odataCount == rawDataCount);
        }

        [Fact]
        public void TestFilter_GreaterThan_Date()
        {
            int queryYear = DateTime.UtcNow.AddYears(-28).Year;
            var date = new DateTime(queryYear, 1, 1);
            var edmEntities = ApplyODataFilterAndGetData($"https://localhost:44312/odata/entities/user?$filter=BornOn gt {queryYear}-01-01");
            var parseableEntities = GetDataFromEdmEntities(edmEntities);
            var rawData = _genericEntityRepository.GetEntities("user");            
            var rawDataCount = rawData.Where(p => ((DateTime)p.PropertyList["BornOn"]) > date).Count();
            var odataCount = parseableEntities.Count();
            Assert.True(odataCount == rawDataCount);
        }

        [Fact]
        public void TestFilter_GreaterThanOrEquals()
        {
            var edmEntities = ApplyODataFilterAndGetData(@"https://localhost:44312/odata/entities/user?$filter=Age ge 30 or VacationDaysInHours ge 25 or Salary ge 200000 or UniversalId ge 22335679003");
            var parseableEntities = GetDataFromEdmEntities(edmEntities);
            var rawData = _genericEntityRepository.GetEntities("user");
            var rawDataCount = rawData.Where(p => (int)p.PropertyList["Age"] >= 30 
                                                || (double)p.PropertyList["VacationDaysInHours"] >= 25
                                                || (decimal)p.PropertyList["Salary"] >= 200000
                                                || (long)p.PropertyList["UniversalId"] >= 22335679003).Count();
            var odataCount = parseableEntities.Count();
            Assert.True(odataCount == rawDataCount);
        }

        [Fact]
        public void TestFilter_LesserThan()
        {
            var edmEntities = ApplyODataFilterAndGetData(@"https://localhost:44312/odata/entities/user?$filter=Age lt 30 or VacationDaysInHours lt 25 or Salary lt 200000 or UniversalId lt 22335679003");
            var parseableEntities = GetDataFromEdmEntities(edmEntities);
            var rawData = _genericEntityRepository.GetEntities("user");
            var rawDataCount = rawData.Where(p => (int)p.PropertyList["Age"] < 30
                                                || (double)p.PropertyList["VacationDaysInHours"] < 25
                                                || (decimal)p.PropertyList["Salary"] < 200000
                                                || (long)p.PropertyList["UniversalId"] < 22335679003).Count();
            var odataCount = parseableEntities.Count();
            Assert.True(odataCount == rawDataCount);
        }

        [Fact]
        public void TestFilter_LesserThan_Date()
        {
            var edmEntities = ApplyODataFilterAndGetData(@"https://localhost:44312/odata/entities/user?$filter=BornOn lt 1993-01-01");
            var parseableEntities = GetDataFromEdmEntities(edmEntities);
            var rawData = _genericEntityRepository.GetEntities("user");
            var date = new DateTime(1993, 1, 1);
            var rawDataCount = rawData.Where(p => ((DateTime)p.PropertyList["BornOn"]) < date).Count();
            var odataCount = parseableEntities.Count();
            Assert.True(odataCount == rawDataCount);
        }

        [Fact]
        public void TestFilter_LesserThanOrEquals()
        {
            var edmEntities = ApplyODataFilterAndGetData(@"https://localhost:44312/odata/entities/user?$filter=Age le 30 or VacationDaysInHours le 25 or Salary le 200000 or UniversalId le 22335679003");
            var parseableEntities = GetDataFromEdmEntities(edmEntities);
            var rawData = _genericEntityRepository.GetEntities("user");
            var rawDataCount = rawData.Where(p => (int)p.PropertyList["Age"] <= 30
                                                || (double)p.PropertyList["VacationDaysInHours"] <= 25
                                                || (decimal)p.PropertyList["Salary"] <= 200000
                                                || (long)p.PropertyList["UniversalId"] <= 22335679003).Count();
            var odataCount = parseableEntities.Count();
            Assert.True(odataCount == rawDataCount);
        }

        [Fact]
        public void TestFilter_BinaryCondition_Or()
        {
            var edmEntities = ApplyODataFilterAndGetData(@"https://localhost:44312/odata/entities/user?$filter=Salary le 200000 or Age lt 30");
            var parseableEntities = GetDataFromEdmEntities(edmEntities);
            var rawData = _genericEntityRepository.GetEntities("user");
            var rawDataCount = rawData.Where(p => (decimal)p.PropertyList["Salary"] <= 200000 || (int)p.PropertyList["Age"] < 30).Count();
            var odataCount = parseableEntities.Count();
            Assert.True(odataCount == rawDataCount);
        }

        [Fact]
        public void TestTop()
        {
            var edmEntities = ApplyODataFilterAndGetData(@"https://localhost:44312/odata/entities/user?$top=4");
            Assert.True(edmEntities.Count() == 4);
        }

        [Fact]
        public void TestSkip()
        {
            var edmEntities = ApplyODataFilterAndGetData(@"https://localhost:44312/odata/entities/user?$skip=5");
            var rawData = _genericEntityRepository.GetEntities("user");
            Assert.True(rawData.Count() - edmEntities.Count() == 5);
        }

        [Fact]
        public void TestOrderBy_Ascending()
        {
            var edmEntities = ApplyODataFilterAndGetData(@"https://localhost:44312/odata/entities/user?$orderby=Title asc");
            var parseableEntities = GetDataFromEdmEntities(edmEntities);
            var rawData = _genericEntityRepository.GetEntities("user");
            var orderedTitlesFromOData = parseableEntities.Select(p => p["Title"].ToString()).ToList();
            var rawOrderedList = rawData.Select(p => p.PropertyList["Title"].ToString()).OrderBy(p => p).ToList();
            Assert.True(orderedTitlesFromOData.Count == rawOrderedList.Count);
            for (int i = 0; i < orderedTitlesFromOData.Count; i++)
            {
                Assert.True(string.Equals(orderedTitlesFromOData[i], rawOrderedList[i]));
            }
        }

        [Fact]
        public void TestOrderBy_MultiAscending()
        {
            var edmEntities = ApplyODataFilterAndGetData(@"https://localhost:44312/odata/entities/user?$orderby=Title asc, Age desc");
            var parseableEntities = GetDataFromEdmEntities(edmEntities).ToList();
            var rawData = _genericEntityRepository.GetEntities("user");
            var rawOrderedList = rawData.OrderBy(p => p.PropertyList["Title"]).ThenByDescending(p => (int)p.PropertyList["Age"]).ToList();
            Assert.True(parseableEntities.Count == rawOrderedList.Count);
            for (int i = 0; i < parseableEntities.Count; i++)
            {
                Assert.True(string.Equals(rawOrderedList[i].PropertyList["Title"].ToString(), parseableEntities[i]["Title"].ToString()));
                Assert.True(string.Equals(rawOrderedList[i].PropertyList["Age"].ToString(), parseableEntities[i]["Age"].ToString()));
            }
        }

        [Fact]
        public void TestOrderBy_MultiAscending_2()
        {
            var edmEntities = ApplyODataFilterAndGetData(@"https://localhost:44312/odata/entities/user?$orderby=Title desc, Age asc");
            var parseableEntities = GetDataFromEdmEntities(edmEntities).ToList();
            var rawData = _genericEntityRepository.GetEntities("user");
            var rawOrderedList = rawData.OrderByDescending(p => p.PropertyList["Title"]).ThenBy(p => (int)p.PropertyList["Age"]).ToList();
            Assert.True(parseableEntities.Count == rawOrderedList.Count);
            for (int i = 0; i < parseableEntities.Count; i++)
            {
                Assert.True(string.Equals(rawOrderedList[i].PropertyList["Title"].ToString(), parseableEntities[i]["Title"].ToString()));
                Assert.True(string.Equals(rawOrderedList[i].PropertyList["Age"].ToString(), parseableEntities[i]["Age"].ToString()));
            }
        }

        [Fact]
        public void TestOrderBy_Descending()
        {
            var edmEntities = ApplyODataFilterAndGetData(@"https://localhost:44312/odata/entities/user?$orderby=Title desc");
            var parseableEntities = GetDataFromEdmEntities(edmEntities);
            var rawData = _genericEntityRepository.GetEntities("user");
            var orderedTitlesFromOData = parseableEntities.Select(p => p["Title"].ToString()).ToList();
            var rawOrderedList = rawData.Select(p => p.PropertyList["Title"].ToString()).OrderByDescending(p => p).ToList();
            Assert.True(orderedTitlesFromOData.Count == rawOrderedList.Count);
            for (int i = 0; i < orderedTitlesFromOData.Count; i++)
            {
                Assert.True(string.Equals(orderedTitlesFromOData[i], rawOrderedList[i]));
            }
        }

        [Fact]
        public void TestApply_AggregateFunctions_Decimal()
        {
            var edmEntities = ApplyODataFilterAndGetData(@"https://localhost:44312/odata/entities/user?
                $apply=groupby((Title),aggregate(Salary with max as MaxSalary
                , Salary with min as MinSalary
                , Salary with average as AvgSalary
                , Salary with sum as TotalSalary
                , id with countdistinct as EmployeeCount
                , $count as TotalGroupCount))");
            var parseableEntities = GetDataFromEdmEntities(edmEntities, new List<string> { "MaxSalary", "MinSalary", "AvgSalary", "TotalSalary", "EmployeeCount", "TotalGroupCount" });
            var distinctColumns = parseableEntities.SelectMany(p => p.Keys).Distinct().OrderBy(p => p).ToList();
            var rawData = _genericEntityRepository.GetEntities("user");
            var linqAggregateResult = rawData.GroupBy(p => p.PropertyList["Title"]).Select(p => new Dictionary<string, object>
            {
                { "Title", p.Key },
                { "MaxSalary", p.Max(p => p.PropertyList["Salary"])},
                { "MinSalary", p.Min(p => p.PropertyList["Salary"])},
                { "AvgSalary",  p.Average(p => (decimal)p.PropertyList["Salary"])},
                { "TotalSalary", p.Sum(p => (decimal)p.PropertyList["Salary"])},
                { "EmployeeCount", p.Select(p => p.PropertyList["id"].ToString()).Distinct().Count() },
                { "TotalGroupCount", p.Count() }
            });
            Assert.True(CompareListODictionaries(linqAggregateResult, parseableEntities, "Title"));
        }

        [Fact]
        public void TestApply_AggregateFunctions_Int32()
        {
            var edmEntities = ApplyODataFilterAndGetData(@"https://localhost:44312/odata/entities/user?
                $apply=groupby((Title),aggregate(Age with max as MaxAge
                , Age with min as MinAge
                , Age with average as AvgAge
                , Age with sum as TotalAge
                , id with countdistinct as EmployeeCount))");
            var parseableEntities = GetDataFromEdmEntities(edmEntities, new List<string> { "MaxAge", "MinAge", "AvgAge", "TotalAge", "EmployeeCount" });
            var distinctColumns = parseableEntities.SelectMany(p => p.Keys).Distinct().OrderBy(p => p).ToList();
            var rawData = _genericEntityRepository.GetEntities("user");
            var linqAggregateResult = rawData.GroupBy(p => p.PropertyList["Title"]).Select(p => new Dictionary<string, object>
            {
                { "Title", p.Key },
                { "MaxAge", p.Max(p => p.PropertyList["Age"])},
                { "MinAge", p.Min(p => p.PropertyList["Age"])},
                { "AvgAge",  p.Average(p => (int)p.PropertyList["Age"])},
                { "TotalAge", p.Sum(p => (int)p.PropertyList["Age"])},
                { "EmployeeCount", p.Select(p => p.PropertyList["id"].ToString()).Distinct().Count() }
            });
            Assert.True(CompareListODictionaries(linqAggregateResult, parseableEntities, "Title"));
        }

        [Fact]
        public void TestApply_AggregateFunctions_Long()
        {
            var edmEntities = ApplyODataFilterAndGetData(@"https://localhost:44312/odata/entities/user?
                $apply=groupby((Title),aggregate(UniversalId with max as MaxUniversalId
                , UniversalId with min as MinUniversalId
                , UniversalId with average as AvgUniversalId
                , UniversalId with sum as TotalUniversalId
                , id with countdistinct as EmployeeCount))");
            var parseableEntities = GetDataFromEdmEntities(edmEntities, new List<string> { "MaxUniversalId", "MinUniversalId", "AvgUniversalId", "TotalUniversalId", "EmployeeCount" });
            var distinctColumns = parseableEntities.SelectMany(p => p.Keys).Distinct().OrderBy(p => p).ToList();
            var rawData = _genericEntityRepository.GetEntities("user");
            var linqAggregateResult = rawData.GroupBy(p => p.PropertyList["Title"]).Select(p => new Dictionary<string, object>
            {
                { "Title", p.Key },
                { "MaxUniversalId", p.Max(p => p.PropertyList["UniversalId"])},
                { "MinUniversalId", p.Min(p => p.PropertyList["UniversalId"])},
                { "AvgUniversalId",  p.Average(p => (long)p.PropertyList["UniversalId"])},
                { "TotalUniversalId", p.Sum(p => (long)p.PropertyList["UniversalId"])},
                { "EmployeeCount", p.Select(p => p.PropertyList["id"].ToString()).Distinct().Count() }
            });
            Assert.True(CompareListODictionaries(linqAggregateResult, parseableEntities, "Title"));
        }

        [Fact]
        public void TestApply_AggregateFunctions_Double()
        {
            var edmEntities = ApplyODataFilterAndGetData(@"https://localhost:44312/odata/entities/user?
                $apply=groupby((Title),aggregate(VacationDaysInHours with max as MaxVacationDaysInHours
                , VacationDaysInHours with min as MinVacationDaysInHours
                , VacationDaysInHours with average as AvgVacationDaysInHours
                , VacationDaysInHours with sum as TotalVacationDaysInHours
                , id with countdistinct as EmployeeCount))&$count=true");
            var parseableEntities = GetDataFromEdmEntities(edmEntities, new List<string> { "MaxVacationDaysInHours", "MinVacationDaysInHours", "AvgVacationDaysInHours", "TotalVacationDaysInHours", "VacationDaysCount","EmployeeCount" });
            var distinctColumns = parseableEntities.SelectMany(p => p.Keys).Distinct().OrderBy(p => p).ToList();
            var rawData = _genericEntityRepository.GetEntities("user");
            var linqAggregateResult = rawData.GroupBy(p => p.PropertyList["Title"]).Select(p => new Dictionary<string, object>
            {
                { "Title", p.Key },
                { "MaxVacationDaysInHours", p.Max(p => p.PropertyList["VacationDaysInHours"])},
                { "MinVacationDaysInHours", p.Min(p => p.PropertyList["VacationDaysInHours"])},
                { "AvgVacationDaysInHours",  p.Average(p => (double)p.PropertyList["VacationDaysInHours"])},
                { "TotalVacationDaysInHours", p.Sum(p => (double)p.PropertyList["VacationDaysInHours"])},
                { "EmployeeCount", p.Select(p => p.PropertyList["id"].ToString()).Distinct().Count() }
            });
            Assert.True(CompareListODictionaries(linqAggregateResult, parseableEntities, "Title"));
        }

        [Fact]
        public void TestApply_CustomAggregateFunction()
        {
            var edmEntities = ApplyODataFilterAndGetData(@"https://localhost:44312/odata/entities/user?
                $apply=groupby((Title),aggregate(id with Custom.List as Items
                , Salary with Custom.CountDistinct_'Salary gt 200000' as HighSalary))");
            var parseableEntities = GetDataFromEdmEntities(edmEntities, new List<string> { "Items", "HighSalary" });
            var distinctColumns = parseableEntities.SelectMany(p => p.Keys).Distinct().OrderBy(p => p).ToList();
            var rawData = _genericEntityRepository.GetEntities("user");
            var linqAggregateResult = rawData.GroupBy(p => p.PropertyList["Title"]).Select(p => new Dictionary<string, object>
            {
                { "Title", p.Key },
                { "Items", p.ToList() },
                { "HighSalary", p.Where(p => (decimal)p.PropertyList["Salary"] > 200000)
                .Select(p => (decimal)p.PropertyList["Salary"]).Distinct().Count() }
            });
            Assert.Equal(linqAggregateResult.Count(), parseableEntities.Count());
        }

        [Fact]
        public void TestApply_CustomAggregateFunctionWithSelect()
        {
            var edmEntities = ApplyODataFilterAndGetData(@"https://localhost:44312/odata/entities/user?
                $apply=groupby((Title),aggregate(id with Custom.List as Items))&$select=Items");
            var parseableEntities = GetDataFromEdmEntities(edmEntities, new List<string> { "Items", "HighSalary" });
            var distinctColumns = parseableEntities.SelectMany(p => p.Keys).Distinct().OrderBy(p => p).ToList();
            var rawData = _genericEntityRepository.GetEntities("user");
            var linqAggregateResult = rawData.GroupBy(p => p.PropertyList["Title"]).Select(p => new Dictionary<string, object>
            {
                { "Items", p.ToList() }
            });
            Assert.Equal(linqAggregateResult.Count(), parseableEntities.Count());
        }


        [Fact]
        public void TestApply_NoALias()
        {
            try
            {
                var edmEntities = ApplyODataFilterAndGetData(@"https://localhost:44312/odata/entities/user?
                $apply=groupby((Title),aggregate(id with countdistinct
                , Age with min))");
            }
            catch(Exception ex)
            {
                Assert.True(ex is ODataException);
            }
        }

        [Fact]
        public void TestApply_AggregateWithoutGroupBy()
        {
            var edmEntities = ApplyODataFilterAndGetData(@"https://localhost:44312/odata/entities/user?
                $apply=aggregate(Salary with min as MinSalary)");
            var parseableEntities = GetDataFromEdmEntities(edmEntities, new List<string> { "MinSalary" });
            var distinctColumns = parseableEntities.SelectMany(p => p.Keys).Distinct().OrderBy(p => p).ToList();
            var rawData = _genericEntityRepository.GetEntities("user");
            var linqAggregateResult = new List<Dictionary<string, object>>{
                new Dictionary<string, object>
                {
                    { "MinSalary",rawData.Min(q => ((decimal)q.PropertyList["Salary"])) }
                } };
            Assert.Equal(linqAggregateResult.Count(), parseableEntities.Count());
        }

        [Fact]
        public void TestODataFilterManager_SetPropertyValue()
        {
            BeforeEachTest();
            var data = _genericEntityRepository.GetEntities("user");
            var firstUser = data.FirstOrDefault();
            var attributekvp = firstUser.PropertyList.ToDictionary(p => p.Key, p => p.Value.ToString());
            var propertyList = _edmEntityTypeSettings.Properties.Select(p => new KeyValuePair<string, string>(p.PropertyName, p.PropertyType));
            var stringobjectdictionary = new Dictionary<string, object>();
            var odataFilterManager = GetODataFilterManager();
            odataFilterManager.SetPropertyValue(attributekvp, propertyList, stringobjectdictionary);
            Assert.True(CompareDictionaries(firstUser.PropertyList, stringobjectdictionary));
        }

        [Fact]
        public void TestODataFilterManager_SetActionInfoValue()
        {
            BeforeEachTest();
            var data = _genericEntityRepository.GetEntities("user");
            var firstUser = data.FirstOrDefault();
            var attributekvp = firstUser.PropertyList.ToDictionary(p => p.Key, p => p.Value.ToString());
            var propertyList = _edmEntityTypeSettings.Properties.Select(p => new KeyValuePair<string, string>(p.PropertyName, p.PropertyType));
            var stringobjectdictionary = new Dictionary<string, object>();
            var odataFilterManager = GetODataFilterManager();
            odataFilterManager.SetActionInfoValue(attributekvp, propertyList, stringobjectdictionary);
            Assert.True(CompareDictionaries(firstUser.PropertyList, stringobjectdictionary));
        }

        [Fact]
        public void TestODataRequestHelper_ModifyResponse()
        {
            var data = File.ReadAllText(Directory.GetCurrentDirectory() + @"/Data/sampledata.json");
            data = data.Replace("//Copyright (c) Microsoft Corporation.  All rights reserved.// Licensed under the MIT License.  See License.txt in the project root for license information.\r\n", string.Empty).Trim();
            var odataRequestHelper = new ODataRequestHelper();
            var resp = odataRequestHelper.ModifyResponse(data, true, true, "Test");
            Assert.DoesNotContain("@odata.type", resp);
            Assert.Contains("Test", resp);
        }

        private bool CompareListODictionaries(IEnumerable<Dictionary<string, object>> first, IEnumerable<Dictionary<string, object>> second, string idProperty)
        {
            foreach (var item in first)
            {
                var seconditem = second.FirstOrDefault(p => p[idProperty] == item[idProperty]);
                if (seconditem == null)
                    return false;
                if (!CompareDictionaries(item, seconditem))
                    return false;
            }
            return true;
        }

        private bool CompareDictionaries(Dictionary<string, object> a, Dictionary<string, object> b)
        {
            foreach (var key in a.Keys)
            {
                if (!b.ContainsKey(key))
                    return false;
                if (a[key].ToString() != b[key].ToString())
                    return false;
            }
            return true;
        }

        private IEnumerable<Dictionary<string, object>> GetDataFromEdmEntities(IEnumerable<IEdmEntityObject> edmEntities, List<string> computedPropertyNames = null)
        {
            var data = new List<Dictionary<string, object>>();
            var propertyNames = _edmEntityTypeSettings.Properties.Select(p => p.PropertyName).ToList();
            if (computedPropertyNames != null && computedPropertyNames.Any())
                propertyNames.AddRange(computedPropertyNames);
            foreach (var entity in edmEntities)
            {
                var dict = new Dictionary<string, object>();
                foreach (var property in propertyNames)
                {
                    if (entity.TryGetPropertyValue(property, out object value))
                        dict.Add(property, value);
                }
                data.Add(dict);
            }
            return data;
        }

        private IEnumerable<IEdmEntityObject> ApplyODataFilterAndGetData(string url)
        {
            BeforeEachTest();
            SetRequestHost(new Uri(url));
            var request = _httpContext.Request;
            var entityType = _oDataRequestHelper.GetEdmEntityTypeReference(request);
            var collectionType = _oDataRequestHelper.GetEdmCollectionType(request);

            var collection = new EdmEntityObjectCollection(new EdmCollectionTypeReference(collectionType));
            var queryCollection = new List<Dictionary<string, object>>();
            var entities = _genericEntityRepository.GetEntities(_edmEntityTypeSettings.RouteName);
            foreach (var entity in entities)
            {
                var dynamicEntityDictionary = entity.PropertyList;
                collection.Add(GetEdmEntityObject(dynamicEntityDictionary, entityType));
                queryCollection.Add(dynamicEntityDictionary);
            }
            var odataFilterManager = GetODataFilterManager();
            return odataFilterManager.ApplyFilter(collection, queryCollection, _httpContext.Request);
        }

        private ODataFilterManager GetODataFilterManager()
        {
            var queryValidator = new Mock<IODataQueryValidator>();
            BaseODataPredicateParser.EdmNamespaceName = "Dynamic.OData.Model";
            queryValidator.Setup(p => p.Validate(It.IsAny<ODataQueryOptions>() ,It.IsAny<ODataValidationSettings>())).Verifiable();
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

        private EdmEntityObject GetEdmEntityObject(Dictionary<string, object> keyValuePairs, IEdmEntityTypeReference edmEntityType)
        {
            var obj = new EdmEntityObject(edmEntityType);
            foreach (var kvp in keyValuePairs)
                obj.TrySetPropertyValue(kvp.Key, kvp.Value);
            return obj;
        }
    }


}
