using SQLite4Unity3d;

[System.Serializable]
public class CategorieType
{
    [PrimaryKey, AutoIncrement]
    public int IdCategTyp { get; set; }
    
    public string TitreCategTyp { get; set; }
    
    public string DescriptionCategTyp { get; set; }
    
    public override string ToString()
    {
        return string.Format("[CategorieType: IdCategTyp={0}, TitreCategTyp={1}, DescriptionCategTyp={2}]", 
            IdCategTyp, TitreCategTyp, DescriptionCategTyp);
    }
}