using SQLite4Unity3d;

[System.Serializable]
public class Option
{
    [PrimaryKey, AutoIncrement]
    public int IdOpt { get; set; }
    
    public string LibOpt { get; set; }
    
    public bool IsTrue { get; set; }
    
    public string TitreQuest { get; set; }
    
    // Propriété de navigation (non stockée en base)
    [Ignore]
    public Question Question { get; set; }
    
    public override string ToString()
    {
        return string.Format("[Option: IdOpt={0}, LibOpt={1}, IsTrue={2}, TitreQuest={3}]", 
            IdOpt, LibOpt, IsTrue, TitreQuest);
    }
}