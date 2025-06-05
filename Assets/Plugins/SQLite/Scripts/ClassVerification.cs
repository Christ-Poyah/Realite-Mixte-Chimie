using UnityEngine;
using UnityEngine.UI;
using System;

public class ClassVerification : MonoBehaviour
{
    public Text debugText;

    void Start()
    {
        VerifyClasses();
    }

    [ContextMenu("Verify All Classes")]
    public void VerifyClasses()
    {
        ToDebug("=== VERIFYING CLASSES ===");
        
        try
        {
            // Test Experience class
            var experience = new Experience();
            experience.Id_Experiences = 1;
            experience.Id_Categorie = 1;
            experience.Libelle = "Test";
            ToDebug("✓ Experience class OK");
        }
        catch (Exception e)
        {
            ToDebug($"❌ Experience class ERROR: {e.Message}");
        }

        try
        {
            // Test Experiment class
            var experiment = new Experiment();
            experiment.Description = "Test";
            experiment.Duration = "5 min";
            experiment.Category = "Test";
            experiment.IsActive = true;
            ToDebug("✓ Experiment class OK");
        }
        catch (Exception e)
        {
            ToDebug($"❌ Experiment class ERROR: {e.Message}");
        }

        try
        {
            // Test DataService class
            var ds = new DataService("test.db");
            ToDebug("✓ DataService class OK");
        }
        catch (Exception e)
        {
            ToDebug($"❌ DataService class ERROR: {e.Message}");
        }

        ToDebug("=== VERIFICATION COMPLETE ===");
    }

    private void ToDebug(string message)
    {
        Debug.Log(message);
        if (debugText != null)
        {
            debugText.text += Environment.NewLine + message;
        }
    }
}