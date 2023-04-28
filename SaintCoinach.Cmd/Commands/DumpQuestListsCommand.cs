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

#pragma warning disable CS1998

namespace SaintCoinach.Cmd.Commands {
    public class DumpQuestListsCommand : ActionCommandBase {
        private ARealmReversed _Realm;

        public DumpQuestListsCommand(ARealmReversed realm)
            : base("redos", "Dump lists of quests in redos (new game plus)") {
            _Realm = realm;
        }

        public struct RedoChapter
        {
            public string GroupEn;
            public string GroupJa;
            public string ChapterNameEn;
            public string ChapterNameJa;
            public RedoQuest[] Quests;
        }

        public struct RedoQuest
        {
            public int Key;
            public string NameEn;
            public string NameJa;
        }

        public struct GroupJson
        {
            public string NameEn;
            public string NameJa;
            public List<ChapterJson> Chapters;
        }

        public struct ChapterJson
        {
            public string NameEn;
            public string NameJa;
            public List<RedoQuest> Quests;
        }

        public override async Task<bool> InvokeAsync(string paramList) {

            var questChain = _Realm.GameData.GetSheet<QuestRedo>();

            var result = new List<RedoChapter>();
            foreach (var chain in questChain)
            {
                var rc = new RedoChapter();
                _Realm.GameData.ActiveLanguage = Language.English;
                rc.GroupEn = chain.ChapterUI.UITab.Text;
                rc.ChapterNameEn = chain.ChapterUI.ChapterName;
                _Realm.GameData.ActiveLanguage = Language.Japanese;
                rc.GroupJa = chain.ChapterUI.UITab.Text;
                rc.ChapterNameJa = chain.ChapterUI.ChapterName;

                if (string.IsNullOrWhiteSpace(rc.GroupEn))
                {
                    // TODO: are these valid? or just incomplete?
                    continue;
                }

                var quests = new List<RedoQuest>();

                foreach (var quest in chain.Quests)
                {
                    _Realm.GameData.ActiveLanguage = Language.English;
                    var nameEn = quest.Name;
                    _Realm.GameData.ActiveLanguage = Language.Japanese;
                    var nameJa = quest.Name;
                    quests.Add(new RedoQuest { NameEn = nameEn, NameJa = nameJa, Key = quest.Key });
                }

                rc.Quests = quests.ToArray();
                result.Add(rc);
            }

            var resultsJson = new List<GroupJson>();

            var groups = result.GroupBy(x => x.GroupEn);
            foreach (var group in groups)
            {
                Console.WriteLine($"{group.First().GroupEn} | {group.First().GroupJa}");
                var groupJson = new GroupJson();
                groupJson.NameEn = group.First().GroupEn;
                groupJson.NameJa = group.First().GroupJa;
                groupJson.Chapters = new List<ChapterJson>();
                var redoChains = group.GroupBy(x => x.ChapterNameEn);
                foreach (var redoChain in redoChains)
                {
                    var chapterJson = new ChapterJson();
                    chapterJson.NameEn = redoChain.First().ChapterNameEn;
                    chapterJson.NameJa = redoChain.First().ChapterNameJa;
                    chapterJson.Quests = new List<RedoQuest>();
                    Console.WriteLine($"  {redoChain.First().ChapterNameEn} | {redoChain.First().ChapterNameJa}");

                    foreach (var q in redoChain.SelectMany(x => x.Quests))
                    {
                        chapterJson.Quests.Add(new RedoQuest() {Key = q.Key, NameEn = q.NameEn, NameJa = q.NameJa});
                        Console.WriteLine($"      {q.NameEn}");
                    }
                    groupJson.Chapters.Add(chapterJson);
                }
                resultsJson.Add(groupJson);
            }
            
            using var writer = File.CreateText($@"c:\temp\redo-quest-keys.json");
            JsonSerializer.Create(new JsonSerializerSettings { Formatting = Formatting.Indented })
                .Serialize(writer, resultsJson);

            return true;
        }
    }
}
