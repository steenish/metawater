using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntersectionTests {

  public static readonly float epsilon = 0.001f;

  // Traverses an AABBTree from currentNode. Returns true if leaf is found, and
  // the triangles are returned in the triangles array. Otherwise, return false.
  public static bool GetTriangles(Vector3 point, AABBTreeNode currentNode, out int[] triangles) {
    triangles = null;
    // Check if point is within bounds.
    if (currentNode.bounds.Contains(point)) {
      // If point within bounds, check if currentNode is leaf.
      if (currentNode.leftChild == null && currentNode.rightChild == null) {
        // If leaf, set triangles and return true.
        triangles = currentNode.triangles;
        return true;
      } else {
        // If not leaf, continue recursion. Either point is in leftChild or rightChild.
        return GetTriangles(point, currentNode.leftChild, out triangles) ||
               GetTriangles(point, currentNode.rightChild, out triangles);
      }
    } else {
      // If not within bounds, return false.
      return false;
    }
  }

  // Returns true if the ray originating in rayOrigin in the normalized direction
  // rayDirection intersects the triangle defined by the vertices in triangleVertices
  // in clockwise winding order, and gives the intersection point in intersectionPoint.
  // Returns false if no intersection, an intersection beyond maxDistance or a
  // negative intersection was found, and intersectionPoint is left null.
  public static bool RayTriangleTest(Vector3 rayOrigin, Vector3 rayDirection, float maxDistance, Vector3[] triangleVertices, out Vector3? intersectionPoint) {
    intersectionPoint = null;
    // Calculate normal, needed for further calculations.
    Vector3 normal = Vector3.Cross(triangleVertices[2] - triangleVertices[0],
                                   triangleVertices[1] - triangleVertices[0]);

    // Calculate denominator of the test.
    float d = Vector3.Dot(rayDirection, normal);
    // Denominator is zero (floating), so ray and plane are parallell.
    if (Mathf.Abs(d) < epsilon) return false;

    // Calculate the test quotient.
    float t = Vector3.Dot((triangleVertices[0] - rayOrigin), normal) / d;

    // Illegal intersection found.
    if (t < 0 || t > maxDistance) return false;

    // Find the potential intersection point.
    Vector3 intersectionCandidate = rayOrigin + t*rayDirection;

    // Test if point is within triangle, by exploiting winding order and dot products
    // of normal and cross products.
    float relative0To1 = Vector3.Dot(normal, Vector3.Cross(intersectionCandidate - triangleVertices[0], triangleVertices[1] - triangleVertices[0]));
    float relative1To2 = Vector3.Dot(normal, Vector3.Cross(intersectionCandidate - triangleVertices[1], triangleVertices[2] - triangleVertices[1]));
    float relative2To0 = Vector3.Dot(normal, Vector3.Cross(intersectionCandidate - triangleVertices[2], triangleVertices[0] - triangleVertices[2]));

    // If all the dot products are positive, it means the intersection point was to the
    // right of every edge in the triangle, and therefore inside the triangle.
    if (relative0To1 >= 0 && relative1To2 >= 0 && relative2To0 >= 0) {
        intersectionPoint = (Vector3) intersectionCandidate;
        return true;
    }
    return false;
  }
}
