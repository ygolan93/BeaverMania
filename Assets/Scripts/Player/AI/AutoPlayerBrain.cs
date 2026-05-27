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
        ShopAtTrader,
        SwitchArsenal,
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
        [SerializeField] AutoPlayerCombatSelector combatSelector;
        [SerializeField] AutoPlayerPlaystyleApplier playstyleApplier;
        [SerializeField] AutoPlayerManualPlayRecorder playstyleRecorder;
        [SerializeField] AutoPlayerDebugHUD debugHud;
        [SerializeField] AutoPlayerTerrainSense terrainSense;
        [SerializeField] AutoPlayerNavigationPlanner navigation;
        [SerializeField] AutoPlayerIdlePlanner idlePlanner;
        [SerializeField] AutoPlayerShopAdapter shopAdapter;
        [SerializeField] AutoPlayerCameraLock cameraLock;
        [SerializeField] AutoPlayerCombatRanged combatRanged;
        [SerializeField] AutoPlayerCombatStaminaGate staminaGate;
        [SerializeField] float shopVisitCooldown = 25f;
        [SerializeField] float staminaRecoveryRetreatDistance = 6f;
        [SerializeField] float retaliateDuration = 4f;
        [SerializeField] float retaliateSearchRadius = 16f;

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
        Trader _activeTrader;
        float _shopPhaseTimer;
        int _shopPurchaseAttempts;
        float _previousHealth = -1f;
        float _retaliateUntil;
        bool _isRetaliating;

        public bool AutoPlayerEnabled => autoPlayerEnabled;
        public AutoPlayerState State => _state;
        public Transform CurrentTarget => _currentTarget;
        public int CurrentPriority => _currentPriority;
        public CombatRangeMode CombatMode => combatRanged != null ? combatRanged.ActiveMode : CombatRangeMode.None;
        public bool IsRetaliating => _isRetaliating;
        public bool CameraOrbitLocked => cameraLock != null && cameraLock.OrbitLocked;
        public bool RecoveringStamina => staminaGate != null && staminaGate.MustRecoverStamina;

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
            if (combatSelector == null)
                combatSelector = GetComponent<AutoPlayerCombatSelector>();
            if (playstyleApplier == null)
                playstyleApplier = GetComponent<AutoPlayerPlaystyleApplier>();
            if (playstyleRecorder == null)
                playstyleRecorder = GetComponent<AutoPlayerManualPlayRecorder>();
            if (debugHud == null)
                debugHud = GetComponent<AutoPlayerDebugHUD>();
            if (terrainSense == null)
                terrainSense = GetComponent<AutoPlayerTerrainSense>();
            if (navigation == null)
                navigation = GetComponent<AutoPlayerNavigationPlanner>();
            if (idlePlanner == null)
                idlePlanner = GetComponent<AutoPlayerIdlePlanner>();
            if (shopAdapter == null)
                shopAdapter = GetComponent<AutoPlayerShopAdapter>();
            if (cameraLock == null)
                cameraLock = GetComponent<AutoPlayerCameraLock>();
            if (combatRanged == null)
                combatRanged = GetComponent<AutoPlayerCombatRanged>();
            if (staminaGate == null)
                staminaGate = GetComponent<AutoPlayerCombatStaminaGate>();
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

            PollDamageReaction();

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
            cameraLock?.SetOrbitLocked(true);
        }

        void DisableAutoPlayer()
        {
            PlayerInputOverride.SetActive(false);
            PlayerInputOverride.ClearAll();
            movement?.ClearDestination();
            combatRanged?.ResetRangedState();
            staminaGate?.ResetRecoveryState();
            cameraLock?.SetOrbitLocked(false);
            _state = AutoPlayerState.Idle;
            _currentTarget = null;
            _isRetaliating = false;
            _retaliateUntil = 0f;
            _previousHealth = -1f;
        }

        void PollDamageReaction()
        {
            if (adapter == null || adapter.Player == null)
                return;

            float healthNow = adapter.CurrentHealth();
            if (_previousHealth < 0f)
            {
                _previousHealth = healthNow;
                return;
            }

            bool tookDamage = adapter.WasHurtRecently()
                || healthNow < _previousHealth - 0.01f;
            _previousHealth = healthNow;

            if (!tookDamage || healthNow <= 0f)
                return;

            _retaliateUntil = Time.time + retaliateDuration;
            _isRetaliating = true;

            perception?.Scan(memory);
            Transform target = perception != null
                ? perception.NearestThreat ?? perception.FindNearestThreatWithinRadius(retaliateSearchRadius, memory)
                : null;

            if (target == null)
                return;

            if (staminaGate != null && staminaGate.MustRecoverStamina)
                return;

            if (_state == AutoPlayerState.Combat && _currentTarget == target)
                return;

            TransitionTo(AutoPlayerState.Combat, 0, target);
        }

        bool TryHandleHurtRetaliation()
        {
            if (Time.time > _retaliateUntil)
                return false;

            if (perception == null)
                return false;

            Transform target = perception.NearestThreat
                ?? perception.FindNearestThreatWithinRadius(retaliateSearchRadius, memory);
            if (target == null)
                return false;

            if (staminaGate != null && staminaGate.MustRecoverStamina)
                return false;

            _isRetaliating = true;
            TransitionTo(AutoPlayerState.Combat, 0, target);
            return true;
        }

        void RunDecision()
        {
            ApplyPlaystyleTuning();
            terrainSense?.TickSense();
            perception?.Scan(memory);
            if (adapter != null && adapter.Carry != null)
                memory?.SetCarriedLogCount(adapter.CarriedLogCount);

            if (movement != null && movement.IsStuck && _state != AutoPlayerState.RecoverFromStuck)
            {
                TransitionTo(AutoPlayerState.RecoverFromStuck, 1);
                return;
            }

            if (TryHandleHurtRetaliation())
                return;

            if (_retaliateUntil > Time.time && perception != null)
            {
                Transform retaliateTarget = perception.NearestThreat
                    ?? perception.FindNearestThreatWithinRadius(retaliateSearchRadius, memory);
                if (retaliateTarget != null)
                {
                    _isRetaliating = true;
                    TransitionTo(AutoPlayerState.Combat, 0, retaliateTarget);
                    return;
                }
            }

            _isRetaliating = false;

            float engageRadius = perception != null ? perception.CombatEngageRadius : 10f;
            if (perception != null && perception.NearestThreat != null && perception.NearestThreatDistance <= engageRadius)
            {
                if (staminaGate != null && staminaGate.MustRecoverStamina)
                {
                    TransitionTo(AutoPlayerState.Combat, 2, perception.NearestThreat);
                    return;
                }

                if (perception.NearestThreatDistance < CombatSafeDistance * 0.75f)
                    TransitionTo(AutoPlayerState.AvoidDanger, 1, perception.NearestThreat);
                else
                    TransitionTo(AutoPlayerState.Combat, 2, perception.NearestThreat);
                return;
            }

            if (TrySchedulePrimaryBridgeObjective())
                return;

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

            IdlePlannerResult idlePlan = idlePlanner != null
                ? idlePlanner.Evaluate(bridgePipelineActive: false)
                : default;

            if (idlePlan.Action == IdleActionKind.SwitchArsenal && !string.IsNullOrEmpty(idlePlan.ArsenalEntryName))
            {
                TransitionTo(AutoPlayerState.SwitchArsenal, 7, null);
                return;
            }

            if (idlePlan.Action == IdleActionKind.VisitTrader
                && perception != null
                && perception.NearestInteractable != null
                && _interactionTimer <= 0f
                && shopAdapter != null
                && shopAdapter.PlayerNeedsShopVisit()
                && shopAdapter.CanAffordAnyUsefulPurchase())
            {
                IAutoInteractable interactable = perception.NearestInteractable.GetComponent<IAutoInteractable>();
                if (interactable == null || !interactable.IsAutoInteractionBusy())
                {
                    TransitionTo(AutoPlayerState.InteractWithNPC, 6, perception.NearestInteractable);
                    return;
                }
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

        void ApplyPlaystyleTuning()
        {
            if (playstyleApplier == null)
                return;

            playstyleApplier.RefreshTuning();
            PlaystyleRuntimeTuning tuning = playstyleApplier.Tuning;
            perception?.ApplyPlaystyleTuning(tuning);
            movement?.ApplyPlaystyleTuning(tuning);
        }

        bool TrySchedulePrimaryBridgeObjective()
        {
            AutoPlayerPrimaryBridgeObjective objective = AutoPlayerPrimaryBridgeObjective.Active;
            if (objective == null)
                objective = AutoPlayerPrimaryBridgeObjective.ResolveInScene();

            if (objective == null || !objective.IsAssigned || objective.IsComplete)
                return false;

            Transform bridgeTarget = objective.BuildTarget;
            if (bridgeTarget == null)
                return false;

            if (adapter != null && adapter.IsCarryingLogs)
            {
                TransitionTo(AutoPlayerState.BuildBridge, 1, bridgeTarget);
                return true;
            }

            if (adapter != null && adapter.CanCarryMore && perception != null && perception.NearestLog != null)
            {
                TransitionTo(AutoPlayerState.CollectLog, 1, perception.NearestLog);
                return true;
            }

            if (perception != null && perception.NearestTree != null && (adapter == null || adapter.CanCarryMore))
            {
                TransitionTo(AutoPlayerState.ChopTree, 1, perception.NearestTree);
                return true;
            }

            TransitionTo(AutoPlayerState.MoveToTarget, 1, bridgeTarget);
            return true;
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
                    combatRanged?.ResetRangedState();
                    break;
                case AutoPlayerState.Combat:
                case AutoPlayerState.AvoidDanger:
                    combatRanged?.ResetRangedState();
                    if (_currentTarget != null)
                        SetNavigationDestination(_currentTarget.position);
                    break;
                case AutoPlayerState.BuildBridge:
                case AutoPlayerState.CollectLog:
                case AutoPlayerState.ChopTree:
                case AutoPlayerState.InteractWithNPC:
                case AutoPlayerState.ShopAtTrader:
                    if (_currentTarget != null)
                        SetNavigationDestination(_currentTarget.position);
                    break;
                case AutoPlayerState.MoveToTarget:
                    if (_currentTarget != null)
                        SetNavigationDestination(_currentTarget.position);
                    break;
                case AutoPlayerState.SwitchArsenal:
                    movement?.ClearDestination();
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
                case AutoPlayerState.ShopAtTrader:
                    TickShopAtTrader();
                    break;
                case AutoPlayerState.SwitchArsenal:
                    TickSwitchArsenal();
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

            ApplyTerrainAvoidance();
        }

        void ApplyTerrainAvoidance()
        {
            if (movement == null || !movement.HasDestination)
                return;

            Vector3 avoid = Vector3.zero;
            if (terrainSense != null)
            {
                TerrainProbeSnapshot snap = terrainSense.Snapshot;
                if (snap.CliffAhead || snap.HazardAhead)
                    avoid = snap.SuggestedAvoidDirection;
            }

            if (avoid.sqrMagnitude < LookRotationEpsilon && perception != null && perception.HazardAhead)
                avoid = perception.HazardAvoidDirection;

            if (avoid.sqrMagnitude > LookRotationEpsilon)
                SetNavigationDestination(transform.position + avoid * 3f);
        }

        void SetNavigationDestination(Vector3 worldPoint)
        {
            if (movement == null)
                return;

            if (navigation != null && navigation.TryGetReachablePoint(worldPoint, out Vector3 waypoint))
                movement.SetDestination(waypoint);
            else
                movement.SetDestination(worldPoint);
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

            Vector3 biasDirection = transform.forward;
            if (terrainSense != null)
            {
                TerrainProbeSnapshot snap = terrainSense.Snapshot;
                if (snap.HasGroundAhead)
                    biasDirection = transform.forward;
                else if (snap.SuggestedAvoidDirection.sqrMagnitude > LookRotationEpsilon)
                    biasDirection = snap.SuggestedAvoidDirection;
            }

            for (int attempt = 0; attempt < 10; attempt++)
            {
                Vector2 rnd = Random.insideUnitCircle * exploreRadius;
                Vector3 offset = new Vector3(rnd.x, 0f, rnd.y);
                if (attempt < 4)
                    offset += biasDirection * (exploreRadius * 0.35f);

                Vector3 candidate = transform.position + offset;
                if (!perception.TryGetGroundPoint(candidate, exploreRadius * 0.25f, out Vector3 ground))
                    continue;

                Vector3 destination = ground;
                if (navigation != null)
                {
                    if (!navigation.TryValidateExplorePoint(ground, memory, out Vector3 validated))
                    {
                        memory?.BlacklistWaypoint(ground, 8f);
                        continue;
                    }

                    destination = validated;
                }
                memory?.RememberExplorePoint(destination);
                _exploreDestination = destination;
                SetNavigationDestination(destination);
                return;
            }

            SetNavigationDestination(transform.position + biasDirection * 4f);
        }

        void TickAvoidDanger()
        {
            if (_currentTarget == null || adapter == null || movement == null)
                return;

            if (TryTickStaminaRecovery(_currentTarget))
                return;

            cameraLock?.AimAtTarget(_currentTarget);

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

        bool TryTickStaminaRecovery(Transform threat)
        {
            if (staminaGate == null || adapter == null || movement == null || threat == null)
                return false;

            staminaGate.UpdateRecoveryState();
            if (!staminaGate.MustRecoverStamina)
                return false;

            adapter.ClearCombatInputs();
            combatRanged?.ResetRangedState();

            Vector3 away = transform.position - threat.position;
            away.y = 0f;
            if (away.sqrMagnitude > LookRotationEpsilon)
            {
                Vector3 retreatPoint = transform.position + away.normalized * staminaRecoveryRetreatDistance;
                SetNavigationDestination(retreatPoint);
                adapter.SetWorldMoveDirection(away.normalized);
            }
            else
            {
                movement.ClearDestination();
                adapter.SetWorldMoveDirection(Vector3.zero);
            }

            adapter.SetSprint(false);
            movement.TickMovement(allowJump: false);
            return true;
        }

        void TickCombat()
        {
            if (_currentTarget == null || adapter == null || movement == null)
                return;

            if (!_currentTarget.gameObject.activeInHierarchy)
            {
                combatRanged?.ResetRangedState();
                TransitionTo(AutoPlayerState.Explore, 10);
                return;
            }

            if (TryTickStaminaRecovery(_currentTarget))
                return;

            Vector3 toEnemy = _currentTarget.position - transform.position;
            toEnemy.y = 0f;
            float dist = toEnemy.magnitude;

            cameraLock?.AimAtTarget(_currentTarget);

            CombatRangeMode mode = combatRanged != null
                ? combatRanged.EvaluateMode(_currentTarget, dist)
                : CombatRangeMode.Melee;

            if (mode == CombatRangeMode.Stone
                && (staminaGate == null || staminaGate.CanPerformCombatAttacks()))
            {
                combatRanged.TickRangedCombat(_currentTarget, dist, movement);
                movement.TickMovement(allowJump: false);

                if (adapter.HealthRatio() < LowHealthDefendRatio)
                    adapter.RequestDefend();

                return;
            }

            combatRanged?.ResetRangedState();

            string threatTag = perception != null ? perception.NearestThreatTag : _currentTarget.tag;
            AutoCombatPlan plan = combatSelector != null
                ? combatSelector.BuildPlan(_currentTarget, dist, threatTag)
                : default;

            bool useBow = plan.Action == AutoCombatActionKind.Bow;
            float standOffDistance = useBow ? 7f : CombatAttackDistance;

            if (dist > CombatSafeDistance)
                SetNavigationDestination(_currentTarget.position);
            else if (dist < standOffDistance)
                movement.ClearDestination();
            else
                SetNavigationDestination(_currentTarget.position);

            if (toEnemy.sqrMagnitude > LookRotationEpsilon)
            {
                adapter.FaceWorldPosition(_currentTarget.position);
                adapter.SetWorldMoveDirection(toEnemy.normalized);
            }

            if (staminaGate == null || staminaGate.CanPerformCombatAttacks())
            {
                if (combatSelector != null)
                    combatSelector.ApplyPlan(adapter, plan);
                else if (dist <= CombatAttackDistance)
                    adapter.RequestPrimaryAttack();
                else
                    adapter.SetSprint(true);
            }
            else
            {
                adapter.ClearCombatInputs();
            }

            if (adapter.HealthRatio() < LowHealthDefendRatio && (staminaGate == null || staminaGate.CanPerformCombatAttacks()))
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

            if (shopAdapter != null && shopAdapter.PlayerNeedsShopVisit() && shopAdapter.CanAffordAnyUsefulPurchase())
            {
                _activeTrader = idlePlanner != null
                    ? idlePlanner.GetTraderFromTarget(_currentTarget)
                    : _currentTarget.GetComponentInParent<Trader>();
                _shopPhaseTimer = 0f;
                _shopPurchaseAttempts = 0;
                TransitionTo(AutoPlayerState.ShopAtTrader, 6, _currentTarget);
                return;
            }

            _interactionTimer = interactionCooldown;
            TransitionTo(AutoPlayerState.Idle, 0);
        }

        void TickShopAtTrader()
        {
            if (_currentTarget == null || adapter == null || movement == null)
            {
                FinishShopVisit();
                return;
            }

            _shopPhaseTimer += Time.deltaTime;
            movement.SetDestination(_currentTarget.position);
            movement.TickMovement(allowJump: false);

            if (_activeTrader == null)
                _activeTrader = idlePlanner != null
                    ? idlePlanner.GetTraderFromTarget(_currentTarget)
                    : _currentTarget.GetComponentInParent<Trader>();

            if (_activeTrader == null)
            {
                FinishShopVisit();
                return;
            }

            if (movement.DistanceToDestination > interactionDistance + 0.5f && _shopPhaseTimer < 0.75f)
                return;

            adapter.SetWorldMoveDirection(Vector3.zero);

            if (!_activeTrader.IsTraderSessionActive())
            {
                IAutoInteractable interactable = _currentTarget.GetComponent<IAutoInteractable>();
                if (interactable != null && interactable.CanAutoInteract(gameObject))
                    interactable.AutoInteract(gameObject);
                else
                    adapter.RequestInteract();
            }

            if (!_activeTrader.IsShopUiOpen() && _shopPhaseTimer > 0.35f)
                _activeTrader.BeginAutoShopSession();

            if (shopAdapter != null)
            {
                shopAdapter.BindShop(shopAdapter.ResolveShopFromTrader(_currentTarget));
                if (_activeTrader.IsShopUiOpen())
                {
                    if (shopAdapter.TryAutoPurchaseBestNeed())
                        _shopPurchaseAttempts++;
                }
            }

            if (_shopPhaseTimer > 2.5f || _shopPurchaseAttempts >= 2 || (shopAdapter != null && !shopAdapter.PlayerNeedsShopVisit()))
                FinishShopVisit();
        }

        void FinishShopVisit()
        {
            _activeTrader?.EndAutoTraderSession();
            memory?.ScheduleNextShopVisit(shopVisitCooldown);
            _interactionTimer = interactionCooldown;
            _activeTrader = null;
            TransitionTo(AutoPlayerState.Explore, 10);
        }

        void TickSwitchArsenal()
        {
            if (adapter == null || idlePlanner == null)
            {
                TransitionTo(AutoPlayerState.Explore, 10);
                return;
            }

            IdlePlannerResult plan = idlePlanner.LastResult;
            if (!string.IsNullOrEmpty(plan.ArsenalEntryName))
                adapter.TryEquipArsenal(plan.ArsenalEntryName);

            TransitionTo(AutoPlayerState.Explore, 10);
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

            adapter?.EquipForChop();
            SetNavigationDestination(_currentTarget.position);
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

            SetNavigationDestination(_currentTarget.position);
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

            SetNavigationDestination(_currentTarget.position);
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
