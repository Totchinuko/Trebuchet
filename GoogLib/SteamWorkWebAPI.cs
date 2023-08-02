using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goog
{
    public class SteamWorkWebAPI
    {
        private string apikey;

        public SteamWorkWebAPI(string apikey) 
        {
            this.apikey = apikey;
        }

        public Dictionary<string, PublishedFile> GetPublishedFiles(List<string> IDs)
        {
            throw new NotImplementedException();
        }
    }
}
