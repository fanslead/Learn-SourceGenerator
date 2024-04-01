using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace GenerateClassFromSwagger.Analysis
{
    [Generator]
    public class ClassFromSwaggerGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var pipeline = context.AdditionalTextsProvider.Select(static (text, cancellationToken) =>
            {
                if (!text.Path.EndsWith("swagger.json", StringComparison.OrdinalIgnoreCase))
                {
                    return default;
                }

                return JObject.Parse(text.GetText(cancellationToken)!.ToString());
            })
            .Where((pair) => pair is not null);

            context.RegisterSourceOutput(pipeline, static (context, swagger) =>
            {

                List<(string name, string sourceString)> sources = new List<(string name, string sourceString)>();


                #region 生成实体
                var schemas = (JObject)swagger["components"]!["schemas"]!;
                foreach (JProperty item in schemas.Properties())
                {
                    if (item != null)
                    {
                        sources.Add((HandleClassName(item.Name), $@"#nullable enable
using System;
using System.Collections.Generic;

namespace SwaggerEntities;
public {ClassOrEnum((JObject)item.Value)} {HandleClassName(item.Name)} 
{{
    {BuildProperty((JObject)item.Value)}
}}
                "));
                    }
                }
                foreach (var (name, sourceString) in sources)
                {
                    var sourceText = SourceText.From(sourceString, Encoding.UTF8);
                    context.AddSource($"{name}.g.cs", sourceText);
                }
                #endregion
            });
        }

        static string HandleClassName(string name)
        {
            return name.Split('.').Last().Replace("<", "").Replace(">", "").Replace(",", "");
        }
        static string ClassOrEnum(JObject value)
        {
            return value.ContainsKey("enum") ? "enum" : "partial class";
        }


        static string BuildProperty(JObject value)
        {
            var sb = new StringBuilder();
            if (value.ContainsKey("properties"))
            {
                var propertys = (JObject)value["properties"]!;
                foreach (JProperty item in propertys!.Properties())
                {
                    sb.AppendLine($@"
    public {BuildProertyType((JObject)item.Value)} {ToUpperFirst(item.Name)}  {{ get; set; }}
");
                }
            }
            if (value.ContainsKey("enum"))
            {
                foreach (var item in JsonConvert.DeserializeObject<List<int>>(value["enum"]!.ToString())!)
                {
                    sb.Append($@"
    _{item},
");
                }
                sb.Remove(sb.Length - 1, 1);
            }
            return sb.ToString();
        }

        static string BuildProertyType(JObject value)
        {
            ;
            var type = GetType(value);
            var nullable = value.ContainsKey("nullable") ? value["nullable"]!.Value<bool?>() switch
            {
                true => "?",
                false => "",
                _ => ""
            } : "";
            return type + nullable;
        }

        static string GetType(JObject value)
        {
            return value.ContainsKey("type") ? value["type"]!.Value<string>() switch
            {
                "string" => "string",
                "boolean" => "bool",
                "number" => value["format"]!.Value<string>() == "float" ? "float" : "double",
                "integer" => value["format"]!.Value<string>() == "int32" ? "int" : "long",
                "array" => ((JObject)value["items"]!).ContainsKey("items") ?
                $"List<{HandleClassName(value["items"]!["$ref"]!.Value<string>()!)}>"
                : $"List<{GetType((JObject)value["items"]!)}>",
                "object" => value.ContainsKey("additionalProperties") ? $"Dictionary<string, {GetType((JObject)value["additionalProperties"]!)}>" : "object",
                _ => "object"
            } : value.ContainsKey("$ref") ? HandleClassName(value["$ref"]!.Value<string>()!) : "object";
        }

        static unsafe string ToUpperFirst(string str)
        {
            if (str == null) return null;
            string ret = string.Copy(str);
            fixed (char* ptr = ret)
                *ptr = char.ToUpper(*ptr);
            return ret;
        }
    }
}
