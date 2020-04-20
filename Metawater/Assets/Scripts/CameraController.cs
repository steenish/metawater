using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour {

  [SerializeField]
  private bool invertY = false;
  [SerializeField]
  [Range(1.0f, 5.0f)]
  private float speed = 2.0f;
  [SerializeField]
  [Range(1.0f, 5.0f)]
  private float sensitivity = 2.0f;
  [SerializeField]
  private bool frameDependent = true;

  private float yaw;
  private float pitch;
  private Vector2 mouseLook;
  private Vector2 smoothV;

  void Start() {
    Cursor.lockState = CursorLockMode.Locked;

    yaw = transform.eulerAngles.y;
    pitch = transform.eulerAngles.x;
  }

  void Update() {
    // Handle camera translation.
    float forwardTranslation = Input.GetAxis("Vertical") * speed;
    float rightTranslation = Input.GetAxis("Horizontal") * speed;
    float upTranslation = 0.0f;
    float upTranslationChange = 0.5f * speed;

    if (frameDependent) {
      forwardTranslation *= Time.deltaTime;
      rightTranslation *= Time.deltaTime;
      upTranslationChange *= Time.deltaTime;
    }

    if (Input.GetKey(KeyCode.Space)) {
      upTranslation += upTranslationChange;
    }

    if (Input.GetKey(KeyCode.LeftShift)) {
      upTranslation -= upTranslationChange;
    }

    transform.Translate(rightTranslation, 0, forwardTranslation);
    transform.Translate(0, upTranslation, 0, Space.World);

    // Handle camera rotation.
    if (Cursor.lockState == CursorLockMode.Locked) {
      yaw += sensitivity * Input.GetAxis("Mouse X");
      float pitchChange = sensitivity * Input.GetAxis("Mouse Y");
      pitch += (invertY) ? pitchChange : -pitchChange;

      transform.eulerAngles = new Vector3(pitch, yaw, 0.0f);
    }

    // Handle mouse unlocking.
    if (Input.GetKeyDown(KeyCode.Escape)) {
      Cursor.lockState = CursorLockMode.None;
    }
  }
}
