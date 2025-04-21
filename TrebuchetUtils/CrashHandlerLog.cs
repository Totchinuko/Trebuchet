using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TrebuchetUtils;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(CrashHandlerLog))]
public partial class CrashHandlerLogJsonContext : JsonSerializerContext
{
}

public class CrashHandlerLog
{
    public Dictionary<string, string> Properties { get; init; } = [];
    public string Message { get; init; } = string.Empty;
    public string LogLevel { get; init; } = string.Empty;
    public DateTime Date { get; init; } = DateTime.MinValue;
}