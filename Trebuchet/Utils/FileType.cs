﻿namespace Trebuchet
{
    public struct FileType
    {
        public string extention;
        public string name;

        public static FileType Json => new FileType
        {
            extention = "json",
            name = "Json Text"
        };

        public static FileType Pak => new FileType
        {
            extention = "pak",
            name = "Unreal Pak"
        };
        public static FileType Txt => new FileType
        {
            extention = "txt",
            name = "Plain Text"
        };
        public string Filter => $"{name} (*.{extention})|*.{extention}";
    }
}