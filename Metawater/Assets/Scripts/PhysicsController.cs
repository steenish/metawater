using System;
﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsController : MonoBehaviour {

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
  private float gravityAcceleration = 9.82f;
  [SerializeField]
  private float ballSpringStiffness = 1.0f;
  [SerializeField]
  private float minimumDistance = 1.0f;
  [SerializeField]
  private float maximumDistance = 3.0f;
  [SerializeField]
  [Range(0.0f, 1.0f)]
  private float groundDamping = 0.7f;
  [SerializeField]
  [Range(1, 100)]
  private int numBalls = 1;
  [SerializeField]
  private float meanRadius = 2.0f;
  [SerializeField]
  private float constantOffset = 0.07f;
  [SerializeField]
  [Range(2, 16)]
  private int leafNumTriangles = 8;
  [SerializeField]
  private bool showDebugMarkers = false;
  [SerializeField]
  #pragma warning disable
  private Sprite debugSprite;
  #pragma warning restore
  [SerializeField]
  private bool showDebugBoundsVis = false;
  [SerializeField]
  #pragma warning disable
  private GameObject debugBoundsVis;
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
    terrainBounds = terrain.GetComponent<TerrainConstructor>().terrainBounds;
    maxY = terrainBounds.max.y;
    minY = terrainBounds.min.y;

    // Create AABB tree.
    ConstructAABBTree();

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

  // Since these physics are independent from Unity's doing physics in Update
  // as opposed to FixedUpdate is not a problem.
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
          numInstantiatedBalls++;
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

      // Calculate the cumulative acceleration.
      Vector3 totalAcceleration = gravityAcceleration * Vector3.down;
      foreach (Metaball otherBall in metaballs) {
        if (ball == otherBall || !otherBall.instantiated) continue;

        // Force is like a spring force between each metaball and each other metaball.
        // The spring force is to keep balls at a minimum distance from eachother.
        // The force weakens the further away the balls are from eachother beyond the
        // minimum distance.

        Vector3 ballToOther = ball.transform.position - otherBall.transform.position;
        Vector3 springForce = ballSpringStiffness * (-ballToOther + Vector3.Normalize(ballToOther)*minimumDistance);

        // Force equation gives F = m * a, and with m = 1 for simplicity, we have F = a.
        // Scale by 0.5 because this is only one of the two balls.
        // If ball is outside maximumDistance of the equilibrium point, apply no force.
        if (ballToOther.magnitude - minimumDistance < maximumDistance) {
          totalAcceleration += 0.5f * springForce;
        }
      }

      // Update velocity according to acceleration.
      ball.velocity += totalAcceleration * Time.deltaTime;

      // Update position according to velocity.
      ball.lastPosition = ball.transform.position;
      ball.transform.position += ball.velocity * Time.deltaTime;

      // Check for intersection.
      int[] triangles;
      if (IntersectionTests.GetTriangles(ball.transform.position, rootNode, out triangles)) {
        bool fastIntersectionFound = false;
        // Do two passes for intersection tests.
        // First pass tests the found triangles in the leaf bounding box. Fast.
        // Second pass tests all triangles if the first pass failed.
        // This minimizes risk for fallthrough while giving good performance.
        for (int pass = 1; pass <= 2; ++pass) {
          // If no fast intersection was found, do exhaustive.
          if (pass == 2 && !fastIntersectionFound) {
            triangles = terrainMesh.triangles;
          }

          // Iterate through all found triangles.
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
              Vector3 point = (Vector3) intersectionPoint;
              Vector3 normal = Vector3.Normalize(Vector3.Cross(triangle[2] - triangle[0],
                                                               triangle[1] - triangle[0]));
              // Get reflection direction.
              Vector3 newDirection = Vector3.Normalize(2*Vector3.Dot(-rayDirection, normal) * normal + rayDirection);
              // Set dampened velocity.
              ball.velocity = (1.0f - groundDamping) * ball.velocity.magnitude * newDirection;
              // Update last position to the intersection point.
              ball.lastPosition = point + constantOffset * Vector3.up;
              // The new ball position is the distance traveled from the intersection point
              // the distance traveled beyond the triangle, but adjusted for the new
              // velocity and along the reflection direction.
              ball.transform.position = point + constantOffset * Vector3.up + ball.velocity * Time.deltaTime;

              // Break out of loop checking triangles after intersection was found.
              // If this is on the first pass, signal that second pass is not necessary.
              // If this is on the second pass, setting this will not matter.
              fastIntersectionFound = true;
              break;
            }
          }
        }
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

  private void ConstructAABBTree() {
    // Construct root node.
    rootNode = new AABBTreeNode(terrainBounds);
    rootNode.triangles = terrainMesh.triangles;

    if (showDebugBoundsVis) {
      GameObject cube = Instantiate(debugBoundsVis, terrainBounds.center, Quaternion.identity, transform);
      cube.transform.localScale = terrainBounds.size;
    }

    rootNode.leftChild = AABBTreeRecursion(rootNode, Direction.Left);
    rootNode.rightChild = AABBTreeRecursion(rootNode, Direction.Right);
  }

  private AABBTreeNode AABBTreeRecursion(AABBTreeNode parent, Direction childDirection) {
    // If left child, get the first half of the parent's triangles.
    // If right child, get the second half of the parent's triangles.
    int triangleStartIndex = (childDirection == Direction.Left) ? 0 : parent.triangles.Length / 2;

    // Get the correct triangles.
    int[] currentTriangles = new int[parent.triangles.Length / 2];
    Array.Copy(parent.triangles, triangleStartIndex, currentTriangles, 0, currentTriangles.Length);

    // Construct bounds from triangles.
    Vector3[] terrainVertices = terrainMesh.vertices;
    Vector3 min = terrainVertices[currentTriangles[0]];
    Vector3 max = terrainVertices[currentTriangles[currentTriangles.Length-1]];
    // Correct height of the bounds.
    min = new Vector3(min.x, minY, min.z);
    max = new Vector3(max.x, maxY, max.z);
    Vector3 center = Vector3.Lerp(min, max, 0.5f);
    Bounds currentBounds = new Bounds(center, Vector3.zero);
    currentBounds.Encapsulate(min);
    currentBounds.Encapsulate(max);

    // Create new node.
    AABBTreeNode currentNode = new AABBTreeNode(currentBounds);
    currentNode.triangles = currentTriangles;

    if (showDebugBoundsVis) {
      GameObject cube = Instantiate(debugBoundsVis, center, Quaternion.identity, transform);
      cube.transform.localScale = currentBounds.size;
    }

    // If node should not be leaf, set children, otherwise leave them null.
    if (currentTriangles.Length / 3 > leafNumTriangles) {
      currentNode.leftChild = AABBTreeRecursion(currentNode, Direction.Left);
      currentNode.rightChild = AABBTreeRecursion(currentNode, Direction.Right);
    }
    return currentNode;
  }
}
