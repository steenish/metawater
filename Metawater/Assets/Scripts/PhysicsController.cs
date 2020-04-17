using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsController : MonoBehaviour {

  [SerializeField]
  private float gravityAcceleration = 9.82f;
  [SerializeField]
  [Range(0.0f, 1.0f)]
  private float groundDamping = 0.7f;
  [SerializeField]
  [Range(2, 10)]
  private int numBalls = 2;
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
    // Create AABB tree. TODO optimize

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

  // Since these physics are independent from Unity's doing physics in Update
  // as opposed to FixedUpdate is not a problem.
  void Update() {
    Vector3[] vertices = terrainMesh.vertices;
    int[] triangles = terrainMesh.triangles;
    foreach (Metaball ball in metaballs) {
      // Update velocity according to acceleration.
      ball.velocity += Vector3.down * gravityAcceleration * Time.deltaTime;

      // Update position according to velocity.
      ball.lastPosition = ball.transform.position;
      ball.transform.position += ball.velocity * Time.deltaTime;

      // Check for intersection.
      // Iterate through all triangles. TODO optimize
      for (int i = 0; i < triangles.Length; i += 3) {
        // Get triangle vertices.
        Vector3[] triangle = new Vector3[] { vertices[triangles[i]],
                                             vertices[triangles[i+1]],
                                             vertices[triangles[i+2]] };
        // Test for intersection.
        Vector3 rayDirection = ball.transform.position - ball.lastPosition;
        float distance = rayDirection.magnitude;
        rayDirection = Vector3.Normalize(rayDirection);
        Vector3? intersectionPoint;
        // If intersection, compute new position and velocity.
        if (IntersectionTests.RayTriangleTest(ball.lastPosition, rayDirection, distance, triangle, out intersectionPoint)) {
          Debug.Log("intersection");
          Vector3 point = (Vector3) intersectionPoint;
          Vector3 wrongPos = ball.transform.position;
          Vector3 normal = Vector3.Normalize(Vector3.Cross(triangle[2] - triangle[0],
                                                           triangle[1] - triangle[0]));
          Vector3 newDirection = Vector3.Normalize(2*Vector3.Dot(-rayDirection, normal) * normal + rayDirection);
          ball.velocity = (1.0f - groundDamping) * ball.velocity.magnitude * newDirection;
          ball.lastPosition = point;
          ball.transform.position = point + ball.velocity * Time.deltaTime;
        }
      }
    }
  }
}
