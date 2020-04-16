using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsController : MonoBehaviour {

  [SerializeField]
  private float gravityAcceleration = 9.8f;
  [SerializeField]
  private int numBalls = 10;
  [SerializeField]
  private float meanRadius = 2.0f;
  [SerializeField]
  private bool showDebugMarkers = false;
  [SerializeField]
  private Sprite debugSprite;

  public Metaball[] metaballs { get; private set; }

  private Bounds terrainBounds;
  private Mesh terrainMesh;

  void Start() {
    // Get terrain mesh. Mesh guaranteed to already be constructed since that happens in Awake.
    GameObject terrain = GameObject.FindWithTag("Terrain");
    terrainMesh = terrain.GetComponent<MeshFilter>().mesh;
    terrainBounds = terrain.GetComponent<TerrainConstructor>().terrainBounds;
    // Create AABB tree. TODO

    // Instantiate metaballs.
    metaballs = new Metaball[numBalls];
    for (int i = 0; i < numBalls; ++i) {
      // Select random position within bounds.
      float x = Random.Range(terrainBounds.min.x + meanRadius, terrainBounds.max.x - meanRadius);
      float y = Random.Range(terrainBounds.min.y + meanRadius, terrainBounds.max.y - meanRadius);
      float z = Random.Range(terrainBounds.min.z + meanRadius, terrainBounds.max.z - meanRadius);

      // Create metaball object, creat metaball component and set parameters.
      GameObject metaballObject = new GameObject("Metaball" + (i+1));
      metaballs[i] = metaballObject.AddComponent<Metaball>();
      metaballs[i].transform.position = new Vector3(x, y, z);
      metaballs[i].radius = meanRadius;
      metaballs[i].velocity = new Vector3(0.0f, 0.0f, 0.0f);
      metaballs[i].lastPosition = metaballs[i].transform.position;
      metaballs[i].transform.parent = transform;

      // Potentially render debug balls.
      if (showDebugMarkers) {
        metaballObject.AddComponent<SpriteRenderer>().sprite = debugSprite;
      }
    }
  }

  void FixedUpdate() {
    //foreach (Metaball ball in metaballs) {
      // Update velocity according to acceleration.
      // Update position according to velocity.
      // Check for intersection.
      // If intersection, compute new position and velocity.
    //}
  }
}
