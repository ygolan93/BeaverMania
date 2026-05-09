using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaspSpawner : MonoBehaviour, IRuntimeResettable
{
    public GameObject Wasp;
    [SerializeField] GameObject Hive;
    public Behaviour Player;
    Transform playerTransform;
    private bool initialized;
    public Vector3 Distance;
    [SerializeField] float SpawnDistance;
    public int WaspCounter=3;
    int Counter;
    public float SpawnClock=15f;
   public float RealClock;

    Vector3 initialPosition;
    Quaternion initialRotation;
    int initialCounter;
    float initialRealClock;
    readonly List<GameObject> spawnedWasps = new List<GameObject>();
    readonly List<Collider> spawnedWaspColliders = new List<Collider>();
    
    private void Start()
    { 
        Counter=WaspCounter;
        RealClock = SpawnClock;
        if (Hive == null)
        {
            Hive = gameObject;
        }

        if (Player == null)
        {
            PlayerReference.TryGetPlayer(out Player);
        }

        if (Player != null)
        {
            playerTransform = Player.transform;
        }

        bool validReferences = ValidateReferences();
        if (validReferences && playerTransform != null)
        {
            CaptureRuntimeState();
        }

        initialized = validReferences && playerTransform != null;
    }

    void CaptureRuntimeState()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        initialCounter = Counter;
        initialRealClock = RealClock;
    }

    public void RuntimeReset()
    {
        transform.SetPositionAndRotation(initialPosition, initialRotation);
        Counter = initialCounter;
        RealClock = initialRealClock;
        Distance = Vector3.zero;

        for (int i = spawnedWasps.Count - 1; i >= 0; i--)
        {
            if (spawnedWasps[i] != null)
            {
                Destroy(spawnedWasps[i]);
            }
        }

        spawnedWasps.Clear();
        spawnedWaspColliders.Clear();
    }

    bool ValidateReferences()
    {
        return RuntimeReferenceValidator.Require(Wasp, this, nameof(Wasp)) &
            RuntimeReferenceValidator.Require(Hive, this, nameof(Hive)) &
            RuntimeReferenceValidator.Require(Player, this, nameof(Player));
    }


    public void Update()
    {
        if (!initialized)
        {
            return;
        }

         Distance = playerTransform.position - gameObject.transform.position;

        if (Mathf.Abs(Distance.magnitude) < SpawnDistance )
        {
            if (Counter > 0)
            {
               Quaternion RotWasp = SafeRotation.LookRotationOrCurrent(Distance, transform.rotation);
               SpawnWasp(RotWasp);
                Counter--;
            }
            if (Counter <=0)
            {

                RealClock -= Time.deltaTime;
                if (RealClock <= 0)
                {
                    Counter = WaspCounter;
                    RealClock = SpawnClock;
                }
            }
        }

       else
        {
            Counter = 0;
            RealClock = 0;
        }
    }

    void SpawnWasp(Quaternion rotation)
    {
        var wasp = Instantiate(Wasp, Hive.transform.position, rotation);
        spawnedWasps.Add(wasp);

        if (!wasp.TryGetComponent(out Collider waspCollider))
        {
            return;
        }

        for (int i = 0; i < spawnedWaspColliders.Count; i++)
        {
            if (spawnedWaspColliders[i] != null)
            {
                Physics.IgnoreCollision(waspCollider, spawnedWaspColliders[i]);
            }
        }

        spawnedWaspColliders.Add(waspCollider);
    }
}
