using UnityEngine;
using UnityEngine.SceneManagement;

public class Setup : MonoBehaviour
{
    void Start()
    {
        DataCache.LoadAll();
        
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex + 1);
    }
}
