using System.Collections;
using System.Collections.Generic;

namespace Trebuchet
{
    public class CatapulServersMessage
    { }

    public class CatapultClientMessage : CatapultMessage
    {
        public readonly bool isBattleEye;

        public CatapultClientMessage(string modlist, string profile, bool isBattleEye) : base(modlist, profile)
        {
            this.isBattleEye = isBattleEye;
        }
    }

    public abstract class CatapultMessage
    {
        public readonly string modlist;
        public readonly string profile;

        public CatapultMessage(string modlist, string profile)
        {
            this.modlist = modlist;
            this.profile = profile;
        }
    }

    public class CatapultServerMessage : CatapultMessage
    {
        public readonly int instance;

        public CatapultServerMessage(string modlist, string profile, int instance) : base(modlist, profile)
        {
            this.instance = instance;
        }
    }

    public class VerifyFilesMessage
    {
        public readonly IEnumerable<ulong> modlist;

        public VerifyFilesMessage(IEnumerable<ulong> modlist)
        {
            this.modlist = modlist;
        }
    }
}