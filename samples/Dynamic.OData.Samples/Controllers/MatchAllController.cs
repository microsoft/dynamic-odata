// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using MarkdownSharp;
using Microsoft.AspNetCore.Mvc;
using Dynamic.OData.Helpers.Interface;
using Dynamic.OData.Interface;
using Microsoft.OData.Edm;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.OData.Formatter.Value;

namespace Dynamic.OData.Samples.Controllers
{
    public class MatchAllController : ControllerBase
    {
        private readonly IODataModelSettingsProvider _oDataModelSettingsProvider;
        private readonly IODataFilterManager _oDataFilterManager;
        private readonly IODataRequestHelper _oDataRequestHelper;
        private readonly IGenericEntityRepository _genericEntityRepository;

        public MatchAllController(IODataModelSettingsProvider oDataModelSettingsProvider
            , IODataFilterManager oDataFilterManager
            , IODataRequestHelper oDataRequestHelper
            , IGenericEntityRepository genericEntityRepository)
        {
            _oDataModelSettingsProvider = oDataModelSettingsProvider;
            _oDataFilterManager = oDataFilterManager;
            _oDataRequestHelper = oDataRequestHelper;
            _genericEntityRepository = genericEntityRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<IEdmEntityObject>>> Get()
        {
            var entityType = _oDataRequestHelper.GetEdmEntityTypeReference(Request);
            var collectionType = _oDataRequestHelper.GetEdmCollectionType(Request);

            var collection = new EdmEntityObjectCollection(new EdmCollectionTypeReference(collectionType));
            var queryCollection = new List<Dictionary<string, object>>();
            var modelSettings = _oDataModelSettingsProvider.GetEdmModelSettingsFromRequest(Request);
            var entities = _genericEntityRepository.GetEntities(modelSettings.RouteName);
            foreach(var entity in entities)
            {
                var dynamicEntityDictionary = entity.PropertyList;
                collection.Add(GetEdmEntityObject(dynamicEntityDictionary, entityType));
                queryCollection.Add(dynamicEntityDictionary);
            }
            var result = _oDataFilterManager.ApplyFilter(collection, queryCollection, Request);
            return Ok(result);
        }

        [HttpGet]
        [Route("{controller}/help")]
        public async Task<ContentResult> GetHelpContent()
        {
            var currentdir = Directory.GetCurrentDirectory();
            var markdownText = System.IO.File.ReadAllText(currentdir + @"\readme.md");
            var cssStyle = System.IO.File.ReadAllText(currentdir + @"\content.css");
            var markdown = new Markdown();
            var inlineCSS = $"<head><style>{cssStyle}</style></head>";
            return new ContentResult
            {
                ContentType = "text/html",
                StatusCode = (int)HttpStatusCode.OK,
                Content = $"<html>{inlineCSS}<body>{markdown.Transform(markdownText)}</body></html>"
            };
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
