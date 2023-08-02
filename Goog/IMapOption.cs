using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goog
{
    public interface IMapOption
    {
        [Option("map", HelpText = "Choose a specific map by its pak path or its shortcut: exile, siptah, savage, sapphire")]
        public string? map { get; set; }

        public static string? ProcessMap(IMapOption option)
        {
            if (option.map == null) 
                return null;
            switch (option.map)
            {
                case "exile":
                    return "/Game/Maps/ConanSandbox/ConanSandbox";
                case "siptah":
                    return "/Game/DLC_EXT/DLC_Siptah/Maps/DLC_Isle_of_Siptah";
                case "savage":
                    return "/Game/Mods/Savage_Wilds/Savage_Wilds";
                case "sapphire":
                    return "/Game/Mods/LCDATest_mapping/LCDAMap";
                default:
                    return option.map;
            }
        }
    }
}
