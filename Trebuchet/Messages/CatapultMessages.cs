using System.Collections;
using System.Collections.Generic;

namespace Trebuchet
{
    public class CatapulServersMessage
    { }

    public class CatapultClientMessage : CatapultMessage
    {
        public readonly bool isBattleEye;
        public readonly string modlist;
        public readonly string profile;

        public CatapultClientMessage(string profile, string modlist, bool isBattleEye)
        {
            this.isBattleEye = isBattleEye;
            this.modlist = modlist;
            this.profile = profile;
        }
    }

    public abstract class CatapultMessage
    {
    }

    public class CatapultServerMessage : CatapultMessage
    {
        public readonly IEnumerable<(string modlist, string profile, int intance)> instances;

        public CatapultServerMessage(IEnumerable<(string modlist, string profile, int intance)> instances)
        {
            this.instances = instances;
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