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
    public class DumpQuestsCommand : ActionCommandBase {
        private ARealmReversed _Realm;

        public DumpQuestsCommand(ARealmReversed realm)
            : base("msqs", "Dump MSQ quests, quest links and quest text") {
            _Realm = realm;
        }

        public override async Task<bool> InvokeAsync(string paramList) {

            var quests = _Realm.GameData.GetSheet<Quest>();
            var endwalker = quests[70_000];
            var pending = new Queue<Quest>();
            var seen = new HashSet<XivString>();
            pending.Enqueue(endwalker);
            while (pending.Any())
            {
                var next = pending.Dequeue();
                if (seen.Contains(next.Id))
                {
                    continue;
                }
                Console.WriteLine($"{next.Id} {next.Name}");
                seen.Add(next.Id);
                if (!next.Requirements.PreviousQuest.PreviousQuests.Any())
                {
                    Console.WriteLine("^ is a starting quest");
                }
                foreach (var prereq in next.Requirements.PreviousQuest.PreviousQuests)
                {
                    Console.WriteLine($" > {prereq.Name}");
                    pending.Enqueue(prereq);
                }
            }

            return true;
        }
    }
}
