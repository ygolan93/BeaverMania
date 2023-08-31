using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Destroy : MonoBehaviour
{
    float Clock;
    public GameObject effect;
    public bool saveAfterKill;
    // Start is called before the first frame update
    void Start()
    {
         Clock = 5f;
        Physics.IgnoreLayerCollision(0, 7);
    }
    public void DestroySelf()
    {
        Instantiate(effect, gameObject.transform.position, Quaternion.identity);
        if (saveAfterKill == false)
        {
            Destroy(gameObject);
        }
        if (saveAfterKill == true)
        {
            gameObject.SetActive(false);
        }

    }
    private void OnCollisionEnter(Collision OBJ)
    {
        if (OBJ.gameObject.CompareTag("Player"))
            DestroySelf();
    }
    // Update is called once per frame
    void Update()
    {
        Clock -= Time.deltaTime;
        if (Clock <= 0)
        {
            DestroySelf();
        }
    }
}
