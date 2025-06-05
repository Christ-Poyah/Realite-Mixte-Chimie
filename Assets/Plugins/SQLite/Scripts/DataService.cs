using SQLite4Unity3d;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
#if !UNITY_EDITOR
using System.Collections;
using System.IO;
#endif

public class DataService
{
    private SQLiteConnection _connection;

    public DataService(string DatabaseName)
    {
#if UNITY_EDITOR
        var dbPath = string.Format(@"Assets/StreamingAssets/{0}", DatabaseName);
#else
        var filepath = string.Format("{0}/{1}", Application.persistentDataPath, DatabaseName);

        if (!File.Exists(filepath))
        {
            Debug.Log("Database not in Persistent path");

#if UNITY_ANDROID 
            var loadDb = new WWW("jar:file://" + Application.dataPath + "!/assets/" + DatabaseName);
            while (!loadDb.isDone) { }
            File.WriteAllBytes(filepath, loadDb.bytes);
#elif UNITY_IOS
            var loadDb = Application.dataPath + "/Raw/" + DatabaseName;
            File.Copy(loadDb, filepath);
#else
            var loadDb = Application.dataPath + "/StreamingAssets/" + DatabaseName;
            File.Copy(loadDb, filepath);
#endif
            Debug.Log("Database written");
        }

        var dbPath = filepath;
#endif
        _connection = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
        Debug.Log("Database PATH: " + dbPath);
    }

    public void CreateDB()
    {
        // Suppression et création des tables
        _connection.DropTable<CategorieNiveau>();
        _connection.DropTable<CategorieType>();
        _connection.DropTable<ExperienceChimie>();
        _connection.DropTable<NoteEval>();
        _connection.DropTable<Question>();
        _connection.DropTable<Option>();

        _connection.CreateTable<CategorieNiveau>();
        _connection.CreateTable<CategorieType>();
        _connection.CreateTable<ExperienceChimie>();
        _connection.CreateTable<NoteEval>();
        _connection.CreateTable<Question>();
        _connection.CreateTable<Option>();

        // Insertion des données de test
        InsertDefaultData();
    }

    private void InsertDefaultData()
    {
        // Insertion des catégories de niveau
        var categoriesNiveau = new[]
        {
            new CategorieNiveau { TitreCategNiv = "6ème", DescriptionCategNiv = "Classe de sixième - Découverte de la chimie" },
            new CategorieNiveau { TitreCategNiv = "5ème", DescriptionCategNiv = "Classe de cinquième - Approfondir les bases" },
            new CategorieNiveau { TitreCategNiv = "4ème", DescriptionCategNiv = "Classe de quatrième - Réactions chimiques" },
            new CategorieNiveau { TitreCategNiv = "3ème", DescriptionCategNiv = "Classe de troisième - Chimie avancée" },
            new CategorieNiveau { TitreCategNiv = "2nde", DescriptionCategNiv = "Seconde - Introduction chimie lycée" },
            new CategorieNiveau { TitreCategNiv = "1ère", DescriptionCategNiv = "Première - Chimie organique et inorganique" },
            new CategorieNiveau { TitreCategNiv = "Terminale", DescriptionCategNiv = "Terminale - Chimie approfondie" }
        };
        _connection.InsertAll(categoriesNiveau);

        // Insertion des catégories de type
        var categoriesType = new[]
        {
            new CategorieType { TitreCategTyp = "Alcanes", DescriptionCategTyp = "Hydrocarbures saturés" },
            new CategorieType { TitreCategTyp = "Alcools", DescriptionCategTyp = "Composés organiques avec groupe hydroxyle" },
            new CategorieType { TitreCategTyp = "Aldéhydes", DescriptionCategTyp = "Composés carbonylés avec groupe CHO" },
            new CategorieType { TitreCategTyp = "Cétones", DescriptionCategTyp = "Composés carbonylés avec groupe CO" },
            new CategorieType { TitreCategTyp = "Acides", DescriptionCategTyp = "Composés avec groupe carboxyle" },
            new CategorieType { TitreCategTyp = "Esters", DescriptionCategTyp = "Dérivés d'acides carboxyliques" },
            new CategorieType { TitreCategTyp = "Polymères", DescriptionCategTyp = "Macromolécules formées de monomères" },
            new CategorieType { TitreCategTyp = "Sels", DescriptionCategTyp = "Composés ioniques" },
            new CategorieType { TitreCategTyp = "Métaux", DescriptionCategTyp = "Éléments métalliques et leurs propriétés" },
            new CategorieType { TitreCategTyp = "Électrochimie", DescriptionCategTyp = "Réactions avec échange d'électrons" }
        };
        _connection.InsertAll(categoriesType);

        // Insertion des expériences
        var experiences = new[]
        {
            new ExperienceChimie { TitreExp = "Combustion du méthane", DescriptionExp = "Étude de la réaction de combustion complète du méthane", Duree = "15 min", IdCategNiv = 1, IdCategTyp = 1 },
            new ExperienceChimie { TitreExp = "Propriétés des alcanes", DescriptionExp = "Observation des propriétés physiques et chimiques des alcanes", Duree = "20 min", IdCategNiv = 2, IdCategTyp = 1 },
            new ExperienceChimie { TitreExp = "Oxydation de l'éthanol", DescriptionExp = "Transformation de l'éthanol en éthanal puis en acide éthanoïque", Duree = "25 min", IdCategNiv = 6, IdCategTyp = 2 },
            new ExperienceChimie { TitreExp = "Test de reconnaissance des alcools", DescriptionExp = "Utilisation de tests chimiques pour identifier les alcools", Duree = "18 min", IdCategNiv = 5, IdCategTyp = 2 },
            new ExperienceChimie { TitreExp = "Réaction avec la liqueur de Fehling", DescriptionExp = "Test de reconnaissance des aldéhydes avec la liqueur de Fehling", Duree = "12 min", IdCategNiv = 6, IdCategTyp = 3 },
            new ExperienceChimie { TitreExp = "Oxydation des aldéhydes", DescriptionExp = "Transformation des aldéhydes en acides carboxyliques", Duree = "22 min", IdCategNiv = 6, IdCategTyp = 3 },
            new ExperienceChimie { TitreExp = "Synthèse d'un ester", DescriptionExp = "Réaction d'estérification entre un acide et un alcool", Duree = "30 min", IdCategNiv = 6, IdCategTyp = 6 },
            new ExperienceChimie { TitreExp = "Hydrolyse d'un ester", DescriptionExp = "Réaction inverse de l'estérification", Duree = "28 min", IdCategNiv = 7, IdCategTyp = 6 },
            new ExperienceChimie { TitreExp = "Polymérisation du styrène", DescriptionExp = "Formation du polystyrène à partir du styrène", Duree = "35 min", IdCategNiv = 7, IdCategTyp = 7 },
            new ExperienceChimie { TitreExp = "Cristallisation du chlorure de sodium", DescriptionExp = "Formation de cristaux de sel par évaporation", Duree = "40 min", IdCategNiv = 3, IdCategTyp = 8 },
            new ExperienceChimie { TitreExp = "Tests de reconnaissance des métaux", DescriptionExp = "Identification des métaux par tests à la flamme", Duree = "20 min", IdCategNiv = 4, IdCategTyp = 9 },
            new ExperienceChimie { TitreExp = "Électrolyse de l'eau", DescriptionExp = "Décomposition de l'eau par passage de courant électrique", Duree = "25 min", IdCategNiv = 5, IdCategTyp = 10 },
            new ExperienceChimie { TitreExp = "Pile électrochimique", DescriptionExp = "Construction d'une pile avec différents métaux", Duree = "30 min", IdCategNiv = 7, IdCategTyp = 10 }
        };
        _connection.InsertAll(experiences);

        Debug.Log($"Données insérées : {categoriesNiveau.Length} niveaux, {categoriesType.Length} types, {experiences.Length} expériences");
    }

    // Méthodes pour CategorieNiveau
    public IEnumerable<CategorieNiveau> GetAllCategoriesNiveau()
    {
        return _connection.Table<CategorieNiveau>();
    }

    public CategorieNiveau GetCategorieNiveauById(int id)
    {
        return _connection.Table<CategorieNiveau>().Where(x => x.IdCategNiv == id).FirstOrDefault();
    }

    // Méthodes pour CategorieType
    public IEnumerable<CategorieType> GetAllCategoriesType()
    {
        return _connection.Table<CategorieType>();
    }

    public CategorieType GetCategorieTypeById(int id)
    {
        return _connection.Table<CategorieType>().Where(x => x.IdCategTyp == id).FirstOrDefault();
    }

    // Méthodes pour ExperienceChimie
    public IEnumerable<ExperienceChimie> GetAllExperiences()
    {
        return _connection.Table<ExperienceChimie>();
    }

    public IEnumerable<ExperienceChimie> GetExperiencesByNiveau(int idCategNiv)
    {
        return _connection.Table<ExperienceChimie>().Where(x => x.IdCategNiv == idCategNiv);
    }

    public IEnumerable<ExperienceChimie> GetExperiencesByType(int idCategTyp)
    {
        return _connection.Table<ExperienceChimie>().Where(x => x.IdCategTyp == idCategTyp);
    }

    public IEnumerable<ExperienceChimie> GetExperiencesByNiveauAndType(int idCategNiv, int idCategTyp)
    {
        return _connection.Table<ExperienceChimie>().Where(x => x.IdCategNiv == idCategNiv && x.IdCategTyp == idCategTyp);
    }

    public ExperienceChimie GetExperienceById(int id)
    {
        return _connection.Table<ExperienceChimie>().Where(x => x.IdExp == id).FirstOrDefault();
    }

    // Méthode pour obtenir les expériences avec leurs catégories (jointure)
    public List<ExperienceChimie> GetExperiencesWithCategories()
    {
        var experiences = GetAllExperiences().ToList();
        var niveaux = GetAllCategoriesNiveau().ToDictionary(n => n.IdCategNiv, n => n);
        var types = GetAllCategoriesType().ToDictionary(t => t.IdCategTyp, t => t);

        foreach (var exp in experiences)
        {
            if (niveaux.ContainsKey(exp.IdCategNiv))
                exp.CategorieNiveau = niveaux[exp.IdCategNiv];
            if (types.ContainsKey(exp.IdCategTyp))
                exp.CategorieType = types[exp.IdCategTyp];
        }

        return experiences;
    }

    // Méthodes CRUD pour les expériences
    public int AddExperience(ExperienceChimie experience)
    {
        return _connection.Insert(experience);
    }

    public int UpdateExperience(ExperienceChimie experience)
    {
        return _connection.Update(experience);
    }

    public int DeleteExperience(int id)
    {
        return _connection.Delete<ExperienceChimie>(id);
    }
}