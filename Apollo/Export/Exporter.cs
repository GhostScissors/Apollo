using Apollo.Export.VOs;

namespace Apollo.Export;

public static class Exporter
{
    public static VoiceLinesExporter VoiceLines { get; } = new();
}