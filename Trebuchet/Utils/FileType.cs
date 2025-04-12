using Avalonia.Platform.Storage;

namespace Trebuchet.Utils
{
    public static class FileType
    {
        public static readonly FilePickerFileType Json = new(@"Json Text")
        {
            MimeTypes = [@"application/json"],
            Patterns = [@"*.json"],
        };
        
        public static readonly FilePickerFileType Pak = new (@"Unreal Pak")
        {
            Patterns = [@"*.pak"],
            MimeTypes = [@"application/octet-stream"],
        };
        
        public static readonly FilePickerFileType Txt = new (@"Plain Text")
        {
            Patterns = [@"*.txt"],
            MimeTypes = [@"text/plain"],
        };
    }
}