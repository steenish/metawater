using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class TerrainIntersector : MonoBehaviour {

	private ContactPoint[] contactPoints;

	private void OnCollisionEnter(Collision collision) {
		contactPoints = new ContactPoint[collision.contactCount];
		collision.GetContacts(contactPoints);
		Debug.Log("collision");
		foreach (ContactPoint point in contactPoints) {
			Debug.Log(point.point);
		}
	}
}
