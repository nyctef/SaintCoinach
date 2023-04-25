using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Tharga.Toolkit.Console;
using Tharga.Toolkit.Console.Command;
using Tharga.Toolkit.Console.Command.Base;

using SaintCoinach;
using SaintCoinach.Ex;
using SaintCoinach.Text;
using SaintCoinach.Xiv;

#pragma warning disable CS1998

namespace SaintCoinach.Cmd.Commands {
    public class DumpAllQuestsCommand : ActionCommandBase {
        private ARealmReversed _Realm;

        public DumpAllQuestsCommand(ARealmReversed realm)
            : base("allq", "Dump text and prereqs for all quests") {
            _Realm = realm;
        }

        public struct DSQ
        {
            public int Key;
            public string InternalId;
            public List<int> PreviousQuests;
            public string NameEn;
            public string NameJa;
            public List<int> NextQuests;
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

            // _Realm.GameData.ActiveLanguage = Language.Japanese;
            
            var quests = _Realm.GameData.GetSheet<Quest>();
            var enpcs = _Realm.GameData.ENpcs;
            var objs = _Realm.GameData.GetSheet<EObj>();

            var nextQuestsByQuest = new Dictionary<int, List<int>>();
            foreach (var quest in quests)
            {
                var preReqQuests = quest.Requirements.PreviousQuest.PreviousQuests.Select(x => x.Key);
                foreach (var preReq in preReqQuests)
                {
                    // TODO: a defaultdict would be nice here
                    if (!nextQuestsByQuest.ContainsKey(preReq))
                    {
                        nextQuestsByQuest.Add(preReq, new List<int>());
                    }
                    nextQuestsByQuest[preReq].Add(quest.Key);
                }
            }

            var result = new Dictionary<int, DSQ>();
            
            foreach (var quest in quests)
            {
                if (quest.Id.IsEmpty)
                {
                    continue;
                }
                _Realm.GameData.ActiveLanguage = Language.English;
                
                var q = ParseQuest(quest);
                if (nextQuestsByQuest.TryGetValue(q.Key, out var nextQuests))
                {
                    q.NextQuests = nextQuests;
                }

                result.Add(q.Key, q);
            }

            using var writer = File.CreateText(@"c:\temp\all-quest-texts.json");
            JsonSerializer.Create(new JsonSerializerSettings { Formatting = Formatting.Indented })
                .Serialize(writer, result);

            return true;
        }

        private DSQ ParseQuest(Quest quest)
        {
            var internalQuestIdNum = quest.Id.ToString().Split('_')[1];
            _Realm.GameData.ActiveLanguage = Language.English;
            var nameEn = quest.Name;
            _Realm.GameData.ActiveLanguage = Language.Japanese;
            var nameJa = quest.Name;

            var result = new DSQ();
            result.Key = quest.Key;
            result.InternalId = quest.Id;
            result.PreviousQuests = quest.Requirements.PreviousQuest.PreviousQuests.Select(x => x.Key).ToList();
            result.NameEn = nameEn;
            result.NameJa = nameJa;

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
            return result;
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
                }
                else if (rowIdParts.Length == 5 && rowIdParts[0] == "TEXT" && rowIdParts[3] == "POP" &&
                         rowIdParts[4] == "MESSAGE")
                {
                    // skipping these for now
                    continue;
                }
                else if (rowIdParts.Length == 5 && rowIdParts[0] == "TEXT" && rowIdParts[3] == "ACCESS" &&
                         rowIdParts[4] == "MESSAGE")
                {
                    // skipping these for now
                    continue;
                }
                else if (rowIdParts.Length == 7 && rowIdParts[0] == "TEXT" && (rowIdParts[4].StartsWith("Q") || rowIdParts[4].StartsWith("A")))
                {
                    // skipping these QAs for now
                    continue;
                }
                else if (rowIdParts.Length == 6 && rowIdParts[0] == "TEXT")
                {
                    var speaker = rowIdParts[3];
                    dialog.Add(new DialogLine { InternalId = rowId, Speaker = speaker, Text = text});
                }
                else
                {
                    // throw new Exception($"Don't know how to handle text id {rowId} with value {text}");
                    // Console.WriteLine($"Don't know how to handle text id {rowId} with value {text}");
                }
            }

            return Tuple.Create(todos, journal, dialog);
        }
    }
}