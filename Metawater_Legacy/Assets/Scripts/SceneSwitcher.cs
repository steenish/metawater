using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour {

  void Update() {
    if (Input.GetKey(KeyCode.Alpha1)) {
      SceneManager.LoadScene(0);
    } else if (Input.GetKey(KeyCode.Alpha2)) {
      SceneManager.LoadScene(1);
    } else if (Input.GetKey(KeyCode.Alpha3)) {
      SceneManager.LoadScene(2);
    }
  }
}
