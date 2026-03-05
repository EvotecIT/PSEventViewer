using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace EventViewerX.Tests;

public class TestRuleSourceGuards
{
    [Fact]
    public void RuleSources_DoNotUseDirectEventObjectDataIndexer()
    {
        Regex directIndexerPattern = new(@"_eventObject\s*\.\s*Data\s*\[", RegexOptions.Compiled);
        IReadOnlyList<string> offenders = FindRuleSourceOffenders(directIndexerPattern);

        Assert.True(
            offenders.Count == 0,
            "Direct _eventObject.Data[...] usage is forbidden in rules. Use TryGetDataValue/GetDataValueOrEmpty. Offenders:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, offenders));
    }

    [Fact]
    public void RuleSources_DoNotUseDirectDataTryGetValueCalls()
    {
        Regex directTryGetValuePattern = new(@"\b(?:_eventObject|eventObject)\s*\.\s*Data\s*\.\s*TryGetValue\s*\(", RegexOptions.Compiled);
        IReadOnlyList<string> offenders = FindRuleSourceOffenders(directTryGetValuePattern);

        Assert.True(
            offenders.Count == 0,
            "Direct eventObject.Data.TryGetValue(...) is forbidden in rules. Use TryGetDataValue instead. Offenders:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, offenders));
    }

    private static IReadOnlyList<string> FindRuleSourceOffenders(Regex pattern)
    {
        string rulesDirectory = ResolveRulesDirectory();
        string repositoryRoot = Directory.GetParent(Directory.GetParent(rulesDirectory)!.FullName)!.FullName;

        var offenders = new List<string>();
        foreach (string filePath in Directory.EnumerateFiles(rulesDirectory, "*.cs", SearchOption.AllDirectories))
        {
            string[] lines = File.ReadAllLines(filePath);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.TrimStart();
                if (trimmed.StartsWith("//", StringComparison.Ordinal) ||
                    trimmed.StartsWith("/*", StringComparison.Ordinal) ||
                    trimmed.StartsWith("*", StringComparison.Ordinal))
                {
                    continue;
                }

                if (!pattern.IsMatch(line))
                {
                    continue;
                }

                string relativePath = Path.GetRelativePath(repositoryRoot, filePath);
                offenders.Add($"{relativePath}:{i + 1}: {trimmed}");
            }
        }

        return offenders;
    }

    private static string ResolveRulesDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            string candidate = Path.Combine(current.FullName, "Sources", "EventViewerX", "Rules");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Unable to locate Sources/EventViewerX/Rules from test base directory.");
    }
}
