using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Tharga.Toolkit.Console;
using Tharga.Toolkit.Console.Command;
using Tharga.Toolkit.Console.Command.Base;

using SaintCoinach;
using SaintCoinach.Ex;
using SaintCoinach.Text;
using SaintCoinach.Xiv;

#pragma warning disable CS1998

namespace SaintCoinach.Cmd.Commands {
    public class DumpSingleQuestCommand : ActionCommandBase {
        private ARealmReversed _Realm;

        public DumpSingleQuestCommand(ARealmReversed realm)
            : base("dsq", "Dump a named quest") {
            _Realm = realm;
        }

        public override async Task<bool> InvokeAsync(string paramList) {

            if (!int.TryParse(paramList, out var questId))
            {
                throw new Exception($"a quest id is required (failed to parse [{paramList}])");
            }

            // _Realm.GameData.ActiveLanguage = Language.Japanese;
            
            var quests = _Realm.GameData.GetSheet<Quest>();
            var enpcs = _Realm.GameData.ENpcs;
            var objs = _Realm.GameData.GetSheet<EObj>();
            var quest = quests[questId];

            Console.WriteLine($"{quest.Id}: {quest.Name}");
            var internalQuestIdNum = quest.Id.ToString().Split('_')[1];

            var textData = _Realm.GameData.GetSheet($"quest/{internalQuestIdNum.Substring(0, 3)}/{quest.Id}");

            foreach (var row in textData)
            {
                Console.WriteLine($"{row[0]}: {row[1]}");
            }
            
            for (int i = 0; i < 64; i++)
            {
                var enpcId = quest.AsInt32("Listener", i);
                if (enpcId == 0)
                {
                    break;
                }
                try
                {
                    var enpc = enpcs[enpcId];
                    Console.WriteLine($"{enpcId}: {enpc.Singular}");
                }
                catch (Exception)
                {
                    var obj = objs[enpcId];
                    Console.WriteLine($"dummy {obj.Key}");
                }
            }
            

            return true;
        }
    }
}
