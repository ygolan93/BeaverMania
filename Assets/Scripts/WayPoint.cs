using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WayPoint : MonoBehaviour
{
    public Image Mark;
    private Transform target;
    public Transform[] Locations;
    public Transform Arrow;
    public int i;

    // Start is called before the first frame update
    void Start()
    {
        Arrow.gameObject.SetActive(false);
        for (int index = 0; index < Locations.Length; index++)
        {
            if (index != i && index != 21 && index != 22)
            {
                Locations[index].gameObject.SetActive(false);
            }
        }
        if (i >= 0 && i < Locations.Length)
        {
            Locations[i].gameObject.SetActive(true);
            target = Locations[i];
        }
        else
        {
            Debug.LogError("Index i is out of bounds.");
        }
    }

    void Update()
    {
        // Arrow Compass
        if (Input.GetKey(KeyCode.Q))
        {
            Arrow.gameObject.SetActive(true);
        }
        else
        {
            Arrow.gameObject.SetActive(false);
        }

        if (target != null)
        {
            Arrow.gameObject.transform.rotation = Quaternion.LookRotation(target.position - transform.position);
        }

        // Waypoint Mark
        if (target != null)
        {
            float minX = Mark.GetPixelAdjustedRect().width / 2;
            float maxX = Screen.width - minX;
            float minY = Mark.GetPixelAdjustedRect().height / 2;
            float maxY = Screen.height - minY;
            Vector2 pos = Camera.main.WorldToScreenPoint(target.position + new Vector3(0, 2, 0));

            if (Vector3.Dot((target.position - transform.position), transform.forward) < 0)
            {
                // Target is behind player
                Mark.enabled = false;
            }
            else
            {
                Mark.enabled = true;
            }

            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            Mark.transform.position = pos;
        }
    }

    private void OnTriggerEnter(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("WayPoint"))
        {
            OBJ.gameObject.SetActive(false);

            if (int.TryParse(OBJ.gameObject.name, out int newIndex))
            {
                i = newIndex;
                if (i >= 0 && i < Locations.Length)
                {
                    Locations[i].gameObject.SetActive(true);
                    target = Locations[i];
                }
                else
                {
                    Debug.LogWarning("Parsed index is out of bounds: " + i);
                }
            }
            else
            {
                Debug.LogWarning("Failed to parse waypoint name: " + OBJ.gameObject.name);
            }
        }
    }
}
