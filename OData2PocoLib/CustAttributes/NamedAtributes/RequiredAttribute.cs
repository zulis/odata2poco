﻿// Copyright (c) Mohamed Hassan & Contributors. All rights reserved. See License.md in the project root for license information.

namespace OData2Poco.CustAttributes.NamedAtributes;

public class RequiredAttribute : INamedAttribute
{
    public string Name { get; } = "req";

    public List<string> GetAttributes(PropertyTemplate property)
    {
        return property.IsNullable ? new List<string>() : new List<string> { "[Required]" };
    }

    public List<string> GetAttributes(ClassTemplate classTemplate)
    {
        return new();
    }
}