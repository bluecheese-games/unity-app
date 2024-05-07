//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.Unity.App.Services.Editor
{
    public static class ConfigCodeGen
    {
        public const string _gencodeStartString = "// GEN CODE START";
        public const string _gencodeEndString = "// GEN CODE END";
        public const string _declarationTemplate = "public {0} {1} => ({0}){2}(\"{1}\");";

        public static void Generate(ConfigAsset asset)
        {
            if (asset == null || string.IsNullOrWhiteSpace(asset.GeneratedFolder))
            {
                return;
            }

            if (!asset.GeneratedFileExists)
            {
                GenerateConfigFile(asset);
            }

            IEnumerable<string> generatedLines = GenerateConfigLines(asset.Items);

            InsertGeneratedCode(asset, generatedLines);
        }

        private static void GenerateConfigFile(ConfigAsset asset)
        {
            var sb = new StringBuilder()
                .AppendLine($"namespace {typeof(Config).Namespace}")
                .AppendLine("{")
                .AppendLine("    public partial class Config")
                .AppendLine("    {")
                .AppendLine($"        {_gencodeStartString}")
                .AppendLine($"        {_gencodeEndString}")
                .AppendLine("    }")
                .AppendLine("}");

            using var writer = File.CreateText(asset.GeneratedFilePath);
            writer.Write(sb.ToString());
            writer.Close();

            AssetDatabase.Refresh();
        }

        private static IEnumerable<string> GenerateConfigLines(ConfigItem[] items)
        {
            return items.Select(item => GenerateConfigLine(item)).Where(str => str != null);
        }

        private static string GenerateConfigLine(ConfigItem item)
        {
            Type type = GetConfigType(item);
            if (type == null)
            {
                return null;
            }

            string itemName = item.Key;
            string funcName = GetConfigFuncName(item);
            return string.Format(_declarationTemplate, type.ToString(), itemName, funcName);
        }

        private static Type GetConfigType(ConfigItem item)
        {
            return item.Type switch
            {
                ConfigItem.ValueType.String => typeof(string),
                ConfigItem.ValueType.Int => typeof(int),
                ConfigItem.ValueType.Float => typeof(float),
                ConfigItem.ValueType.Boolean => typeof(bool),
                ConfigItem.ValueType.Object => item.ObjectValue.GetType(),
                _ => null,
            };
        }

        private static string GetConfigFuncName(ConfigItem item)
        {
            return item.Type switch
            {
                ConfigItem.ValueType.String => "GetStringValue",
                ConfigItem.ValueType.Int => "GetIntValue",
                ConfigItem.ValueType.Float => "GetFloatValue",
                ConfigItem.ValueType.Boolean => "GetBoolValue",
                ConfigItem.ValueType.Object => "GetObjectValue",
                _ => null,
            };
        }

        private static void InsertGeneratedCode(ConfigAsset asset, IEnumerable<string> generatedLines)
        {
            string filePath = asset.GeneratedFilePath;
            List<string> lines = File.ReadAllLines(filePath).ToList();

            // Find the index of the line after which you want to insert the new line
            int startLineIndex = lines.FindIndex(s => s.Contains(_gencodeStartString)) + 1;
            int endLineIndex = lines.FindIndex(s => s.Contains(_gencodeEndString));

            // Remove previous generated code
            lines.RemoveRange(startLineIndex, endLineIndex - startLineIndex);

            // Get the indentation
            string line = lines[startLineIndex];
            string indentation = line.Substring(0, line.Length - line.TrimStart().Length);

            // Insert the new lines
            lines.InsertRange(startLineIndex, generatedLines.Select(line => $"{indentation}{line}"));

            // Write the list of strings back to the file, overwriting the original content
            File.WriteAllLines(filePath, lines);

            // Refresh assets
            AssetDatabase.Refresh();
        }
    }
}
