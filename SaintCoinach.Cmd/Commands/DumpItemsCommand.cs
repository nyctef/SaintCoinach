using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SaintCoinach.Xiv;
using Tharga.Toolkit.Console;
using Tharga.Toolkit.Console.Command;
using Tharga.Toolkit.Console.Command.Base;

#pragma warning disable CS1998

namespace SaintCoinach.Cmd.Commands {
    public class DumpItemsCommand : ActionCommandBase {
        private ARealmReversed _Realm;

        public DumpItemsCommand(ARealmReversed realm)
            : base("items", "Export all data (default), or only specific data files, seperated by spaces.") {
            _Realm = realm;
        }

        public override async Task<bool> InvokeAsync(string paramList)
        {

            var equipSlots = _Realm.GameData.EquipSlots;
            var items = _Realm.GameData.Items;
            var baseParams = _Realm.GameData.GetSheet<BaseParam>();
            var tenacity = baseParams.Single(x => x.Name == "Tenacity");
            // Console.WriteLine(tenacity.Description);
            //
            // var item = (Item)items[4345];
            // foreach (var param in item.BaseParams)
            // {
            //     Console.WriteLine(param.Name);
            // }
            //
            // var ilvl = item.ItemLevel;
            // Console.WriteLine(ilvl.Key);
            //
            // Console.WriteLine(item.BaseParams.Contains(tenacity));

            foreach (var item in items.OfType<Item>()
                         .Where(x => x.ItemLevel.Key < 50 && x.BaseParams.Contains(tenacity)).OrderBy(
                             x => x.ItemLevel.Key))
            {
                Console.WriteLine($"{item.ItemLevel} {item.Name}");
            }
            

            return true;
        }
    }
}
