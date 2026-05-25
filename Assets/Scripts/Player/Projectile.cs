using System.Collections.Generic;
using Beavermania.Display;
using Beavermania.NPC;
using Beavermania.Objects;
using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;
using UnityEngine;
using UnityEngine.Pool;

namespace Beavermania.Player.Combat
{

    public enum ProjectileType
    {
        Generic = 0,
        Arrow = 1,
        FireBreath = 2,
    }

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
        [SerializeField] ProjectileType projectileType = ProjectileType.Generic;
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
        [Tooltip("Push embedded pickup slightly off the hit surface (meters).")]
        [SerializeField] float pickupSurfaceOffset = 0.06f;
        [Header("FireBreath AoE")]
        [Tooltip("AoE radius at impact (meters).")]
        [SerializeField] float aoeRadius = 6f;
        [Tooltip("AoE damage per enemy. When 0, uses the projectile Damage value.")]
        [SerializeField] int aoeDamage;
        [Tooltip("Layers scanned for enemies in the explosion radius.")]
        [SerializeField] LayerMask enemyLayerMask;
        [Tooltip("Optional explosion VFX prefab spawned at impact.")]
        [SerializeField] GameObject explosionPrefab;
        [Tooltip("Fallback fireball speed when forwardVel is 0.")]
        [SerializeField] int minFireballForwardSpeed = 30;
        [Tooltip("Owner collision grace for FireBreath (seconds).")]
        [SerializeField] float fireBreathOwnerCollisionGrace = 0.15f;
        [Tooltip("Minimum travel from launch before FireBreath can explode on world geometry.")]
        [SerializeField] float fireBreathMinTravelBeforeWorldImpact = 2f;
        [Tooltip("Logs FireBreath launch velocity and first collision target.")]
        [SerializeField] bool debugFireBreath;
        Quaternion arrowVisualBaseLocalRotation = Quaternion.identity;
        Vector3 fireBreathLaunchPosition;
        bool fireBreathLoggedFirstCollision;

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
        bool impactResolved;
        bool HasExploded => impactResolved;

        bool UsesFireBreathLogic =>
            projectileType == ProjectileType.FireBreath || (isFireBall && !isArrow);

        bool UsesArrowLogic =>
            projectileType == ProjectileType.Arrow || (isArrow && !UsesFireBreathLogic);

        void EnforceProjectileTypeFlags()
        {
            if (UsesFireBreathLogic)
            {
                projectileType = ProjectileType.FireBreath;
                isFireBall = true;
                isArrow = false;
                return;
            }

            if (UsesArrowLogic)
            {
                projectileType = ProjectileType.Arrow;
                isArrow = true;
                isFireBall = false;
            }
        }

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
            EnforceProjectileTypeFlags();

            overflowInstance = isOverflowInstance;
            released = false;
            impactResolved = false;
            arrowRbDebugFixedStepsLogged = 0;
            arrowLoggedFirstContact = false;
            fireBreathLoggedFirstCollision = false;
            ResetRuntimeState();

            Vector3 launchDirection;
            if (worldLaunchDirection.HasValue && worldLaunchDirection.Value.sqrMagnitude > 1e-8f)
                launchDirection = worldLaunchDirection.Value.normalized;
            else
                launchDirection = (rotation * Vector3.forward).normalized;

            if (launchDirection.sqrMagnitude < 1e-8f)
                launchDirection = Vector3.forward;

            Quaternion flightRotation = UsesArrowLogic
                ? ResolveArrowFlightRotation(launchDirection)
                : Quaternion.LookRotation(launchDirection, Vector3.up);

            transform.SetPositionAndRotation(position, flightRotation);
            if (UsesArrowLogic)
                ApplyArrowVisualMeshOffset();

            if (UsesFireBreathLogic)
                fireBreathLaunchPosition = position;

            if (owner != null)
                Player = owner;
            else
                ResetOwnerReference();

            if (UsesArrowLogic || UsesFireBreathLogic)
                ApplyOwnerCollisionIgnores();

            if (Ball != null)
            {
                if (UsesArrowLogic)
                {
                    Ball.isKinematic = false;
                    Ball.detectCollisions = true;
                    Ball.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                    Ball.constraints = RigidbodyConstraints.None;
                    Ball.WakeUp();

                    float arrowSpeed = Mathf.Abs(initialForwardVel) > 0f ? Mathf.Abs(initialForwardVel) : minArrowForwardSpeed;
                    Ball.velocity = launchDirection.normalized * arrowSpeed;
                }
                else if (UsesFireBreathLogic)
                {
                    Ball.isKinematic = false;
                    Ball.detectCollisions = true;
                    Ball.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                    Ball.constraints = RigidbodyConstraints.None;
                    Ball.useGravity = false;
                    Ball.velocity = Vector3.zero;
                    Ball.angularVelocity = Vector3.zero;
                    Ball.WakeUp();

                    float fireSpeed = Mathf.Abs(initialForwardVel) > 0f ? Mathf.Abs(initialForwardVel) : minFireballForwardSpeed;
                    Ball.velocity = launchDirection * fireSpeed;

                    if (debugFireBreath)
                    {
                        Debug.Log(
                            $"[Projectile] FireBreath launch pos={position} dir={launchDirection} speed={fireSpeed} vel={Ball.velocity}",
                            this);
                        Debug.DrawRay(position, launchDirection * 24f, Color.magenta, 3f);
                    }
                }
                else
                {
                    Ball.velocity = launchDirection * forwardVel + Vector3.up * upwardVel;
                }
            }

            if (UsesArrowLogic && debugLaunchRayLength > 0f)
                Debug.DrawRay(position, launchDirection * debugLaunchRayLength, Color.cyan, 2f);

            if (debugBowProjectile && UsesArrowLogic)
            {
                var vel = Ball != null ? Ball.velocity : Vector3.zero;
                int spdDbg = Mathf.Abs(initialForwardVel) > 0 ? Mathf.Abs(initialForwardVel) : minArrowForwardSpeed;
                Debug.Log($"[Projectile] Launch dir={launchDirection} arrowSpeed={spdDbg} vel={vel} mag={vel.magnitude} kinematic={(Ball != null && Ball.isKinematic)} pos={position}", this);
            }

        }

        void ResetRuntimeState()
        {
            ClearOwnerCollisionIgnores();
            aliveTime = 0f;
            impactResolved = false;
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
                {
                    if (trails[i] == null)
                        continue;

                    trails[i].Clear();
                }
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

        void ApplyOwnerCollisionIgnores()
        {
            ClearOwnerCollisionIgnores();

            if ((!UsesArrowLogic && !UsesFireBreathLogic) || colliders == null || Player == null)
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
            Player.CollectOwnerProjectileIgnoreColliders(ignoreAgainst);

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
            return UsesArrowLogic && aliveTime >= minPickupSpawnDelay;
        }

        static bool IsGivingTreeCollider(Collider collider)
        {
            return collider != null && collider.GetComponentInParent<LogSpawner>() != null;
        }

        bool IsPriorityFireBreathTarget(Collider collider)
        {
            if (collider == null)
                return false;

            if (ColliderHasTag(collider, "NPC")
                || ColliderHasTag(collider, "Scorpion")
                || ColliderHasTag(collider, "Hive"))
            {
                return true;
            }

            return GetComponentInParentSafe<NPC_Basic>(collider) != null
                || GetComponentInParentSafe<ScorpionScript>(collider) != null
                || GetComponentInParentSafe<Static_Hive>(collider) != null;
        }

        bool CanFireBreathExplodeFromCollision(Collision collision)
        {
            if (!UsesFireBreathLogic)
                return true;

            if (collision == null || collision.collider == null)
                return true;

            if (ShouldIgnoreOwnerCollision(collision))
                return false;

            if (IsPriorityFireBreathTarget(collision.collider))
                return true;

            float minTravel = Mathf.Max(0.5f, fireBreathMinTravelBeforeWorldImpact);
            float traveled = Vector3.Distance(transform.position, fireBreathLaunchPosition);
            return traveled >= minTravel;
        }

        static T GetComponentInParentSafe<T>(Collider collider) where T : Component
        {
            return collider != null ? collider.GetComponentInParent<T>() : null;
        }

        static bool ColliderHasTag(Collider collider, string tag)
        {
            if (collider == null || string.IsNullOrEmpty(tag))
                return false;

            if (collider.CompareTag(tag))
                return true;

            Transform current = collider.transform.parent;
            while (current != null)
            {
                if (current.CompareTag(tag))
                    return true;
                current = current.parent;
            }

            return false;
        }

        bool IsOwnerCollider(Collider collider)
        {
            return Player != null
                && collider != null
                && collider.GetComponentInParent<BeaverPlayer>() == Player;
        }

        bool ShouldIgnoreOwnerCollision(Collision collision)
        {
            if (collision == null || collision.collider == null)
                return true;

            if (IsOwnerCollider(collision.collider))
                return true;

            float ownerGrace = UsesFireBreathLogic
                ? Mathf.Max(0.05f, fireBreathOwnerCollisionGrace)
                : collisionResponseGrace;

            if (aliveTime < ownerGrace
                && Player != null
                && collision.collider.transform.IsChildOf(Player.transform)
                && !ColliderHasTag(collision.collider, "NPC")
                && !ColliderHasTag(collision.collider, "Scorpion")
                && !ColliderHasTag(collision.collider, "Hive")
                && !ColliderHasTag(collision.collider, "Isle"))
            {
                return true;
            }

            return false;
        }

        void TryApplyArrowGameplayDamage(Collision collision)
        {
            if (collision == null || collision.collider == null)
                return;

            Collider hitCollider = collision.collider;

            NPC_Basic npc = GetComponentInParentSafe<NPC_Basic>(hitCollider);
            if (npc != null)
            {
                npc.TakeDamage(Damage);
                npc.combo += npc.hit2stun;
                ArrowHit();
                if (Player != null)
                {
                    Player.Plattering = "Bam! Take that";
                    Player.ChangeSpeech = 1f;
                }
                return;
            }

            ScorpionScript scorpion = GetComponentInParentSafe<ScorpionScript>(hitCollider);
            if (scorpion != null)
            {
                scorpion.TakeDamage(Damage);
                scorpion.combo += 3;
                ArrowHit();
                return;
            }

            Static_Hive hive = GetComponentInParentSafe<Static_Hive>(hitCollider);
            if (hive != null)
            {
                hive.TakeDamage(Damage);
                ArrowHit();
                return;
            }

            if (ColliderHasTag(hitCollider, "Isle"))
                ArrowHit();
        }

        void FinalizeArrowImpact(Collision collision, bool spawnPickup)
        {
            if (!UsesArrowLogic || impactResolved || released)
                return;

            impactResolved = true;

            if (Ball != null)
            {
                Ball.velocity = Vector3.zero;
                Ball.angularVelocity = Vector3.zero;
                Ball.isKinematic = true;
            }

            if (spawnPickup)
                SpawnArrowPickup(collision);

            CompleteProjectile(false);
        }

        private void Update()
        {
            if (released)
                return;

            aliveTime += Time.deltaTime;
            clock -= Time.deltaTime;
            if (clock <= 0)
            {
                if (UsesArrowLogic && !impactResolved)
                    FinalizeArrowImpact(null, ShouldSpawnEmbeddedArrowPickup());
                else if (UsesFireBreathLogic && !HasExploded)
                    Explode(null);
                else
                    CompleteProjectile(false);
            }
        }

        void FixedUpdate()
        {
            if (!debugBowProjectile || !UsesArrowLogic || released || Ball == null)
                return;

            if (arrowRbDebugFixedStepsLogged < 2)
            {
                Debug.Log($"[Projectile] FixedUpdate step={arrowRbDebugFixedStepsLogged} velocity={Ball.velocity} mag={Ball.velocity.magnitude:F2} kinematic={Ball.isKinematic} constraints={Ball.constraints}", this);
                arrowRbDebugFixedStepsLogged++;
            }
        }
        int ResolveAoeDamage()
        {
            return aoeDamage > 0 ? aoeDamage : Damage;
        }

        Vector3 ResolveImpactPoint(Collision collision)
        {
            if (collision != null && collision.contactCount > 0)
                return collision.GetContact(0).point;

            return transform.position;
        }

        GameObject ResolveExplosionPrefab()
        {
            if (explosionPrefab != null)
                return explosionPrefab;

            return Explosion;
        }

        void Explode(Collision collision)
        {
            if (!UsesFireBreathLogic || HasExploded || released)
                return;

            impactResolved = true;

            if (Ball != null)
            {
                Ball.velocity = Vector3.zero;
                Ball.angularVelocity = Vector3.zero;
                Ball.isKinematic = true;
            }

            Vector3 impactPoint = ResolveImpactPoint(collision);
            Quaternion impactRotation = transform.rotation;
            if (collision != null && collision.contactCount > 0)
            {
                Vector3 normal = collision.GetContact(0).normal;
                if (normal.sqrMagnitude > 1e-8f)
                    impactRotation = Quaternion.LookRotation(-normal, Vector3.up);
            }

            GameObject vfx = ResolveExplosionPrefab();
            if (vfx != null)
                PooledOneShotVfx.Spawn(vfx, impactPoint, impactRotation);

            ApplyAoEDamage(impactPoint);
            ApplyFireBreathGivingTreeDestruction(collision, impactPoint);
            FireExplosionHit();
            CompleteProjectile(false);
        }

        void ApplyFireBreathGivingTreeDestruction(Collision collision, Vector3 impactPoint)
        {
            if (!UsesFireBreathLogic)
                return;

            Transform destroyReference = Player != null ? Player.transform : transform;
            var destroyedTrees = new HashSet<int>();

            void TryDestroyTree(LogSpawner spawner)
            {
                if (spawner == null || !destroyedTrees.Add(spawner.GetInstanceID()))
                    return;

                spawner.DestroyTree(destroyReference);
            }

            if (collision != null && collision.collider != null)
                TryDestroyTree(collision.collider.GetComponentInParent<LogSpawner>());

            float radius = Mathf.Max(0.1f, aoeRadius);
            Collider[] hits = Physics.OverlapSphere(
                impactPoint,
                radius,
                ~0,
                QueryTriggerInteraction.Collide);

            if (hits == null)
                return;

            for (var i = 0; i < hits.Length; i++)
            {
                Collider hitCollider = hits[i];
                if (hitCollider == null)
                    continue;

                TryDestroyTree(hitCollider.GetComponentInParent<LogSpawner>());
            }
        }

        void ApplyAoEDamage(Vector3 impactPoint)
        {
            if (!UsesFireBreathLogic)
                return;

            float radius = Mathf.Max(0.1f, aoeRadius);
            LayerMask mask = enemyLayerMask.value != 0
                ? enemyLayerMask
                : (Player != null && Player.enemyLayers.value != 0 ? Player.enemyLayers : (LayerMask)~0);

            Collider[] hits = Physics.OverlapSphere(impactPoint, radius, mask, QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0)
                return;

            int damageAmount = ResolveAoeDamage();
            var damagedTargets = new HashSet<int>();

            for (var i = 0; i < hits.Length; i++)
            {
                Collider hitCollider = hits[i];
                if (hitCollider == null || IsOwnerCollider(hitCollider) || IsGivingTreeCollider(hitCollider))
                    continue;

                if (!IsPriorityFireBreathTarget(hitCollider))
                    continue;

                NPC_Basic npc = GetComponentInParentSafe<NPC_Basic>(hitCollider);
                if (npc != null && damagedTargets.Add(npc.GetInstanceID()))
                {
                    npc.TakeDamage(damageAmount);
                    npc.combo += npc.hit2stun;
                    continue;
                }

                ScorpionScript scorpion = GetComponentInParentSafe<ScorpionScript>(hitCollider);
                if (scorpion != null && damagedTargets.Add(scorpion.GetInstanceID()))
                {
                    scorpion.TakeDamage(damageAmount);
                    scorpion.combo += 3;
                    continue;
                }

                Static_Hive hive = GetComponentInParentSafe<Static_Hive>(hitCollider);
                if (hive != null && damagedTargets.Add(hive.GetInstanceID()))
                    hive.TakeDamage(damageAmount);
            }
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (!UsesFireBreathLogic || aoeRadius <= 0f)
                return;

            Gizmos.color = new Color(1f, 0.45f, 0.1f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, aoeRadius);
        }
#endif

        void PlayImpactSound(float volume, float pitch)
        {
            if (Sound == null || Sound.clip == null)
                return;

            Sound.volume = volume;
            Sound.pitch = pitch;
            Sound.PlayOneShot(Sound.clip);
        }

        public void RockHit()
        {
            PlayImpactSound(0.2f, 0.8f);
        }

        public void ArrowHit()
        {
            PlayImpactSound(0.25f, 1f);
        }

        public void FireExplosionHit()
        {
            PlayImpactSound(0.35f, 0.9f);
        }

        void CompleteProjectile(bool spawnArrowPickup = false)
        {
            if (released)
                return;

            released = true;

            if (debugBowProjectile && UsesArrowLogic)
                Debug.Log($"[Projectile] CompleteProjectile spawnPickup={spawnArrowPickup} aliveTime={aliveTime:F3} clock={clock:F3} pos={transform.position}", this);

            if (spawnArrowPickup)
                SpawnArrowPickup(null);

            if (pool != null && !overflowInstance)
            {
                pool.Release(this);
                return;
            }

            Destroy(gameObject);
        }

        GameObject ResolveArrowPickupPrefab()
        {
            if (!UsesArrowLogic || UsesFireBreathLogic)
                return null;

            if (arrowPickup != null)
                return arrowPickup;

            if (Player != null && Player.ArrowPickup != null)
                return Player.ArrowPickup;

            return null;
        }

        void SpawnArrowPickup(Collision collision)
        {
            GameObject pickupPrefab = ResolveArrowPickupPrefab();
            if (pickupPrefab == null)
                return;

            Vector3 spawnPosition = transform.position;
            Quaternion spawnRotation = transform.rotation;

            if (collision != null && collision.contactCount > 0)
            {
                ContactPoint contact = collision.GetContact(0);
                Vector3 normal = contact.normal.sqrMagnitude > 1e-8f
                    ? contact.normal
                    : -transform.forward;
                spawnPosition = contact.point + normal * Mathf.Max(0f, pickupSurfaceOffset);
                spawnRotation = Quaternion.LookRotation(-normal, Vector3.up) * Quaternion.Euler(arrowMeshRotationOffsetEuler);
            }

            Instantiate(pickupPrefab, spawnPosition, spawnRotation);
        }

        private void OnCollisionEnter(Collision OBJ)
        {
            if (released || impactResolved)
                return;

            if (debugBowProjectile && UsesArrowLogic && !arrowLoggedFirstContact)
            {
                arrowLoggedFirstContact = true;
                Debug.Log($"[Projectile] FirstCollision with='{OBJ.collider?.name}' root='{OBJ.collider?.transform.root?.name}' aliveTime={aliveTime:F3}", this);
            }

            if (UsesFireBreathLogic)
            {
                if (!CanFireBreathExplodeFromCollision(OBJ))
                {
                    if (debugFireBreath)
                    {
                        Debug.Log(
                            $"[Projectile] FireBreath ignored collision with='{OBJ.collider?.name}' layer={OBJ.collider?.gameObject.layer} traveled={Vector3.Distance(transform.position, fireBreathLaunchPosition):F2} alive={aliveTime:F3}",
                            this);
                    }

                    return;
                }

                if (debugFireBreath && !fireBreathLoggedFirstCollision)
                {
                    fireBreathLoggedFirstCollision = true;
                    var vel = Ball != null ? Ball.velocity : Vector3.zero;
                    Debug.Log(
                        $"[Projectile] FireBreath firstCollision with='{OBJ.collider?.name}' layer={OBJ.collider?.gameObject.layer} vel={vel} traveled={Vector3.Distance(transform.position, fireBreathLaunchPosition):F2}",
                        this);
                }

                Explode(OBJ);
                return;
            }

            if (UsesArrowLogic)
            {
                if (ShouldIgnoreOwnerCollision(OBJ))
                {
                    if (debugBowProjectile)
                        Debug.Log($"[Projectile] Ignored collision with='{OBJ.collider?.name}' aliveTime={aliveTime:F3}", this);
                    return;
                }

                TryApplyArrowGameplayDamage(OBJ);
                FinalizeArrowImpact(OBJ, spawnPickup: true);
                return;
            }

            if (IsOwnerCollider(OBJ.collider))
                return;

            var hit = false;

            if (ColliderHasTag(OBJ.collider, "NPC"))
            {
                hit = true;
                NPC_Basic npc = GetComponentInParentSafe<NPC_Basic>(OBJ.collider);
                if (npc != null)
                {
                    npc.TakeDamage(Damage);
                    npc.combo += npc.hit2stun;
                }
                if (UsesFireBreathLogic)
                    Explode(OBJ);
                else
                    RockHit();
                if (Player != null)
                {
                    Player.Plattering = "Bam! Take that";
                    Player.ChangeSpeech = 1f;
                }
            }
            if (ColliderHasTag(OBJ.collider, "Scorpion"))
            {
                hit = true;
                ScorpionScript scorpion = GetComponentInParentSafe<ScorpionScript>(OBJ.collider);
                if (scorpion != null)
                {
                    scorpion.TakeDamage(Damage);
                    scorpion.combo += 3;
                }
                if (UsesFireBreathLogic)
                    Explode(OBJ);
                else
                    RockHit();
            }
            if (ColliderHasTag(OBJ.collider, "Hive"))
            {
                hit = true;
                Static_Hive hive = GetComponentInParentSafe<Static_Hive>(OBJ.collider);
                if (hive != null)
                    hive.TakeDamage(Damage);
                if (UsesFireBreathLogic)
                    Explode(OBJ);
                else
                    RockHit();
            }
            if (ColliderHasTag(OBJ.collider, "Isle"))
            {
                hit = true;
                if (UsesFireBreathLogic)
                    Explode(OBJ);
                else
                    RockHit();
            }

            if (hit && !UsesFireBreathLogic)
                CompleteProjectile(false);
        }
    }
}
