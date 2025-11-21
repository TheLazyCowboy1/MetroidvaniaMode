using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu;
using UnityEngine;

namespace MetroidvaniaMode.UI;

public class InventoryCustomizationPage : ChangeablePage
{
    private MenuLabel Title;

    public InventoryCustomizationPage(Menu.Menu menu, MenuObject owner, string name, int index, List<SelectableMenuObject> extraSelectables) : base(menu, owner, name, index, extraSelectables)
    {
        Title = new(menu, this, menu.Translate("Inventory"), new(500, 500), new(500, 50), true);
        subObjects.Add(Title);
    }
}
