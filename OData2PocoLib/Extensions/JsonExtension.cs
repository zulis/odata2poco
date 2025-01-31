﻿// Copyright (c) Mohamed Hassan & Contributors. All rights reserved. See License.md in the project root for license information.

using Newtonsoft.Json;

namespace OData2Poco.Extensions;

public static class JsonExtension
{
    /// <summary>
    public static string ToJson(this object data)
    {
        var json = JsonConvert.SerializeObject(data, Formatting.Indented);
        return json;
    }

    public static T? ToObject<T>(this string json)
    {
        var results = JsonConvert.DeserializeObject<T>(json);
        return results;
    }
}