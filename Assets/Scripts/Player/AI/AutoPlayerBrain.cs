using Beavermania.Core.Input;
using Beavermania.NPC;
using Beavermania.Objects;
using UnityEngine;

namespace Beavermania.Player.AI
{
    public enum AutoPlayerState
    {
        Idle,
        Explore,
        MoveToTarget,
        AvoidDanger,
        Combat,
        InteractWithNPC,
        ChopTree,
        CollectLog,
        CarryLog,
        BuildBridge,
        RecoverFromStuck
    }

    [DisallowMultipleComponent]
    public class AutoPlayerBrain : MonoBehaviour
    {
        const float LookRotationEpsilon = 1e-6f;
        const float CombatSafeDistance = 3.5f;
        const float CombatAttackDistance = 2.2f;
        const float ChopDistance = 2.5f;
        const float LogPickupDistance = 2f;
        const float LowHealthDefendRatio = 0.35f;

        [Header("Enable")]
        [SerializeField] bool autoPlayerEnabled;
        [SerializeField] bool toggleWithF8 = true;

        [Header("Timing")]
        [SerializeField] float decisionInterval = 0.2f;
        [SerializeField] float interactionCooldown = 2f;
        [SerializeField] float exploreRadius = 18f;

        [Header("Distances (mirror movement agent if unset)")]
        [SerializeField] float interactionDistance = 1.8f;

        [Header("Debug")]
        [SerializeField] bool debugMode;

        [Header("Components")]
        [SerializeField] AutoPlayerPerception perception;
        [SerializeField] AutoPlayerMovementAgent movement;
        [SerializeField] AutoPlayerActionAdapter adapter;
        [SerializeField] AutoPlayerTaskMemory memory;
        [SerializeField] AutoPlayerDebugHUD debugHud;

        AutoPlayerState _state = AutoPlayerState.Idle;
        AutoPlayerState _previousState;
        Transform _currentTarget;
        Vector3 _exploreDestination;
        float _decisionTimer;
        float _interactionTimer;
        float _recoveryPhaseTimer;
        int _recoveryPhase;
        int _currentPriority;
        bool _loggedMissingAdapter;

        public bool AutoPlayerEnabled => autoPlayerEnabled;
        public AutoPlayerState State => _state;
        public Transform CurrentTarget => _currentTarget;
        public int CurrentPriority => _currentPriority;

        void Awake()
        {
            if (perception == null)
                perception = GetComponent<AutoPlayerPerception>();
            if (movement == null)
                movement = GetComponent<AutoPlayerMovementAgent>();
            if (adapter == null)
                adapter = GetComponent<AutoPlayerActionAdapter>();
            if (memory == null)
                memory = GetComponent<AutoPlayerTaskMemory>();
            if (debugHud == null)
                debugHud = GetComponent<AutoPlayerDebugHUD>();
        }

        void Update()
        {
            if (toggleWithF8 && Input.GetKeyDown(KeyCode.F8))
                SetAutoPlayerEnabled(!autoPlayerEnabled);

            if (!autoPlayerEnabled)
            {
                if (PlayerInputOverride.IsActive)
                    DisableAutoPlayer();
                return;
            }

            PlayerInputOverride.ClearFramePulses();
            EnableAutoPlayerDrive();

            if (adapter == null)
            {
                if (!_loggedMissingAdapter)
                {
                    _loggedMissingAdapter = true;
                    Debug.LogWarning($"{nameof(AutoPlayerBrain)} on '{name}' is missing {nameof(AutoPlayerActionAdapter)}.", this);
                }
                return;
            }

            if (adapter.IsGameplayBlocked())
            {
                movement?.ClearDestination();
                adapter.ClearActionRequests();
                adapter.ApplyToInputOverride();
                PlayerInputOverride.ClearFramePulses();
                return;
            }

            _decisionTimer -= Time.deltaTime;
            if (_decisionTimer <= 0f)
            {
                _decisionTimer = decisionInterval;
                RunDecision();
            }

            TickActiveState();
            adapter.ApplyToInputOverride();

            if (debugHud != null)
                debugHud.SyncFromBrain(this, movement, adapter, memory);
        }

        public void SetAutoPlayerEnabled(bool enabled)
        {
            autoPlayerEnabled = enabled;
            if (!enabled)
                DisableAutoPlayer();
        }

        void EnableAutoPlayerDrive()
        {
            PlayerInputOverride.SetActive(true);
        }

        void DisableAutoPlayer()
        {
            PlayerInputOverride.SetActive(false);
            PlayerInputOverride.ClearAll();
            movement?.ClearDestination();
            _state = AutoPlayerState.Idle;
            _currentTarget = null;
        }

        void RunDecision()
        {
            perception?.Scan(memory);
            if (adapter != null && adapter.Carry != null)
                memory?.SetCarriedLogCount(adapter.CarriedLogCount);

            if (movement != null && movement.IsStuck && _state != AutoPlayerState.RecoverFromStuck)
            {
                TransitionTo(AutoPlayerState.RecoverFromStuck, 1);
                return;
            }

            if (perception != null && perception.NearestThreat != null && perception.NearestThreatDistance < perception.CombatDetectionRadius * 0.85f)
            {
                if (perception.NearestThreatDistance < CombatSafeDistance * 0.75f)
                    TransitionTo(AutoPlayerState.AvoidDanger, 1, perception.NearestThreat);
                else
                    TransitionTo(AutoPlayerState.Combat, 2, perception.NearestThreat);
                return;
            }

            if (adapter != null && adapter.IsCarryingLogs && perception != null && perception.NearestBridge != null)
            {
                TransitionTo(AutoPlayerState.BuildBridge, 3, perception.NearestBridge);
                return;
            }

            if (adapter != null && adapter.CanCarryMore && perception != null && perception.NearestLog != null)
            {
                TransitionTo(AutoPlayerState.CollectLog, 4, perception.NearestLog);
                return;
            }

            if (perception != null && perception.NearestTree != null && (adapter == null || adapter.CanCarryMore))
            {
                TransitionTo(AutoPlayerState.ChopTree, 5, perception.NearestTree);
                return;
            }

            if (perception != null && perception.NearestInteractable != null && _interactionTimer <= 0f)
            {
                IAutoInteractable interactable = perception.NearestInteractable.GetComponent<IAutoInteractable>();
                if (interactable == null || !interactable.IsAutoInteractionBusy())
                {
                    TransitionTo(AutoPlayerState.InteractWithNPC, 6, perception.NearestInteractable);
                    return;
                }
            }

            if (_state == AutoPlayerState.Idle || _state == AutoPlayerState.MoveToTarget || !movement.HasDestination)
                TransitionTo(AutoPlayerState.Explore, 10, null);
        }

        void TransitionTo(AutoPlayerState newState, int priority, Transform target = null)
        {
            if (newState == _state && _currentTarget == target)
                return;

            _previousState = _state;
            _state = newState;
            _currentPriority = priority;
            _currentTarget = target;

            if (debugMode)
                Debug.Log($"[AutoPlayer] {_previousState} -> {_state} target={(target != null ? target.name : "none")}", this);

            OnEnterState(newState);
        }

        void OnEnterState(AutoPlayerState state)
        {
            adapter?.ClearActionRequests();

            switch (state)
            {
                case AutoPlayerState.Explore:
                    PickExploreDestination();
                    break;
                case AutoPlayerState.RecoverFromStuck:
                    _recoveryPhase = 0;
                    _recoveryPhaseTimer = 0f;
                    if (_currentTarget != null)
                        memory?.Blacklist(_currentTarget.gameObject);
                    movement?.ClearDestination();
                    break;
                case AutoPlayerState.BuildBridge:
                case AutoPlayerState.CollectLog:
                case AutoPlayerState.ChopTree:
                case AutoPlayerState.Combat:
                case AutoPlayerState.AvoidDanger:
                case AutoPlayerState.InteractWithNPC:
                    if (_currentTarget != null)
                        movement?.SetDestination(_currentTarget.position);
                    break;
            }
        }

        void TickActiveState()
        {
            switch (_state)
            {
                case AutoPlayerState.Idle:
                    adapter?.SetWorldMoveDirection(Vector3.zero);
                    break;
                case AutoPlayerState.Explore:
                case AutoPlayerState.MoveToTarget:
                    TickExplore();
                    break;
                case AutoPlayerState.AvoidDanger:
                    TickAvoidDanger();
                    break;
                case AutoPlayerState.Combat:
                    TickCombat();
                    break;
                case AutoPlayerState.InteractWithNPC:
                    TickInteractNpc();
                    break;
                case AutoPlayerState.ChopTree:
                    TickChopTree();
                    break;
                case AutoPlayerState.CollectLog:
                    TickCollectLog();
                    break;
                case AutoPlayerState.CarryLog:
                    TickCarryLog();
                    break;
                case AutoPlayerState.BuildBridge:
                    TickBuildBridge();
                    break;
                case AutoPlayerState.RecoverFromStuck:
                    TickRecover();
                    break;
            }

            if (perception != null && perception.HazardAhead && movement != null && movement.HasDestination)
            {
                Vector3 avoid = perception.HazardAvoidDirection;
                if (avoid.sqrMagnitude > LookRotationEpsilon)
                    movement.SetDestination(transform.position + avoid * 3f);
            }
        }

        void TickExplore()
        {
            if (movement == null)
                return;

            if (!movement.HasDestination)
                PickExploreDestination();

            movement.TickMovement(allowJump: true);
        }

        void PickExploreDestination()
        {
            if (perception == null || movement == null)
                return;

            for (int attempt = 0; attempt < 8; attempt++)
            {
                Vector2 rnd = Random.insideUnitCircle * exploreRadius;
                Vector3 candidate = transform.position + new Vector3(rnd.x, 0f, rnd.y);
                if (!perception.TryGetGroundPoint(candidate, exploreRadius * 0.25f, out Vector3 ground))
                    continue;

                memory?.RememberExplorePoint(ground);
                _exploreDestination = ground;
                movement.SetDestination(ground);
                return;
            }

            movement.SetDestination(transform.position + transform.forward * 4f);
        }

        void TickAvoidDanger()
        {
            if (_currentTarget == null || adapter == null || movement == null)
                return;

            Vector3 away = transform.position - _currentTarget.position;
            away.y = 0f;
            if (away.sqrMagnitude > LookRotationEpsilon)
            {
                movement.SetDestination(transform.position + away.normalized * 5f);
                adapter.SetWorldMoveDirection(away.normalized);
                adapter.SetSprint(true);
            }

            if (adapter.HealthRatio() < LowHealthDefendRatio)
                adapter.RequestDefend();

            movement.TickMovement(allowJump: true);
        }

        void TickCombat()
        {
            if (_currentTarget == null || adapter == null || movement == null)
                return;

            Vector3 toEnemy = _currentTarget.position - transform.position;
            toEnemy.y = 0f;
            float dist = toEnemy.magnitude;

            if (dist > CombatSafeDistance)
                movement.SetDestination(_currentTarget.position);
            else if (dist < CombatAttackDistance)
                movement.ClearDestination();
            else
                movement.SetDestination(_currentTarget.position);

            if (toEnemy.sqrMagnitude > LookRotationEpsilon)
                adapter.SetWorldMoveDirection(toEnemy.normalized);

            if (dist <= CombatAttackDistance)
                adapter.RequestPrimaryAttack();
            else
                adapter.SetSprint(true);

            if (adapter.HealthRatio() < LowHealthDefendRatio)
                adapter.RequestDefend();

            movement.TickMovement(allowJump: false);
        }

        void TickInteractNpc()
        {
            if (_currentTarget == null || adapter == null || movement == null)
                return;

            movement.SetDestination(_currentTarget.position);
            movement.TickMovement(allowJump: false);

            if (movement.DistanceToDestination > interactionDistance)
                return;

            adapter.SetWorldMoveDirection(Vector3.zero);

            IAutoInteractable interactable = _currentTarget.GetComponent<IAutoInteractable>();
            if (interactable != null)
            {
                if (interactable.CanAutoInteract(gameObject))
                    interactable.AutoInteract(gameObject);
            }
            else
            {
                adapter.RequestInteract();
            }

            _interactionTimer = interactionCooldown;
            TransitionTo(AutoPlayerState.Idle, 0);
        }

        void TickChopTree()
        {
            if (_currentTarget == null || adapter == null || movement == null)
                return;

            if (!_currentTarget.gameObject.activeInHierarchy)
            {
                TransitionTo(AutoPlayerState.Explore, 10);
                return;
            }

            movement.SetDestination(_currentTarget.position);
            movement.TickMovement(allowJump: false);

            if (movement.DistanceToDestination <= ChopDistance)
            {
                Vector3 toTree = _currentTarget.position - transform.position;
                toTree.y = 0f;
                if (toTree.sqrMagnitude > LookRotationEpsilon)
                    adapter.SetWorldMoveDirection(toTree.normalized);
                adapter.RequestChop();
            }
        }

        void TickCollectLog()
        {
            if (_currentTarget == null || adapter == null || movement == null)
                return;

            movement.SetDestination(_currentTarget.position);
            movement.TickMovement(allowJump: false);

            if (movement.DistanceToDestination <= LogPickupDistance)
            {
                adapter.RequestPickUp();

                Drawn drawn = _currentTarget.GetComponentInParent<Drawn>();
                if (drawn != null)
                    adapter.RequestDrawLog();
            }
        }

        void TickCarryLog()
        {
            if (movement == null || adapter == null)
                return;

            if (perception != null && perception.NearestBridge != null)
            {
                TransitionTo(AutoPlayerState.BuildBridge, 3, perception.NearestBridge);
                return;
            }

            TransitionTo(AutoPlayerState.Explore, 10);
        }

        void TickBuildBridge()
        {
            if (_currentTarget == null || adapter == null || movement == null)
                return;

            IAutoBuildTarget build = _currentTarget.GetComponent<IAutoBuildTarget>();
            NewConstructor constructor = build == null ? _currentTarget.GetComponent<NewConstructor>() : null;

            movement.SetDestination(_currentTarget.position);
            movement.TickMovement(allowJump: false);

            if (movement.DistanceToDestination > interactionDistance + 1f)
                return;

            adapter.SetWorldMoveDirection(Vector3.zero);

            if (build != null)
            {
                if (!build.IsComplete && !build.NeedsLogs && adapter.CarriedLogCount >= 9)
                {
                    adapter.RequestDropOrPlaceLog();
                    return;
                }

                if (!build.IsComplete && build.NeedsLogs)
                {
                    adapter.PulseRollPress();
                    adapter.RequestPickUp();
                }
            }
            else if (constructor != null)
            {
                if (!constructor.isLocked)
                    adapter.PulseRollPress();
                else if (adapter.CarriedLogCount > 0)
                    adapter.RequestPickUp();
                else if (adapter.CarriedLogCount >= 9)
                    adapter.RequestDropOrPlaceLog();
            }
            else if (adapter.CarriedLogCount >= 9)
            {
                adapter.RequestDropOrPlaceLog();
            }
        }

        void TickRecover()
        {
            if (adapter == null || movement == null)
                return;

            _recoveryPhaseTimer += Time.deltaTime;

            switch (_recoveryPhase)
            {
                case 0:
                    adapter.SetWorldMoveDirection(Vector3.zero);
                    if (_recoveryPhaseTimer >= 0.35f)
                    {
                        _recoveryPhase = 1;
                        _recoveryPhaseTimer = 0f;
                    }
                    break;
                case 1:
                    adapter.SetWorldMoveDirection(movement.GetRecoveryDirection());
                    if (_recoveryPhaseTimer >= 0.6f)
                    {
                        _recoveryPhase = 2;
                        _recoveryPhaseTimer = 0f;
                    }
                    break;
                case 2:
                    Vector3 strafe = Vector3.Cross(Vector3.up, transform.forward);
                    adapter.SetWorldMoveDirection(strafe);
                    if (_recoveryPhaseTimer >= 0.5f)
                    {
                        _recoveryPhase = 3;
                        _recoveryPhaseTimer = 0f;
                    }
                    break;
                case 3:
                    if (adapter.IsGrounded)
                        adapter.RequestJump();
                    if (_recoveryPhaseTimer >= 0.4f)
                    {
                        movement.ResetStuck();
                        TransitionTo(AutoPlayerState.Explore, 10);
                    }
                    break;
            }
        }
    }
}
