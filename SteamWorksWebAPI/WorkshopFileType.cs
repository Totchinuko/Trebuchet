using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamWorksWebAPI
{
    public enum WorkshopFileType
    {
        First = 0, //Only used for enumerating.
        Community = 0, //Normal Workshop item that can be subscribed to.
        Microtransaction = 1, //Workshop item that is meant to be voted on for the purpose of selling in-game. (See: Curated Workshop)
        Collection = 2, //A collection of Workshop items.
        Art = 3, //Artwork.
        Video = 4, //External video.
        Screenshot = 5, //Screenshot.
        Game = 6, //Unused, used to be for Greenlight game entries
        Software = 7, //Unused, used to be for Greenlight software entries.
        Concept = 8, //Unused, used to be for Greenlight concepts.
        WebGuide = 9, //Steam web guide.
        IntegratedGuide = 10, //Application integrated guide.
        Merch = 11, //Workshop merchandise meant to be voted on for the purpose of being sold.
        ControllerBinding = 12, //Steam Controller bindings.
        SteamworksAccessInvite = 13, //Only used internally in Steam.
        SteamVideo = 14, //Steam video.
        GameManagedItem = 15, //Managed completely by the game, not the user, and not shown on the web.
        Max = 16, //Only used for enumerating.
    }
}
