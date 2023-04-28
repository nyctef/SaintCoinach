using System.Collections.Generic;
using SaintCoinach.Ex.Relational;

namespace SaintCoinach.Xiv;

public class QuestRedo : XivRow
{
    private readonly IXivSheet _Sheet;

    public QuestRedo(IXivSheet sheet, IRelationalRow sourceRow) : base(sheet, sourceRow)
    {
        _Sheet = sheet;
    }
    
    public Quest FinalQuest { get { return As<Quest>("FinalQuest"); } }
    
    public QuestRedoChapter Chapter { get { return As<QuestRedoChapter>("Chapter"); } }
    
    public QuestRedoChapterUI ChapterUI
    {
        get { return _Sheet.Collection.GetSheet<QuestRedoChapterUI>()[Chapter.Key]; }
    }

    public Quest[] Quests
    {
        get
        {
            var result = new List<Quest>();
            for (int i = 0; i < 32; i++)
            {
                var q = As<Quest>("Quest", i);
                if (q == null) continue;
                result.Add(q);
            }

            return result.ToArray();
        }
    }
}

public class QuestRedoChapterUI : XivRow
{
    public QuestRedoChapterUI(IXivSheet sheet, IRelationalRow sourceRow) : base(sheet, sourceRow)
    {
    }
    
    public QuestRedoChapterUITab UITab { get { return As<QuestRedoChapterUITab>("UITab"); } }
    public QuestRedoChapterUICategory Category { get { return As<QuestRedoChapterUICategory>("Category"); } }
    public string ChapterName { get { return AsString("ChapterName"); } }
    public string ChapterPart { get { return AsString("ChapterPart"); } }
    public string Description { get { return AsString("Transient"); } }
}

public class QuestRedoChapterUICategory : XivRow
{
    public QuestRedoChapterUICategory(IXivSheet sheet, IRelationalRow sourceRow) : base(sheet, sourceRow)
    {
    }
    
}

public class QuestRedoChapterUITab : XivRow
{
    public QuestRedoChapterUITab(IXivSheet sheet, IRelationalRow sourceRow) : base(sheet, sourceRow)
    {
    }
    public string Text { get { return AsString("Text"); } }
}

public class QuestRedoChapter : XivRow
{
    public QuestRedoChapter(IXivSheet sheet, IRelationalRow sourceRow) : base(sheet, sourceRow)
    {
    }
}