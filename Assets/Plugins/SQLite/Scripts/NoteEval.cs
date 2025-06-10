
using SQLite4Unity3d;
using System;

[System.Serializable]
public class NoteEval
{
    [PrimaryKey, AutoIncrement]
    public int IdNote { get; set; }
    
    public float Valeur { get; set; }
    
    public string DateEval { get; set; }
    
    public int IdExp { get; set; }
    
    // Propriété de navigation (non stockée en base)
    [Ignore]
    public ExperienceChimie Experience { get; set; }
    
    public override string ToString()
    {
        return string.Format("[NoteEval: IdNote={0}, Valeur={1}, DateEval={2}, IdExp={3}]", 
            IdNote, Valeur, DateEval, IdExp);
    }
}