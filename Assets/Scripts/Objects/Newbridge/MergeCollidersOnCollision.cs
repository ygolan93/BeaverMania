using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MergeCollidersOnCollision : MonoBehaviour
{
    // The two GameObjects with colliders to merge
    public GameObject FirstPart;
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            Debug.Log("Part added to bridge: " + collision.gameObject.name);
            MergeColliders(FirstPart, collision.gameObject);
        }
        
    }

    // Merge the colliders of two GameObjects
    void MergeColliders(GameObject obj1, GameObject obj2)
    {
        // Get colliders of both objects
        Collider[] colliders1 = obj1.GetComponents<Collider>();
        Collider[] colliders2 = obj2.GetComponents<Collider>();

        // Disable the colliders on both objects
        foreach (var collider in colliders1)
        {
            collider.enabled = false;
        }
        foreach (var collider in colliders2)
        {
            collider.enabled = false;
        }

        // Create a new empty GameObject to hold the merged collider
        GameObject mergedObject = new GameObject("MergedCollider");
        mergedObject.transform.position = obj1.transform.position;

        // Attach a new collider to the merged GameObject
        // Here, you can use a specific type of collider (e.g., MeshCollider) based on your needs
        // This example assumes a BoxCollider for simplicity
        BoxCollider mergedCollider = mergedObject.AddComponent<BoxCollider>();

        // Combine the bounds of the original colliders to form the new collider's size
        Bounds mergedBounds = new Bounds();
        foreach (var collider in colliders1)
        {
            mergedBounds.Encapsulate(collider.bounds);
        }
        foreach (var collider in colliders2)
        {
            mergedBounds.Encapsulate(collider.bounds);
        }

        // Set the size and center of the merged collider
        mergedCollider.size = mergedBounds.size;
        mergedCollider.center = mergedBounds.center;

        // Destroy the original GameObjects
        Destroy(obj1);
        Destroy(obj2);
    }
}

