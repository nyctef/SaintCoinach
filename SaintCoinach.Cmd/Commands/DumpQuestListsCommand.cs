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
    public class DumpQuestListsCommand : ActionCommandBase {
        private ARealmReversed _Realm;

        public DumpQuestListsCommand(ARealmReversed realm)
            : base("redos", "Dump lists of quests in redos (new game plus)") {
            _Realm = realm;
        }

        public override async Task<bool> InvokeAsync(string paramList) {

            var questChain = _Realm.GameData.GetSheet<QuestRedo>();

            foreach (var chain in questChain)
            {
                Console.WriteLine(chain.Key);
                Console.WriteLine(chain.FinalQuest?.Name ?? "<null>");
                Console.WriteLine($" {chain.ChapterUI.UITab.Text}");
                Console.WriteLine(
                    $"{chain.ChapterUI.ChapterName} {chain.ChapterUI.ChapterPart} {chain.ChapterUI.Description}");
                foreach (var quest in chain.Quests)
                {
                    Console.WriteLine($"  {quest.Key} {quest.Name}");
                }
            }

            return true;
        }
    }
}
