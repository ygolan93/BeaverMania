using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Destroy : MonoBehaviour
{
    [SerializeField] float Clock;
    public GameObject effect;
    public bool saveAfterKill;

    bool destroySelfSuppressed;

    public void SetDestroySelfSuppressed(bool suppressed)
    {
        destroySelfSuppressed = suppressed;
    }

    public void PrepareForPool()
    {
        destroySelfSuppressed = true;
        enabled = false;
    }

    // Start is called before the first frame update
    void Start()
    {
        Physics.IgnoreLayerCollision(0, 7);
    }
    public void DestroySelf()
    {
        if (destroySelfSuppressed)
            return;

        if (effect!=null)
        {
            Instantiate(effect, gameObject.transform.position, Quaternion.identity);
        }

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
        if (destroySelfSuppressed)
            return;

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
