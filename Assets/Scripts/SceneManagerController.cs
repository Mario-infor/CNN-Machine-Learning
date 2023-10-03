using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagerController : MonoBehaviour
{
    public void resetCurrentScene()
    {
        SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
    }
}
