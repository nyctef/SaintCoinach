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
using SaintCoinach.Xiv.Collections;

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
                var rowIdParts = ((XivString)row.GetRaw(0)).ToString().Split('_');
                if (rowIdParts.Length != 6)
                {
                    continue;
                }
                
                Console.WriteLine($"{rowIdParts[3]}");
            }
            
            for (int i = 0; i < 64; i++)
            {
                var enpcId = quest.AsInt32("Listener", i);
                if (enpcId == 0)
                {
                    break;
                }
                Console.WriteLine(GetNpcOrObjectName(enpcs, objs, enpcId));
            }

            for (int i = 0; i < 50; i++)
            {
                var scriptInstruction = quest.AsString("Script{Instruction}", i);
                var scriptArg = quest.AsInt32("Script{Arg}", i);
                var scripter = GetNpcOrObjectName(enpcs, objs, scriptArg);
                Console.WriteLine($"{scripter}: {scriptInstruction}");
            }
            
            

            return true;
        }

        private static string GetNpcOrObjectName(ENpcCollection enpcs, IXivSheet<EObj> objs, int objOrNpcId)
        {
            try
            {
                var enpc = enpcs[objOrNpcId];
                return ($"{objOrNpcId}: {enpc.Singular}");
            }
            catch (Exception)
            {
            }

            try
            {

                var obj = objs[objOrNpcId];
                return ($"dummy {obj.Key}");
            }
            catch (Exception)
            {
                
            }

            return "unknown id " + objOrNpcId;
        }
    }
}
