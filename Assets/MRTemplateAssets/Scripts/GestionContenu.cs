using UnityEngine;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class Experience
{
    public string titre;
    public int temps;
    public string description;
}

public class ExperienceManager : MonoBehaviour
{
    public GameObject experiencePrefab; // Le prefab que tu as créé
    public Transform contentParent;     // Le Content du ScrollView
    public List<Experience> experiences;

    void Start()
    {
        ChargerExperiences();
        AfficherExperiences();
    }

    void ChargerExperiences()
    {
        experiences = new List<Experience>
        {
            new Experience { titre = "Réaction acide-base", temps = 10, description = "Vinaigre + bicarbonate" },
            new Experience { titre = "Flamme colorée", temps = 5, description = "Coloration par sels métalliques" },
            new Experience { titre = "Osmose avec pomme de terre", temps = 15, description = "Diffusion de l'eau dans un tube de pomme de terre" },
            new Experience { titre = "Chromatographie", temps = 20, description = "Séparation des encres par capillarité" },
        };
    }

    void AfficherExperiences()
    {
        foreach (var exp in experiences)
        {
            GameObject go = Instantiate(experiencePrefab, contentParent);
            go.transform.Find("TextTitre").GetComponent<TMP_Text>().text = exp.titre;
            go.transform.Find("TextTemps").GetComponent<TMP_Text>().text = exp.temps + " min";
            go.transform.Find("TextDescription").GetComponent<TMP_Text>().text = exp.description;
        }
    }
}
