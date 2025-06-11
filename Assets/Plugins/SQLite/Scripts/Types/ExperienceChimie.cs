using SQLite4Unity3d;

[System.Serializable]
public class ExperienceChimie
{
    [PrimaryKey, AutoIncrement]
    public int IdExp { get; set; }
    
    public string TitreExp { get; set; }
    
    public string DescriptionExp { get; set; }
    
    public string Duree { get; set; }
    
    public int IdCategNiv { get; set; }
    
    public int IdCategTyp { get; set; }
    
    // Propriétés de navigation (non stockées en base)
    [Ignore]
    public CategorieNiveau CategorieNiveau { get; set; }
    
    [Ignore]
    public CategorieType CategorieType { get; set; }
    
    public override string ToString()
    {
        return string.Format("[ExperienceChimie: IdExp={0}, TitreExp={1}, DescriptionExp={2}, Duree={3}, IdCategNiv={4}, IdCategTyp={5}]", 
            IdExp, TitreExp, DescriptionExp, Duree, IdCategNiv, IdCategTyp);
    }
}