using System.Collections.Generic;
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

        ObjectPool<Projectile> pool;
        Camera cachedMainCamera;
        Collider[] colliders;
        TrailRenderer[] trails;
        AudioSource[] audioSources;
        float[] defaultVolumes;
        float[] defaultPitches;
        float initialClock;
        int initialDamage;
        bool cachedDefaults;
        bool launched;
        bool released;
        bool overflowInstance;

        public static Projectile Spawn(Projectile prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
                return null;

            if (!Pools.TryGetValue(prefab, out var pool))
            {
                pool = CreatePool(prefab);
                Pools.Add(prefab, pool);
            }

            if (pool.CountActive >= MaxActiveInstances)
                return SpawnOverflow(prefab, position, rotation);

            var projectile = pool.Get();
            projectile.Launch(position, rotation, false);
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

        static Projectile SpawnOverflow(Projectile prefab, Vector3 position, Quaternion rotation)
        {
            var projectile = Instantiate(prefab);
            projectile.Bind(null);
            projectile.Launch(position, rotation, true);
            return projectile;
        }

        void Awake()
        {
            CacheDefaults();
        }

        void Start()
        {
            if (!launched)
                Launch(transform.position, transform.rotation, true);
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
            initialDamage = Damage;
            cachedDefaults = true;
        }

        void Launch(Vector3 position, Quaternion rotation, bool isOverflowInstance)
        {
            CacheDefaults();

            overflowInstance = isOverflowInstance;
            launched = true;
            released = false;
            transform.SetPositionAndRotation(position, rotation);
            ResetRuntimeState();

            cachedMainCamera = Camera.main;
            var launchDirection = cachedMainCamera != null
                ? cachedMainCamera.transform.TransformDirection(Vector3.forward)
                : transform.forward;

            Physics.IgnoreLayerCollision(1, 3);
            ResetOwnerReference();

            if (Ball != null)
                Ball.velocity = launchDirection * forwardVel + Vector3.up * upwardVel;
        }

        void ResetRuntimeState()
        {
            clock = initialClock;
            Damage = initialDamage;
            Player = null;

            if (Ball != null)
            {
                Ball.velocity = Vector3.zero;
                Ball.angularVelocity = Vector3.zero;
            }

            for (var i = 0; i < colliders.Length; i++)
                colliders[i].enabled = true;

            ResetAudioSources();

            for (var i = 0; i < trails.Length; i++)
                trails[i].Clear();
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

        private void Update()
        {
            clock -= Time.deltaTime;
            if (clock <= 0)
            {
                if (isArrow == true)
                    Instantiate(arrowPickup, transform.position, Quaternion.identity);

                CompleteProjectile();
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

        void CompleteProjectile()
        {
            if (released)
                return;

            if (pool != null && !overflowInstance)
            {
                pool.Release(this);
                return;
            }

            released = true;
            Destroy(gameObject);
        }

        private void OnCollisionEnter(Collision OBJ)
        {
            if (released)
                return;

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
                CompleteProjectile();
        }
    }
}
