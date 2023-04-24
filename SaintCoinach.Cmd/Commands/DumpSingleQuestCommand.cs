using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
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

        public struct DSQ
        {
            public int Key;
            public string InternalId;
            public List<string> TodosEn;
            public List<string> JournalEn;
            public List<DialogLine> DialogueEn;
            public List<string> TodosJa;
            public List<string> JournalJa;
            public List<DialogLine> DialogueJa;
        }

        public struct DialogLine
        {
            public string InternalId;
            // will be the same placeholder for now until we can figure out how to get accurate NPC names
            public string Speaker;
            public string Text;
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


            var result = new DSQ();
            result.Key = questId;
            result.InternalId = quest.Id;

            // TODO: this would be way better with valuetuples and destructuring, ie:
            // (result.TodosEn, result.JournalEn, result.DialogEn) = CollectTextData(...)
            _Realm.GameData.ActiveLanguage = Language.English;
            var textDataEn = _Realm.GameData.GetSheet($"quest/{internalQuestIdNum.Substring(0, 3)}/{quest.Id}");
            var parsedEn = CollectTextData(textDataEn);
            result.TodosEn = parsedEn.Item1;
            result.JournalEn = parsedEn.Item2;
            result.DialogueEn = parsedEn.Item3;
            _Realm.GameData.ActiveLanguage = Language.Japanese;
            var textDataJa = _Realm.GameData.GetSheet($"quest/{internalQuestIdNum.Substring(0, 3)}/{quest.Id}");
            var parsedJa = CollectTextData(textDataJa);
            result.TodosJa = parsedJa.Item1;
            result.JournalJa = parsedJa.Item2;
            result.DialogueJa = parsedJa.Item3;
            
            Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));

            // foreach (var row in textData)
            // {
            //     var rowIdParts = ((XivString)row.GetRaw(0)).ToString().Split('_');
            //     if (rowIdParts.Length != 6)
            //     {
            //         continue;
            //     }
            //     
            //     Console.WriteLine($"{rowIdParts[3]}");
            // }
            
            // for (int i = 0; i < 64; i++)
            // {
            //     var enpcId = quest.AsInt32("Listener", i);
            //     if (enpcId == 0)
            //     {
            //         break;
            //     }
            //     Console.WriteLine(GetNpcOrObjectName(enpcs, objs, enpcId));
            // }

            // for (int i = 0; i < 50; i++)
            // {
            //     var scriptInstruction = quest.AsString("Script{Instruction}", i);
            //     var scriptArg = quest.AsInt32("Script{Arg}", i);
            //     var scripter = GetNpcOrObjectName(enpcs, objs, scriptArg);
            //     Console.WriteLine($"{scripter}: {scriptInstruction}");
            // }

            // for (int i = 0; i < 32; i++)
            // {
            //     var uintA = quest.AsInt32("QuestUInt8A", i);
            //     Console.WriteLine(uintA);
            // }
            // for (int i = 0; i < 32; i++)
            // {
            //     var uintB = quest.AsInt32("QuestUInt8B", i);
            //     Console.WriteLine(uintB);
            // }

            return true;
        }

        private Tuple<List<string>,  List<string> , List<DialogLine> > CollectTextData(IXivSheet<XivRow> textData)
        {
            var todos = new List<string>();
            var journal = new List<string>();
            var dialog = new List<DialogLine>();

            foreach (var line in textData)
            {
                var rowId = (XivString)line[0];
                var rowIdParts = rowId.ToString().Split('_');
                var text = (XivString)line[1];
                // TODO: newer framework and list patterns
                if (rowIdParts.Length == 5 && rowIdParts[0] == "TEXT" && rowIdParts[3] == "SEQ")
                {
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        continue; 
                    }
                    journal.Add(text);
                } else if (rowIdParts.Length == 5 && rowIdParts[0] == "TEXT" && rowIdParts[3] == "TODO")
                {
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        continue; 
                    }
                    todos.Add(text);
                } else if (rowIdParts.Length == 6 && rowIdParts[0] == "TEXT")
                {
                    var speaker = rowIdParts[3];
                    dialog.Add(new DialogLine { InternalId = rowId, Speaker = speaker, Text = text});
                }
                else
                {
                    throw new Exception($"Don't know how to handle text id {rowId}");
                }
            }

            return Tuple.Create(todos, journal, dialog);
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
