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

namespace BlueCheese.App.Services
{
    public static class ConfigCodeGen
    {
        public const string _gencodeStartString = "// GEN CODE START";
        public const string _gencodeEndString = "// GEN CODE END";
        public const string _declarationTemplate = "public {0} {1} => ({0})ConfigRegistry.Instance.{2}(\"{1}\");";

        public static void Generate(ConfigAsset asset, string folderPath)
        {
            if (asset == null || string.IsNullOrWhiteSpace(folderPath))
            {
                return;
            }

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string filename = Path.Combine(Application.dataPath, folderPath, asset.name + ".cs");

            if (!File.Exists(filename))
            {
                GenerateConfigScript(filename);
            }

            IEnumerable<string> generatedLines = GenerateConfigLines(asset.Items);

            InsertGeneratedCode(filename, generatedLines);
        }

        private static void GenerateConfigScript(string filename)
        {
            var sb = new StringBuilder()
                .AppendLine("using BlueCheese.App.Services;")
                .AppendLine()
                .AppendLine("public partial class Config")
                .AppendLine("{")
                .AppendLine($"    {_gencodeStartString}")
                .AppendLine($"    {_gencodeEndString}")
                .AppendLine("}");

            using var writer = File.CreateText(filename);
            writer.Write(sb.ToString());
            writer.Close();

            AssetDatabase.Refresh();
        }

        private static IEnumerable<string> GenerateConfigLines(ConfigItem[] items)
        {
            return items
                .Select(item => GenerateConfigLine(item))
                .Where(str => str != null);
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
                ConfigItem.ValueType.String => nameof(ConfigRegistry.GetStringValue),
                ConfigItem.ValueType.Int => nameof(ConfigRegistry.GetIntValue),
                ConfigItem.ValueType.Float => nameof(ConfigRegistry.GetFloatValue),
                ConfigItem.ValueType.Boolean => nameof(ConfigRegistry.GetBoolValue),
                ConfigItem.ValueType.Object => nameof(ConfigRegistry.GetObjectValue),
                _ => null,
            };
        }

        private static void InsertGeneratedCode(string filename, IEnumerable<string> generatedLines)
        {
            List<string> lines = File.ReadAllLines(filename).ToList();

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
            File.WriteAllLines(filename, lines);

            // Refresh assets
            AssetDatabase.Refresh();
        }
    }
}
