using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamWorksWebAPI
{
    public class PlayerSummary
    {
        public string SteamID { get; set; } = string.Empty;
        public int CommunityVisibilityState { get; set; } = 0;
        public int ProfileState { get; set; } = 0;
        public string PersonaName { get; set; } = string.Empty;
        public int CommentPermission { get; set; } = 0;
        public string ProfileURL { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public string AvatarMedium { get; set; } = string.Empty;
        public string AvatarFull { get; set; } = string.Empty;
        public string AvatarHash { get; set; } = string.Empty;
        public ulong LastLogoff { get; set; } = 0;
        public int PersonaState { get; set; } = 0;
        public string PrimaryClanID { get; set; } = string.Empty;
        public ulong TimeCreated { get; set; } = 0;
        public int PersonaStateFlags { get; set; } = 0;
        public string LocCountryCode { get; set; } = string.Empty;
        public string LocStateCode { get; set; } = string.Empty;
        public int LocCityID { get; set; } = 0;
    }
}
