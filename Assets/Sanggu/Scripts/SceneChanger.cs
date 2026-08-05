using UnityEngine;
using UnityEngine.SceneManagement;

public enum EScenes
{
    Village = 3,
    Battle = 4
}

public class SceneChanger : MonoBehaviour
{
    private static SceneChanger _instance;

    public static SceneChanger GetInstance()
    {
        Init();
        return _instance;
    }

    static void Init()
    {
        if (_instance == null)
        {
            GameObject go = GameObject.Find("SceneChanger");
            if (go == null)
            {
                go = new GameObject { name = "SceneChanger" };
                go.AddComponent<SceneChanger>();
            }

            _instance = go.GetComponent<SceneChanger>();
        }
    }

    private void Awake()
    {
        Init();
        DontDestroyOnLoad(gameObject);
    }

    public void NextScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void LoadScene(EScenes scene)
    {
        SceneManager.LoadScene((int)scene);
    }
    public void LoadScene(int scene)
    {
        SceneManager.LoadScene(scene);
    }
}
