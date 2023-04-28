using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

            try
            {
                Directory.Delete(@"c:\temp\all-quest-texts\", true);
            }
            catch (DirectoryNotFoundException) {}
            catch (IOException) {}

            Directory.CreateDirectory(@"c:\temp\all-quest-texts\");
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

                using var writer = File.CreateText($@"c:\temp\all-quest-texts\{q.Key}.json");
                JsonSerializer.Create(new JsonSerializerSettings { Formatting = Formatting.Indented })
                    .Serialize(writer, q);
            }

            return true;
        }

        private DSQ ParseQuest(Quest quest)
        {
            var internalQuestIdNum = quest.Id.ToString().Split('_')[1];
            var items = _Realm.GameData.GetSheet<Item>();
            var eventItems = _Realm.GameData.GetSheet<EventItem>();
            var eobjs = _Realm.GameData.GetSheet<EObj>();
            _Realm.GameData.ActiveLanguage = Language.English;
            var nameEn = quest.Name;
            _Realm.GameData.ActiveLanguage = Language.Japanese;
            var nameJa = quest.Name;

            var result = new DSQ();
            result.Key = quest.Key;
            result.InternalId = quest.Id;
            result.PreviousQuests = quest.Requirements.PreviousQuest.PreviousQuests.Select(x => x.Key).ToList();
            result.NextQuests = new List<int>();
            result.NameEn = nameEn;
            result.NameJa = nameJa;

            // TODO: this would be way better with valuetuples and destructuring, ie:
            // (result.TodosEn, result.JournalEn, result.DialogEn) = CollectTextData(...)
            _Realm.GameData.ActiveLanguage = Language.English;
            var textDataEn = _Realm.GameData.GetSheet($"quest/{internalQuestIdNum.Substring(0, 3)}/{quest.Id}");
            var parsedEn = CollectTextData(textDataEn, items, eventItems, eobjs);
            result.TodosEn = parsedEn.Item1;
            result.JournalEn = parsedEn.Item2;
            result.DialogueEn = parsedEn.Item3;
            _Realm.GameData.ActiveLanguage = Language.Japanese;
            var textDataJa = _Realm.GameData.GetSheet($"quest/{internalQuestIdNum.Substring(0, 3)}/{quest.Id}");
            var parsedJa = CollectTextData(textDataJa, items, eventItems, eobjs);
            result.TodosJa = parsedJa.Item1;
            result.JournalJa = parsedJa.Item2;
            result.DialogueJa = parsedJa.Item3;
            return result;
        }

        private Tuple<List<string>, List<string>, List<DialogLine>> CollectTextData(IXivSheet<XivRow> textData,
            IXivSheet<Item> items, IXivSheet<EventItem> eventItems, IXivSheet<EObj> eobjs)
        {
            var todos = new List<string>();
            var journal = new List<string>();
            var dialog = new List<DialogLine>();

            foreach (var line in textData)
            {
                var rowId = (XivString)line[0];
                var rowIdParts = rowId.ToString().Split('_');
                var text = (string)(XivString)line[1];
                text = ReplacePlaceholders(text, items, eventItems, eobjs);
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
        
        private static Regex EmphasisRegex = new Regex("<Emphasis>([^<]+)</Emphasis>", RegexOptions.Compiled);
        private static Regex IfElseRegex = new Regex(@"<If\(.+?\)>(.*?)<Else/>(.*?)</If>", RegexOptions.Compiled | RegexOptions.Singleline);
        private static Regex SheetRegex = new Regex(@"<Sheet\((Item|EventItem|EObj),\s*(\d+),0\)/>", RegexOptions.Compiled);
        private static Regex UiGlowRegex = new Regex(@"<UIForeground>F201F4</UIForeground><UIGlow>F201F5</UIGlow>(.*?)<UIGlow>01</UIGlow><UIForeground>01</UIForeground>", RegexOptions.Compiled);

        private string ReplacePlaceholders(string text, IXivSheet<Item> items, IXivSheet<EventItem> eventItems,
            IXivSheet<EObj> eobjs)
        {
            text = text
                    .Replace("<Split(<Highlight>ObjectParameter(1)</Highlight>, ,1)/>", "[Firstname]")
                    .Replace("<Split(<Highlight>ObjectParameter(1)</Highlight>, ,2)/>", "[Lastname]")
                    .Replace("<Highlight>ObjectParameter(1)</Highlight>", "[Fullname]")
                    .Replace("<Highlight>ObjectParameter(55)</Highlight>", "[Chocoboname]")
                    .Replace("<If(Equal(PlayerParameter(68),10))>an <Sheet(ClassJob,PlayerParameter(68),0)/><Else/><If(Equal(PlayerParameter(68),14))>an <Sheet(ClassJob,PlayerParameter(68),0)/><Else/><If(Equal(PlayerParameter(68),5))>an <Sheet(ClassJob,PlayerParameter(68),0)/><Else/><If(Equal(PlayerParameter(68),26))>an <Sheet(ClassJob,PlayerParameter(68),0)/><Else/><If(Equal(PlayerParameter(68),33))>an <Sheet(ClassJob,PlayerParameter(68),0)/><Else/>a <Sheet(ClassJob,PlayerParameter(68),0)/></If></If></If></If></If>", "a [Crafter]")
                ;
            text = EmphasisRegex.Replace(text, "*$1*");
            text = SheetRegex.Replace(text, match =>
            {
                var sheetName = match.Groups[1].Value;
                var itemId = int.Parse(match.Groups[2].Value);
                return sheetName switch
                {
                    "Item" => items[itemId].Name.ToString(),
                    "EventItem" => eventItems[itemId].Name.IsEmpty ? eventItems[itemId].Singular : eventItems[itemId].Name,
                    // TODO
                    "EObj" => $"[dummy {eobjs[itemId].Data}]",
                    _ => throw new Exception("unrecognised sheet "+sheetName)
                };
            });
            text = UiGlowRegex.Replace(text, "~$1~");
            // quick hack to handle nested if/else chains
            var timeout = 20;
            while (text.Contains("<If") && timeout > 0)
            {
                text = IfElseRegex.Replace(text, "[$1|$2]");
                timeout--;
            }
            return text;
        }
    }
}