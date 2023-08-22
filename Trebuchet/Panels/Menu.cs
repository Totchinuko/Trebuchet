using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trebuchet
{
    public class Menu
    {
        public List<MenuElement> Bottom { get; set; } = new List<MenuElement>();

        public List<MenuElement> Top { get; set; } = new List<MenuElement>();
    }
}