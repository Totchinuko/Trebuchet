using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogGUI
{
    public class Menu
    {
        public List<MenuElement> Top { get; set; } = new List<MenuElement>();
        public List<MenuElement> Bottom { get; set; } = new List<MenuElement>();

        public IEnumerable<Panel> GetPanels()
        {
            return Top.Where(x => x is Panel).Cast<Panel>()
                .Concat(Bottom.Where(x => x is Panel).Cast<Panel>());
        }

    }
}
