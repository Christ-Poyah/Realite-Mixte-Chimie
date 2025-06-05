using SQLite4Unity3d;

public class Experiences  {

	[PrimaryKey, AutoIncrement]
	public int Id_Experiences { get; set; }
	public int Id_Categorie { get; set; }
	public string Libelle { get; set; }
	public override string ToString ()
	{
		return string.Format ("[Experiences: Id_Experiences={0}, Id_Categorie={1},  Libelle={2}]", Id_Experiences, Id_Categorie ,Libelle);
	}
}
