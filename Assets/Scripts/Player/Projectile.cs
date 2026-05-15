using System.Collections.Generic;
using Beavermania.Debugging;
using Beavermania.Display;
using Beavermania.NPC;
using Beavermania.Objects;
using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;
using UnityEngine;
using UnityEngine.Pool;

namespace Beavermania.Player.Combat
{

    public class Projectile : MonoBehaviour
    {
        const int DefaultPoolCapacity = 8;
        const int MaxActiveInstances = 64;

        struct StoredIgnoreCollision
        {
            public Collider ProjectileCollider;
            public Collider OtherCollider;
        }

        static readonly Dictionary<Projectile, ObjectPool<Projectile>> Pools = new Dictionary<Projectile, ObjectPool<Projectile>>();

        public Rigidbody Ball;
        BeaverPlayer Player;
        public bool isArrow;
        public bool isFireBall;
        [SerializeField] AudioSource Sound;
        [SerializeField] float clock;
        [SerializeField] int forwardVel;
        [SerializeField] int upwardVel;
        [SerializeField] GameObject Explosion;
        [SerializeField] int Damage;
        [SerializeField] GameObject arrowPickup;
        [Tooltip("Do not spawn embedded arrow pickup until the arrow has flown at least this long (seconds).")]
        [SerializeField] float minPickupSpawnDelay = 0.25f;
        [Tooltip("If arrow prefab forwardVel is 0, use this speed so the arrow still launches.")]
        [SerializeField] int minArrowForwardSpeed = 28;
        [Tooltip("If arrow prefab lifetime (clock) is shorter than this, clamp so it does not expire instantly.")]
        [SerializeField] float minArrowFlightSeconds = 2.5f;
        [Tooltip("Ignore collision responses against the player hierarchy for this long after spawn (seconds), except gameplay tags (NPC, Scorpion, Hive, Isle).")]
        [SerializeField] float collisionResponseGrace = 0.16f;
        [Tooltip("When true, logs launch direction, velocity, first contact, and CompleteProjectile calls for arrows.")]
        [SerializeField] bool debugBowProjectile;
        [Tooltip("Optional roots (e.g. detached bow rig) whose colliders are ignored against the arrow.")]
        [SerializeField] Transform[] additionalArrowIgnoreRoots;
        [Tooltip("Local axis the arrow mesh tip points along (Arrow prefab mesh/capsule use +Y).")]
        [SerializeField] Vector3 arrowMeshTipLocalAxis = Vector3.up;
        [Tooltip("Optional visual-only child; mesh offset applies here so root Z stays flight-forward.")]
        [SerializeField] Transform arrowVisualMeshRoot;
        [Tooltip("Extra local euler on visual child when assigned (ignored on root).")]
        [SerializeField] Vector3 arrowVisualLocalRotationOffsetEuler = new Vector3(90f, 0f, 0f);
        [Tooltip("Extra euler after LookRotation when mesh lives on the projectile root (Y-tip mesh => 90,0,0).")]
        [SerializeField] Vector3 arrowMeshRotationOffsetEuler = new Vector3(90f, 0f, 0f);
        [SerializeField] float debugLaunchRayLength = 12f;
        Quaternion arrowVisualBaseLocalRotation = Quaternion.identity;

        ObjectPool<Projectile> pool;
        Collider[] colliders;
        readonly List<StoredIgnoreCollision> ownerIgnorePairs = new List<StoredIgnoreCollision>(128);
        float aliveTime;
        byte arrowRbDebugFixedStepsLogged;
        bool arrowLoggedFirstContact;
        TrailRenderer[] trails;
        AudioSource[] audioSources;
        float[] defaultVolumes;
        float[] defaultPitches;
        float initialClock;
        int initialDamage;
        int initialForwardVel;
        bool cachedDefaults;
        bool released;
        bool overflowInstance;

        public static Projectile Spawn(Projectile prefab, Vector3 position, Quaternion rotation)
        {
            return Spawn(prefab, position, rotation, null, null);
        }

        public static Projectile Spawn(Projectile prefab, Vector3 position, Quaternion rotation, Vector3? worldLaunchDirection, BeaverPlayer owner)
        {
            if (prefab == null)
                return null;

            if (!Pools.TryGetValue(prefab, out var pool))
            {
                pool = CreatePool(prefab);
                Pools.Add(prefab, pool);
            }

            if (pool.CountActive >= MaxActiveInstances)
                return SpawnOverflow(prefab, position, rotation, worldLaunchDirection, owner);

            var projectile = pool.Get();
            projectile.Launch(position, rotation, false, worldLaunchDirection, owner);
            return projectile;
        }

        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
                return null;

            if (!prefab.TryGetComponent(out Projectile projectilePrefab))
                return Instantiate(prefab, position, rotation);

            var projectile = Spawn(projectilePrefab, position, rotation);
            return projectile != null ? projectile.gameObject : null;
        }

        static ObjectPool<Projectile> CreatePool(Projectile prefab)
        {
            ObjectPool<Projectile> pool = null;
            pool = new ObjectPool<Projectile>(
                () => CreateInstance(prefab, pool),
                projectile =>
                {
                    projectile.released = false;
                    projectile.overflowInstance = false;
                    projectile.gameObject.SetActive(true);
                },
                projectile =>
                {
                    projectile.released = true;
                    projectile.ResetRuntimeState();
                    projectile.gameObject.SetActive(false);
                },
                projectile => Destroy(projectile.gameObject),
                true,
                DefaultPoolCapacity,
                MaxActiveInstances);

            return pool;
        }

        static Projectile CreateInstance(Projectile prefab, ObjectPool<Projectile> pool)
        {
            var projectile = Instantiate(prefab);
            projectile.Bind(pool);
            projectile.gameObject.SetActive(false);
            return projectile;
        }

        static Projectile SpawnOverflow(Projectile prefab, Vector3 position, Quaternion rotation, Vector3? worldLaunchDirection, BeaverPlayer owner)
        {
            var projectile = Instantiate(prefab);
            projectile.Bind(null);
            projectile.Launch(position, rotation, true, worldLaunchDirection, owner);
            return projectile;
        }

        void Awake()
        {
            CacheDefaults();
        }

        void Start()
        {
            // Launch is invoked only from Projectile.Spawn / SpawnOverflow.
        }

        void Bind(ObjectPool<Projectile> sourcePool)
        {
            pool = sourcePool;
            CacheDefaults();
        }

        void CacheDefaults()
        {
            if (cachedDefaults)
                return;

            if (Ball == null)
                Ball = GetComponent<Rigidbody>();

            colliders = GetComponentsInChildren<Collider>(true);
            trails = GetComponentsInChildren<TrailRenderer>(true);
            audioSources = GetComponentsInChildren<AudioSource>(true);
            defaultVolumes = new float[audioSources.Length];
            defaultPitches = new float[audioSources.Length];

            for (var i = 0; i < audioSources.Length; i++)
            {
                defaultVolumes[i] = audioSources[i].volume;
                defaultPitches[i] = audioSources[i].pitch;
            }

            initialClock = clock;
            if (isArrow && initialClock < 0.35f)
                initialClock = minArrowFlightSeconds;
            initialDamage = Damage;
            initialForwardVel = forwardVel;
            if (isArrow && Mathf.Abs(initialForwardVel) < minArrowForwardSpeed)
                initialForwardVel = minArrowForwardSpeed;
            if (arrowVisualMeshRoot != null)
                arrowVisualBaseLocalRotation = arrowVisualMeshRoot.localRotation;
            cachedDefaults = true;
        }

        Quaternion ResolveArrowFlightRotation(Vector3 launchDirection)
        {
            Vector3 dir = launchDirection.sqrMagnitude > 1e-8f ? launchDirection.normalized : Vector3.forward;
            Quaternion flight = Quaternion.LookRotation(dir, Vector3.up);

            if (arrowVisualMeshRoot != null)
                return flight;

            return flight * Quaternion.Euler(arrowMeshRotationOffsetEuler);
        }

        Quaternion ResolveArrowVisualLocalRotation()
        {
            if (arrowMeshTipLocalAxis.sqrMagnitude > 1e-8f)
            {
                Vector3 tipAxis = arrowMeshTipLocalAxis.normalized;
                if (Vector3.Dot(tipAxis, Vector3.forward) > 0.999f)
                    return Quaternion.identity;
                return Quaternion.FromToRotation(tipAxis, Vector3.forward);
            }

            return Quaternion.Euler(arrowVisualLocalRotationOffsetEuler);
        }

        void ApplyArrowVisualMeshOffset()
        {
            if (arrowVisualMeshRoot == null)
                return;

            arrowVisualMeshRoot.localRotation =
                arrowVisualBaseLocalRotation * ResolveArrowVisualLocalRotation();
        }

        void Launch(Vector3 position, Quaternion rotation, bool isOverflowInstance, Vector3? worldLaunchDirection, BeaverPlayer owner)
        {
            CacheDefaults();

            overflowInstance = isOverflowInstance;
            released = false;
            arrowRbDebugFixedStepsLogged = 0;
            arrowLoggedFirstContact = false;
            ResetRuntimeState();

            Vector3 launchDirection;
            if (isArrow && worldLaunchDirection.HasValue && worldLaunchDirection.Value.sqrMagnitude > 1e-8f)
            {
                launchDirection = worldLaunchDirection.Value.normalized;
            }
            else
            {
                launchDirection = (rotation * Vector3.forward).normalized;
            }

            if (launchDirection.sqrMagnitude < 1e-8f)
                launchDirection = Vector3.forward;

            Quaternion flightRotation = isArrow
                ? ResolveArrowFlightRotation(launchDirection)
                : rotation;

            transform.SetPositionAndRotation(position, flightRotation);
            if (isArrow)
                ApplyArrowVisualMeshOffset();

            if (owner != null)
                Player = owner;
            else
                ResetOwnerReference();

            if (isArrow)
                ApplyArrowOwnerCollisionIgnores();

            if (Ball != null)
            {
                if (isArrow)
                {
                    Ball.isKinematic = false;
                    Ball.detectCollisions = true;
                    Ball.constraints = RigidbodyConstraints.None;
                    Ball.WakeUp();

                    float arrowSpeed = Mathf.Abs(initialForwardVel) > 0f ? Mathf.Abs(initialForwardVel) : minArrowForwardSpeed;
                    Ball.velocity = launchDirection.normalized * arrowSpeed;
                }
                else
                {
                    Ball.velocity = launchDirection * forwardVel + Vector3.up * upwardVel;
                }
            }

            if (isArrow && debugLaunchRayLength > 0f)
                Debug.DrawRay(position, launchDirection * debugLaunchRayLength, Color.cyan, 2f);

            if (debugBowProjectile && isArrow)
            {
                var vel = Ball != null ? Ball.velocity : Vector3.zero;
                int spdDbg = Mathf.Abs(initialForwardVel) > 0 ? Mathf.Abs(initialForwardVel) : minArrowForwardSpeed;
                Debug.Log($"[Projectile] Launch dir={launchDirection} arrowSpeed={spdDbg} vel={vel} mag={vel.magnitude} kinematic={(Ball != null && Ball.isKinematic)} pos={position}", this);
            }

            // #region agent log
            if (isArrow)
            {
                var vel = Ball != null ? Ball.velocity : Vector3.zero;
                AgentDebugLog.Write("H2", "Projectile.Launch", "arrow launched",
                    $"{{\"speed\":{Mathf.Abs(initialForwardVel)},\"velMag\":{vel.magnitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"dir\":{AgentDebugLog.Vec3(launchDirection)},\"pos\":{AgentDebugLog.Vec3(position)},\"kinematic\":{(Ball != null && Ball.isKinematic).ToString().ToLowerInvariant()}}}");
            }
            // #endregion
        }

        void ResetRuntimeState()
        {
            ClearOwnerCollisionIgnores();
            aliveTime = 0f;
            arrowRbDebugFixedStepsLogged = 0;
            arrowLoggedFirstContact = false;

            clock = initialClock;
            Damage = initialDamage;
            forwardVel = initialForwardVel;
            Player = null;

            if (Ball != null)
            {
                Ball.velocity = Vector3.zero;
                Ball.angularVelocity = Vector3.zero;
            }

            if (colliders != null)
            {
                for (var i = 0; i < colliders.Length; i++)
                {
                    if (colliders[i] != null)
                        colliders[i].enabled = true;
                }
            }

            ResetAudioSources();

            if (trails != null)
            {
                for (var i = 0; i < trails.Length; i++)
                    trails[i].Clear();
            }

            if (arrowVisualMeshRoot != null)
                arrowVisualMeshRoot.localRotation = arrowVisualBaseLocalRotation;
        }

        void ResetAudioSources()
        {
            if (audioSources == null)
                return;

            for (var i = 0; i < audioSources.Length; i++)
            {
                audioSources[i].Stop();
                audioSources[i].volume = defaultVolumes[i];
                audioSources[i].pitch = defaultPitches[i];
            }
        }

        void ResetOwnerReference()
        {
            var playerObject = GameObject.FindGameObjectWithTag("Player");
            Player = playerObject != null ? playerObject.GetComponent<BeaverPlayer>() : null;
        }

        void ClearOwnerCollisionIgnores()
        {
            for (var i = 0; i < ownerIgnorePairs.Count; i++)
            {
                var pair = ownerIgnorePairs[i];
                if (pair.ProjectileCollider != null && pair.OtherCollider != null)
                    Physics.IgnoreCollision(pair.ProjectileCollider, pair.OtherCollider, false);
            }

            ownerIgnorePairs.Clear();
        }

        void ApplyArrowOwnerCollisionIgnores()
        {
            ClearOwnerCollisionIgnores();

            if (!isArrow || colliders == null || Player == null)
                return;

            var ignoreAgainst = new HashSet<Collider>();

            void AddCollidersUnder(Transform root)
            {
                if (root == null)
                    return;
                foreach (var c in root.GetComponentsInChildren<Collider>(true))
                {
                    if (c != null)
                        ignoreAgainst.Add(c);
                }
            }

            AddCollidersUnder(Player.transform);

            if (Player.Bow != null)
            {
                foreach (var bowObj in Player.Bow)
                {
                    if (bowObj == null)
                        continue;
                    AddCollidersUnder(bowObj.transform);
                }
            }

            if (additionalArrowIgnoreRoots != null)
            {
                for (var r = 0; r < additionalArrowIgnoreRoots.Length; r++)
                    AddCollidersUnder(additionalArrowIgnoreRoots[r]);
            }

            for (var i = 0; i < colliders.Length; i++)
            {
                var projectileCol = colliders[i];
                if (projectileCol == null)
                    continue;

                foreach (var other in ignoreAgainst)
                {
                    if (other == null || other == projectileCol)
                        continue;

                    Physics.IgnoreCollision(projectileCol, other, true);
                    ownerIgnorePairs.Add(new StoredIgnoreCollision
                    {
                        ProjectileCollider = projectileCol,
                        OtherCollider = other
                    });
                }
            }
        }

        bool ShouldSpawnEmbeddedArrowPickup()
        {
            return isArrow && aliveTime >= minPickupSpawnDelay;
        }

        private void Update()
        {
            aliveTime += Time.deltaTime;
            clock -= Time.deltaTime;
            if (clock <= 0)
                CompleteProjectile(ShouldSpawnEmbeddedArrowPickup());
        }

        void FixedUpdate()
        {
            if (!debugBowProjectile || !isArrow || released || Ball == null)
                return;

            if (arrowRbDebugFixedStepsLogged < 2)
            {
                Debug.Log($"[Projectile] FixedUpdate step={arrowRbDebugFixedStepsLogged} velocity={Ball.velocity} mag={Ball.velocity.magnitude:F2} kinematic={Ball.isKinematic} constraints={Ball.constraints}", this);
                arrowRbDebugFixedStepsLogged++;
            }
        }
        public void Explode()
        {
            PooledOneShotVfx.Spawn(Explosion, transform.position, transform.rotation);
            CompleteProjectile();
        }

        public void RockHit()
        {
            if (Sound == null)
                return;

            Sound.volume = 0.2f;
            Sound.pitch = 0.8f;
            Sound.PlayOneShot(Sound.clip);
        }

        void CompleteProjectile(bool spawnArrowPickup = false)
        {
            if (released)
                return;

            if (debugBowProjectile && isArrow)
                Debug.Log($"[Projectile] CompleteProjectile spawnPickup={spawnArrowPickup} aliveTime={aliveTime:F3} clock={clock:F3} pos={transform.position}", this);

            // #region agent log
            if (isArrow)
            {
                AgentDebugLog.Write("H3", "Projectile.CompleteProjectile", "arrow completed",
                    $"{{\"spawnPickup\":{spawnArrowPickup.ToString().ToLowerInvariant()},\"aliveTime\":{aliveTime.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"pos\":{AgentDebugLog.Vec3(transform.position)}}}");
            }
            // #endregion

            if (spawnArrowPickup)
                SpawnArrowPickup();

            if (pool != null && !overflowInstance)
            {
                pool.Release(this);
                return;
            }

            released = true;
            Destroy(gameObject);
        }

        void SpawnArrowPickup()
        {
            if (arrowPickup != null)
                Instantiate(arrowPickup, transform.position, Quaternion.identity);
        }

        private void OnCollisionEnter(Collision OBJ)
        {
            if (released)
                return;

            if (debugBowProjectile && isArrow && !arrowLoggedFirstContact)
            {
                arrowLoggedFirstContact = true;
                Debug.Log($"[Projectile] FirstCollision with='{OBJ.collider?.name}' root='{OBJ.collider?.transform.root?.name}' aliveTime={aliveTime:F3}", this);
            }

            if (Player != null && OBJ.collider != null && OBJ.collider.GetComponentInParent<BeaverPlayer>() == Player)
                return;

            if (isArrow && aliveTime < collisionResponseGrace && Player != null && OBJ.collider != null
                && OBJ.collider.transform.IsChildOf(Player.transform))
            {
                var go = OBJ.gameObject;
                if (!go.CompareTag("NPC") && !go.CompareTag("Scorpion") && !go.CompareTag("Hive") && !go.CompareTag("Isle"))
                {
                    if (debugBowProjectile)
                        Debug.Log($"[Projectile] Grace ignore contact='{OBJ.collider.name}' aliveTime={aliveTime:F3}", this);
                    return;
                }
            }

            var hit = false;

            if (OBJ.gameObject.CompareTag("NPC"))
            {
                hit = true;
                if (OBJ.gameObject.TryGetComponent(out NPC_Basic npc))
                {
                    npc.TakeDamage(Damage);
                    npc.combo += npc.hit2stun;
                }
                if (isFireBall == true)
                {
                    Explode();
                }
                if (isFireBall == false)
                {
                    RockHit();
                }
                if (Player != null)
                {
                    Player.Plattering = "Bam! Take that";
                    Player.ChangeSpeech = 1f;
                }
            }
            if (OBJ.gameObject.CompareTag("Scorpion"))
            {
                hit = true;
                if (OBJ.gameObject.TryGetComponent(out ScorpionScript scorpion))
                {
                    scorpion.TakeDamage(Damage);
                    scorpion.combo += 3;
                }
                if (isFireBall == true)
                {
                    Explode();
                }
                if (isFireBall == false)
                {
                    RockHit();
                }
            }
            if (OBJ.gameObject.CompareTag("Hive"))
            {
                hit = true;
                if (OBJ.gameObject.TryGetComponent(out Static_Hive hive))
                {
                    hive.TakeDamage(Damage);
                }
                if (isFireBall == true)
                {
                    Explode();
                }
                if (isFireBall == false)
                {
                    RockHit();
                }
            }
            if (OBJ.gameObject.CompareTag("Isle"))
            {
                hit = true;
                if (isFireBall == true)
                {
                    Explode();
                }
                if (isFireBall == false)
                {
                    RockHit();
                }
            }

            if (hit && isFireBall == false)
                CompleteProjectile(ShouldSpawnEmbeddedArrowPickup());
        }
    }
}
