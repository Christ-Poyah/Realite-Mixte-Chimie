using UnityEngine;
using UnityEngine.SceneManagement;
public class NewMonoBehaviourScript : MonoBehaviour
{
    public void Charger(string nom_scene)
{
    if (Application.CanStreamedLevelBeLoaded(nom_scene))
    {
        SceneManager.LoadScene(nom_scene);
    }
    else
    {
        Debug.LogError($"La scène '{nom_scene}' n'existe pas ou n'est pas incluse dans les Build Settings.");
    }
}

}
