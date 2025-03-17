using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trebuchet.Panels
{
    public class Menu
    {
        public List<MenuElement> Bottom { get; set; } = [];

        public List<MenuElement> Top { get; set; } = [];
    }
}