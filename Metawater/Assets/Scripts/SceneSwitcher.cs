using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour {

  public void LoadFullVersion() {
    SceneManager.LoadScene(0);
  }

  public void LoadUnityCollisionsVersion() {
    SceneManager.LoadScene(1);
  }

  public void LoadUnityCollisionsNileVersion() {
    SceneManager.LoadScene(2);
  }
}
