using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MetaballSystem : MonoBehaviour {

  [SerializeField]
  private Bounds boundingBox;
  [SerializeField]
  [Range(1,30)]
  private float resolution;
  [SerializeField]
  [Range(0, 10)]
  private float threshold;

  // DEBUG START
  public GameObject debuggingPoint;
  public bool renderInside;
  // DEBUG END

  private float tolerance = 0.05f;
  private GameObject[] metaballs;

  // DEBUG START
  private List<GameObject> markerPoints;
  // DEBUG END

  void Start() {
    // DEBUG START
    markerPoints = new List<GameObject>();
    GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
    Mesh sphereMesh = sphere.GetComponent<MeshFilter>().mesh;
    Material sphereMaterial = sphere.GetComponent<MeshRenderer>().sharedMaterial;
    Destroy(sphere);
    int numBalls = 10;
    metaballs = new GameObject[numBalls];
    for (int i = 0; i < numBalls; ++i) {
      float radius = 1.0f;
      float x = Random.Range(boundingBox.min.x + radius, boundingBox.max.x - radius);
      float y = Random.Range(boundingBox.min.y + radius, boundingBox.max.y - radius);
      float z = Random.Range(boundingBox.min.z + radius, boundingBox.max.z - radius);

      metaballs[i] = new GameObject("Metaball"+(i+1));
      metaballs[i].AddComponent<Metaball>();
      metaballs[i].GetComponent<Metaball>().radius = radius;
      metaballs[i].transform.position = new Vector3(x, y, z);

      metaballs[i].AddComponent<MeshFilter>().mesh = sphereMesh;
      metaballs[i].AddComponent<MeshRenderer>().sharedMaterial = sphereMaterial;
    }
    // DEBUG END
  }

  void Update() {
    // DEBUG START
    foreach (GameObject obj in markerPoints) {
      Destroy(obj);
    }
    markerPoints = new List<GameObject>();
    // DEBUG END

    float xIncrement = (boundingBox.max.x - boundingBox.min.x) / resolution;
    float yIncrement = (boundingBox.max.y - boundingBox.min.y) / resolution;
    float zIncrement = (boundingBox.max.z - boundingBox.min.z) / resolution;
    for (float x = boundingBox.min.x; x < boundingBox.max.x; x += xIncrement) {
      for (float y = boundingBox.min.y; y < boundingBox.max.y; y += yIncrement) {
        for (float z = boundingBox.min.z; z < boundingBox.max.z; z += zIncrement) {
          float pointValue = 0;
          foreach (GameObject obj in metaballs) {
            pointValue += obj.GetComponent<Metaball>().falloff(new Vector3(x, y, z));
          }
          //Debug.Log(pointValue);
          // Check if the value in the point (x, y, z) is within tolerance of the threshold.
          if (!renderInside) { // DEBUG
            if (pointValue < threshold + tolerance && pointValue > threshold - tolerance) {
              //Debug.Log("created primitive");
              // DEBUG START
              markerPoints.Add(Instantiate(debuggingPoint, new Vector3(x, y, z), Quaternion.identity));
              markerPoints[markerPoints.Count - 1].transform.rotation = Quaternion.LookRotation(-Camera.main.transform.forward);
              // DEBUG END
            }
            // DEBUG START
          } else if (pointValue > threshold) {
            markerPoints.Add(Instantiate(debuggingPoint, new Vector3(x, y, z), Quaternion.identity));
            markerPoints[markerPoints.Count - 1].transform.rotation = Quaternion.LookRotation(-Camera.main.transform.forward);
          } // DEBUG END
        }
      }
    }
  }
}
