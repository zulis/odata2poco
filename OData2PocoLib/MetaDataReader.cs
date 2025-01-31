﻿// Copyright (c) Mohamed Hassan & Contributors. All rights reserved. See License.md in the project root for license information.

using System.Net;
using OData2Poco.Http;

namespace OData2Poco;

internal static class MetaDataReader
{
    public static async Task<MetaDataInfo> LoadMetaDataHttpAsync(OdataConnectionString odataConnString)
    {
        // to avoid the Error Message://An error occurred while sending the request.-->
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        var client = new CustomHttpClient(odataConnString);
        var content = await client.ReadMetaDataAsync();

        var metaData = new MetaDataInfo
        {
            MetaDataAsString = content,
            MetaDataVersion = Helper.GetMetadataVersion(content),
            ServiceUrl = client.ServiceUri.OriginalString,
            SchemaNamespace = Helper.GetNameSpace(content),
            MediaType = Media.Http
        };
        if (client.Response != null)
            foreach (var entry in client.Response.Headers)
            {
                var value = entry.Value.FirstOrDefault();
                if (value == null) continue;
                var key = entry.Key;
                metaData.ServiceHeader.Add(key, value);
            }

        metaData.ServiceVersion = Helper.GetServiceVersion(metaData.ServiceHeader);
        return metaData;
    }


    /// <summary>
    ///     Load Metadata from xml string
    /// </summary>
    /// <param name="xmlContent">xml string </param>
    /// <returns></returns>
    public static MetaDataInfo LoadMetaDataFromXml(string xmlContent)
    {
        var metaData = new MetaDataInfo
        {
            MetaDataAsString = xmlContent,
            MetaDataVersion = Helper.GetMetadataVersion(xmlContent),
            ServiceUrl = "",
            SchemaNamespace = Helper.GetNameSpace(xmlContent),
            MediaType = Media.Xml
        };
        return metaData;
    }

    public static async Task<MetaDataInfo> LoadMetadataAsync(OdataConnectionString odataConnString)
    {
        MetaDataInfo metaData;
        if (!odataConnString.ServiceUrl.StartsWith("http"))
        {
            using StreamReader reader = new(odataConnString.ServiceUrl);
            var xml = await reader.ReadToEndAsync();
            metaData = LoadMetaDataFromXml(xml);
            metaData.ServiceUrl = odataConnString.ServiceUrl;
            return metaData;
        }

        metaData = await LoadMetaDataHttpAsync(odataConnString);
        return metaData;
    }
}