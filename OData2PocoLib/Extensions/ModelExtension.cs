﻿// Copyright (c) Mohamed Hassan & Contributors. All rights reserved. See License.md in the project root for license information.

using System.Text;
using System.Text.RegularExpressions;
using OData2Poco.graph;

namespace OData2Poco.Extensions;

public static class ModelExtension
{
    internal static string GetGenericBaseType(this string type)
    {
        var pattern = "^List<(.+)>$";
        var match = Regex.Match(type, pattern);
        return match.Success ? match.Groups[1].Value : type;
    }

    internal static ClassTemplate?
        BaseTypeToClassTemplate(this ClassTemplate ct, IEnumerable<ClassTemplate> model)
    {
        return ct.BaseType.ToClassTemplate(model);
    }

    internal static ClassTemplate? ToClassTemplate(this string type, IEnumerable<ClassTemplate> list)
    {
        return list.FirstOrDefault(a => a.FullName == type);
    }

    public static StringBuilder GetImports(this ClassTemplate ct, IEnumerable<ClassTemplate> model, PocoSetting setting)
    {
        var imports = new StringBuilder();
        var allDeps = Dependency.Search(model, ct);
        foreach (var item in allDeps)
        {
            var fileName = item.GlobalName(setting);
            imports.AppendLine($"import {{{item.GlobalName(setting)}}} from './{fileName}';");
        }

        imports.AppendLine();
        return imports;
    }

    //Use FullName or name according to PocoSetting.UseFullName
    public static string GlobalName(this ClassTemplate ct, PocoSetting setting)
    {
        if (setting.UseFullName)
            return ct.FullName.RemoveDot();
        return ct.Name;
    }

    internal static string TailComment(this PropertyTemplate property)
    {
        List<string> comments = new();
        if (!property.IsNullable)
            comments.Add("Not null");
        if (property.IsKey)
            comments.Add("Primary key");
        if (property.IsReadOnly)
            comments.Add("ReadOnly");
        if (property.IsNavigate)
            comments.Add("navigator");

        if (comments.Any())
            return " //" + string.Join(", ", comments);

        return string.Empty;
    }
}