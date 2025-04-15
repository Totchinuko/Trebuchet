using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using tot_lib;

namespace TrebuchetUtils;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(CrashHandlerPayload))]
public partial class CrashHandlerPayloadJsonContext : JsonSerializerContext
{
}

public class CrashHandlerPayload(Exception ex)
{
    public string Message { get; } = ex.Message;
    public List<string> CallStack { get; } = ex.GetAllExceptions().Split(Environment.NewLine).ToList();
    public string OperatingSystem { get; } = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
    public string ProcessPath { get; } = Environment.ProcessPath ?? string.Empty;
    public bool RunAs { get; } = ProcessUtil.IsProcessElevated();
    public string TrebuchetVersion { get; } = ProcessUtil.GetAppVersion().ToString();
}