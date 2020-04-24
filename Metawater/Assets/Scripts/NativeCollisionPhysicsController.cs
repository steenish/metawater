using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NativeCollisionPhysicsController : MonoBehaviour {

  enum Direction {
    Left,
    Right
  }

  [SerializeField]
  private Bounds waterSpawnVolume;
  [SerializeField]
  private Vector3 waterInitialMaxVelocity = Vector3.zero;
  [SerializeField]
  private int waterSpawnInterval = 10;
  [SerializeField]
  [Range(1, 100)]
  private int numBalls = 1;
  [SerializeField]
  private float meanRadius = 2.0f;
  [SerializeField]
  private bool showDebugMarkers = false;
  [SerializeField]
  #pragma warning disable
  private Sprite debugSprite;
  #pragma warning restore

  public Metaball[] metaballs { get; private set; }
  public int numInstantiatedBalls { get; private set; }

  private AABBTreeNode rootNode;
  private Bounds terrainBounds;
  private float maxY;
  private float minY;
  private int framesSinceInstantiation = 0;
  private Mesh terrainMesh;

  void Start() {
    // Get terrain mesh. Mesh guaranteed to already be constructed since that happens in Awake.
    GameObject terrain = GameObject.FindWithTag("Terrain");
    terrainMesh = terrain.GetComponent<MeshFilter>().mesh;
    terrainBounds = terrain.GetComponent<NativeCollisionTerrainConstructor>().terrainBounds;
    maxY = terrainBounds.max.y;
    minY = terrainBounds.min.y;

    // Disable collisions between water particles.
    Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Water"), LayerMask.NameToLayer("Water"));

    // Instantiate metaballs.
    metaballs = new Metaball[numBalls];
    for (int i = 0; i < numBalls; ++i) {
      // Create metaball object, creat metaball component and set parameters.
      GameObject metaballObject = new GameObject("Metaball" + (i+1));
      metaballs[i] = metaballObject.AddComponent<Metaball>();
      metaballs[i].transform.parent = transform;
      metaballs[i].transform.position = GetRandomSpawnPoint();
      metaballs[i].instantiated = false;
      metaballs[i].radius = meanRadius;
      metaballs[i].velocity = Vector3.Lerp(Vector3.zero, waterInitialMaxVelocity, UnityEngine.Random.Range(0.0f, 1.0f));;
      metaballs[i].lastPosition = metaballs[i].transform.position;

      // Potentially render debug markers.
      if (showDebugMarkers) {
        metaballObject.AddComponent<SpriteRenderer>().sprite = debugSprite;
      }
    }
  }

  void Update() {
    Vector3[] vertices = terrainMesh.vertices;
    foreach (Metaball ball in metaballs) {
      // Check if ball is instantiated.
      if (!ball.instantiated) {
        // If ball is not instantiated, check if it should be.
        if (framesSinceInstantiation > waterSpawnInterval) {
          // Ball should be instantiated, reset framesSinceInstantiation.
          framesSinceInstantiation = 0;
          ball.instantiated = true;
          ball.gameObject.AddComponent<Rigidbody>();
          ball.gameObject.AddComponent<SphereCollider>();
          ball.gameObject.layer = LayerMask.NameToLayer("Water");
          numInstantiatedBalls++;
          Debug.Log("instantiated " + ball.gameObject.name);
        } else {
          // Ball should not be instantiated, skip all physics.
          continue;
        }
      }
      // Check if ball is within bounds.
      if (ball.transform.position.y < terrainBounds.min.y) {
        // Set new spawn position and reset velocity if ball is out of bounds.
        ball.transform.position = GetRandomSpawnPoint();
        ball.velocity = Vector3.Lerp(Vector3.zero, waterInitialMaxVelocity, UnityEngine.Random.Range(0.0f, 1.0f));
      }

    }
    framesSinceInstantiation++;
  }

    private Vector3 GetRandomSpawnPoint() {
      // Select random position within bounds.
      float x = UnityEngine.Random.Range(waterSpawnVolume.min.x, waterSpawnVolume.max.x);
      float y = UnityEngine.Random.Range(waterSpawnVolume.min.y, waterSpawnVolume.max.y);
      float z = UnityEngine.Random.Range(waterSpawnVolume.min.z, waterSpawnVolume.max.z);
      return new Vector3(x,y,z);
    }
  }
