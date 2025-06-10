using SQLite4Unity3d;

[System.Serializable]
public class CategorieNiveau
{
    [PrimaryKey, AutoIncrement]
    public int IdCategNiv { get; set; }
    
    public string TitreCategNiv { get; set; }
    
    public string DescriptionCategNiv { get; set; }
    
    public override string ToString()
    {
        return string.Format("[CategorieNiveau: IdCategNiv={0}, TitreCategNiv={1}, DescriptionCategNiv={2}]", 
            IdCategNiv, TitreCategNiv, DescriptionCategNiv);
    }
}