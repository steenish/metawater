using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Metaball : MonoBehaviour {

  public float radius { get; set; }

  void Start() {

  }

  void Update() {

  }

  // Determines the contribution from this metaball to the metaball system.
  public float falloff(Vector3 point) {
    float distance = Vector3.Distance(point, transform.position);
    if (distance == 0) {
      return Mathf.Infinity;
    } else {
      return radius / distance;
    }
  }
}
