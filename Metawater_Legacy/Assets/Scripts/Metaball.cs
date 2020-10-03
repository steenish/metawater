using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Metaball : MonoBehaviour {

  public bool instantiated { get; set; }
  public float radius { get; set; }
  public Vector3 velocity { get; set; }
  public Vector3 lastPosition { get; set; }

  // Determines the contribution from this metaball to the metaball system.
  public float Falloff(Vector3 point) {
    float distance = Vector3.Distance(point, transform.position);
    if (distance == 0) {
      return Mathf.Infinity;
    } else {
      return radius / distance;
    }
    //return Mathf.Pow(1 - Mathf.Pow(distance, 2), 2);
  }
}
