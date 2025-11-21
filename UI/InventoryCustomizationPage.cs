using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu;

namespace MetroidvaniaMode.UI;

public class InventoryCustomizationPage : Page
{
    private MenuLabel Title;

    public InventoryCustomizationPage(Menu.Menu menu, MenuObject owner, string name, int index, List<SelectableMenuObject> extraSelectables) : base(menu, owner, name, index)
    {
        Title = new(menu, this, menu.Translate("Inventory"), new(500, 500), new(500, 50), true);
        subObjects.Add(Title);

        selectables.AddRange(extraSelectables);
    }
}
