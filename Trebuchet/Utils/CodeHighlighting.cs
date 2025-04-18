using System;
using System.IO;
using System.Xml;
using AvaloniaEdit.Highlighting;

namespace Trebuchet.Utils;

public static class CodeHighlighting
{
    public static void RegisterHighlight(string file, string name, params string[] extensions)
    {
        // Load our custom highlighting definition
        IHighlightingDefinition customHighlighting;
        using (Stream s = typeof(UIConfig)
                              .Assembly
                              .GetManifestResourceStream(file)
                          ?? throw new Exception(@$"Highlighting not found ({file})")
              ) 
        {
            using (XmlReader reader = new XmlTextReader(s)) 
            {
                customHighlighting = AvaloniaEdit.Highlighting.Xshd.
                    HighlightingLoader.Load(reader, HighlightingManager.Instance);
            }
        }
        // and register it in the HighlightingManager
        HighlightingManager.Instance.RegisterHighlighting(name, extensions, customHighlighting);
    }
}