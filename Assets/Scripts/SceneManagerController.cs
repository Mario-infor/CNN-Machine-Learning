using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/*
 * Reload scene whuen the corresponding button is pressed on canvas.
 */
public class SceneManagerController : MonoBehaviour
{
    public void resetCurrentScene()
    {
        SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
    }
}
