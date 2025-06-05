using SQLite4Unity3d;

public class Experiment
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    
    public string Description { get; set; }
    
    public string Duration { get; set; }
    
    public string Category { get; set; } // Optionnel : pour catégoriser les expériences
    
    public bool IsActive { get; set; } = true; // Pour pouvoir activer/désactiver des expériences
    
    public override string ToString()
    {
        return string.Format("[Experiment: Id={0}, Description={1}, Duration={2}, Category={3}, IsActive={4}]", 
            Id, Description, Duration, Category, IsActive);
    }
}