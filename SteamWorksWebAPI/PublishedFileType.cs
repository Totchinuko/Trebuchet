using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamWorksWebAPI
{
    public enum PublishedFileType
    {
        Items = 0, // Items.
        Collections = 1, // A collection of Workshop items.
        Art = 2, // Artwork.
        Videos = 3, // Videos.
        Screenshots = 4, // Screenshots.
        CollectionEligible = 5, // Items that can be put inside a collection.
        Games = 6, // Unused.
        Software = 7, // Unused
        Concepts = 8, // Unused
        GreenlightItems = 9, // Unused
        AllGuides = 10, // Guides.
        WebGuides = 11, // Steam web guide.
        IntegratedGuides = 12, // Application integrated guide.
        UsableInGame = 13,
        Merch = 14, // Workshop merchandise meant to be voted on for the purpose of being sold
        ControllerBindings = 15, // Steam Controller bindings.
        SteamworksAccessInvites = 16, // Used internally.
        Items_Mtx = 17, // Workshop items that can be sold in-game.
        Items_ReadyToUse = 18, // Workshop items that can be used right away by the user.
        WorkshopShowcase = 19, 
        GameManagedItems = 20, // Managed completely by the game, not the user, and not shown on the web.
    }
}
