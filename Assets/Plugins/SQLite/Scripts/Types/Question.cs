using SQLite4Unity3d;

[System.Serializable]
public class Question
{
    [PrimaryKey]
    public string TitreQuest { get; set; }
    
    public string LibQuest { get; set; }
    
    public int IdExp { get; set; }
    
    // Propriété de navigation (non stockée en base)
    [Ignore]
    public ExperienceChimie Experience { get; set; }
    
    public override string ToString()
    {
        return string.Format("[Question: TitreQuest={0}, LibQuest={1}, IdExp={2}]", 
            TitreQuest, LibQuest, IdExp);
    }
}