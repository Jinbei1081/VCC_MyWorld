using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;
using Unity.Profiling;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3;
using VRC.SDK3.Components;
using VRC.SDK3.UdonNetworkCalling;
using VRC.Udon.ClientBindings.Interfaces;
using VRC.Udon.Common;
using VRC.Udon.Common.Attributes;
using VRC.Udon.Common.Enums;
using VRC.Udon.Common.Interfaces;
using VRC.Udon.Security;
using VRC.Udon.Serialization.OdinSerializer;
using VRC.Udon.VM;
using Logger = VRC.Core.Logger;
using Object = UnityEngine.Object;
using SyncType = VRC.SDKBase.Networking.SyncType;

#if VRC_CLIENT
using VRC.Core;
#endif

#if UNITY_EDITOR && !VRC_CLIENT
using UnityEditor.SceneManagement;
#endif

namespace VRC.Udon
{
    public sealed class UdonBehaviour : AbstractUdonBehaviour, ISerializationCallbackReceiver
#if VRC_CLIENT
        , INetworkReadyReceiver
#endif
    {
        #region Odin Serialized Fields

        [PublicAPI]
        public IUdonVariableTable publicVariables = new UdonVariableTable();

        #endregion

        #region Serialized Public Fields

        [Obsolete("Use VRCObjectSync instead")]
        [PublicAPI]
        // ReSharper disable once InconsistentNaming
        public bool SynchronizePosition;

        // ReSharper disable once InconsistentNaming
        [PublicAPI]
        public readonly bool SynchronizeAnimation = false; //We don't support animation sync yet, coming soon.

        // ReSharper disable once InconsistentNaming

        [Obsolete("Use VRCObjectSync instead")]
        [PublicAPI]
        public bool AllowCollisionOwnershipTransfer = true;

        // ReSharper disable once InconsistentNaming
        [HideInInspector, Obsolete("Use SyncMethod instead")]
        public bool Reliable = false;

        // ReSharper disable once InconsistentNaming
        [SerializeField]
        private SyncType _syncMethod = SyncType.Unknown;

        public override SyncType SyncMethod
        {
            get 
            {
                // Old Scene?
                if(_syncMethod == SyncType.Unknown)
                {
#pragma warning disable 618
                    _syncMethod = Reliable ? SyncType.Manual : SyncType.Continuous;
#pragma warning restore 618
                }

                return _syncMethod;
            }
            set
            {
                _syncMethod = value;
                if(value == SyncType.None)
                {
                    return;
                }

                // All synced UdonBehaviours on one GameObject must use the same sync method.
#if VRC_CLIENT
                using(gameObject.GetComponentsPooled(out List<UdonBehaviour> behaviours))
#else
                UdonBehaviour[] behaviours = gameObject.GetComponents<UdonBehaviour>();
#endif
                {
                    foreach(UdonBehaviour ub in behaviours)
                    {
                        if(ub != null && ub._syncMethod != SyncType.None)
                        {
                            ub._syncMethod = value;
                        }
                    }
                }
            }
        }

        public bool HasDoneStart => _hasDoneStart;

        public bool HasError => _hasError;

        public bool SyncIsContinuous => SyncMethod == SyncType.Continuous;
        public bool SyncIsManual => SyncMethod == SyncType.Manual;

        #endregion

        #region Serialized Private Fields

        [SerializeField]
        private AbstractSerializedUdonProgramAsset serializedProgramAsset;

#if UNITY_EDITOR && !VRC_CLIENT
        [SerializeField]
        public AbstractUdonProgramSource programSource;
#endif

        #endregion

        #region Public Fields and Properties

        [PublicAPI]
        public static Action<UdonBehaviour, IUdonProgram> OnInit { get; set; } = null;
                
        [PublicAPI]
        public static Action<UdonBehaviour> RequestSerializationHook { get; set; } = null;
        
        [PublicAPI]
        public override bool DisableInteractive { get; set; }

        [PublicAPI]
        [ExcludeFromUdonWrapper]
        public override bool IsNetworkingSupported
        {
            get => _isNetworkingSupported;
            set
            {
                if (_initialized)
                {
                    throw new InvalidOperationException(
                        "IsNetworkingSupported cannot be changed after the UdonBehaviour has been initialized.");
                }

                _isNetworkingSupported = value;
            }
        }

        public override bool IsInteractive => _hasInteractiveEvents && !DisableInteractive;
        
        // ReSharper disable once InconsistentNaming
        public const string ReturnVariableName = "__returnValue";

        internal int UpdateOrder => _program?.UpdateOrder ?? 0;

        [PublicAPI]
        public override bool DisableEventProcessing { get; set; } = false;

        public int ProgramId => serializedProgramAsset != null ? serializedProgramAsset.GetInstanceID() : 0;

        public ulong ProgramSize => serializedProgramAsset != null ? serializedProgramAsset.GetSerializedProgramSize() : 0L;

        public override NetworkCallingEntrypointMetadata[] GetNetworkCallingMetadata() =>
            serializedProgramAsset != null ? serializedProgramAsset.GetNetworkCallingMetadata() : null;
        public override NetworkCallingEntrypointMetadata GetNetworkCallingMetadata(string entrypoint) =>
            serializedProgramAsset != null ? serializedProgramAsset.GetNetworkCallingMetadata(entrypoint) : null;

        public override bool TryGetEntrypointNameFromHash(uint hash, out string entrypoint)
        {
            if (serializedProgramAsset == null)
            {
                entrypoint = null;
                return false;
            }
            return serializedProgramAsset.TryGetEntrypointNameFromHash(hash, out entrypoint);
        }
        public override bool TryGetEntrypointHashFromName(string entrypoint, out uint hash)
        {
            if (serializedProgramAsset == null)
            {
                hash = 0;
                return false;
            }
            return serializedProgramAsset.TryGetEntrypointHashFromName(entrypoint, out hash);
        }

        public bool IsInitialized => _initialized;

        // a component index that doesn't change at runtime so it can be used as an identifier
        [NonSerialized] private int _componentIndexFixed = -1;
        public override int GetComponentIndexFixed()
        {
            if (_componentIndexFixed == -1)
                _componentIndexFixed = GetComponentIndex();
            return _componentIndexFixed;
        }

        #endregion

        #region Private Fields and Properties

        private UdonManager _udonManager;
        private IUdonProgram _program;
        private IUdonVM _udonVM;
        private bool _isReady;
        private string _categoryName;
        private bool _hasError;
        private bool _hasDoneStart;
        private bool _initialized;
        private bool _isNetworkingSupported = false;
        
        private bool _hasInteractiveEvents;
        private bool _hasUpdateEvent;
        private bool _hasLateUpdateEvent;
        private bool _hasFixedUpdateEvent;
        private bool _hasPostLateUpdateEvent;
        private readonly Dictionary<string, List<uint>> _eventTable = new Dictionary<string, List<uint>>();

        private readonly Dictionary<(string eventName, string symbolName), string> _symbolNameCache =
            new Dictionary<(string, string), string>();

        private static ProfilerMarker _managedUpdateProfilerMarker =
            new ProfilerMarker("UdonBehaviour.ManagedUpdate()");

        private static ProfilerMarker _managedLateUpdateProfilerMarker =
            new ProfilerMarker("UdonBehaviour.ManagedLateUpdate()");

        private static ProfilerMarker _managedFixedUpdateProfilerMarker =
            new ProfilerMarker("UdonBehaviour.ManagedFixedUpdate()");
			
        private static ProfilerMarker _postLateUpdateProfilerMarker =
            new ProfilerMarker("UdonBehaviour.PostLateUpdate()");

        private readonly SortedDictionary<uint, (uint, uint)> _variableToChangeEvent = new SortedDictionary<uint, (uint, uint)>();

        private readonly List<AbstractUdonBehaviourEventProxy> _eventProxies = new List<AbstractUdonBehaviourEventProxy>();
        
        #endregion

        #region Editor Only

#if UNITY_EDITOR && !VRC_CLIENT
        public void RunEditorUpdate(ref bool dirty)
        {
            if (programSource == null)
            {
                return;
            }

            programSource.RunEditorUpdate(this, ref dirty);

            if (!dirty)
            {
                return;
            }

            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }

#endif

        #endregion

        #region Private Methods

        private bool LoadProgram(IUdonSignatureVerifier signatureVerifier)
        {
            if (serializedProgramAsset == null)
            {
                return false;
            }

            if(_program == null)
            {
                bool verificationResult = signatureVerifier.VerifySignature(serializedProgramAsset as IUdonSignatureHolder);
                if (!verificationResult)
                {
                    Logger.LogError($"Program signature verification failed for {name}", _categoryName, this);
                    return false;
                }

                _program = serializedProgramAsset.RetrieveProgram();
            }

            IUdonSymbolTable symbolTable = _program?.SymbolTable;
            IUdonHeap heap = _program?.Heap;
            if (symbolTable == null || heap == null)
            {
                return false;
            }

            foreach (string variableSymbol in publicVariables.VariableSymbols)
            {
                if (!symbolTable.HasAddressForSymbol(variableSymbol))
                {
                    continue;
                }

                uint symbolAddress = symbolTable.GetAddressFromSymbol(variableSymbol);

                if (!publicVariables.TryGetVariableType(variableSymbol, out Type declaredType))
                {
                    continue;
                }

                publicVariables.TryGetVariableValue(variableSymbol, out object value);
                if (declaredType == typeof(GameObject) || declaredType == typeof(UdonBehaviour) ||
                    declaredType == typeof(Transform))
                {
                    if (value == null)
                    {
                        value = new UdonGameObjectComponentHeapReference(declaredType);
                        declaredType = typeof(UdonGameObjectComponentHeapReference);
                    }
                }

                heap.SetHeapVariable(symbolAddress, value, declaredType);
            }

            return true;
        }
        
        private void RegisterEventProxy<T>() where T : AbstractUdonBehaviourEventProxy
        {
            // Exit early if we already have a match.
            foreach (AbstractUdonBehaviourEventProxy existingProxy in _eventProxies)
            {
                if (!(existingProxy is T existingProxyAsT))
                {
                    continue;
                }

                if (existingProxyAsT.EventReceiver.Equals(this))
                {
                    return;
                }
            }

            AbstractUdonBehaviourEventProxy proxy = gameObject.AddComponent<T>();
#if UNITY_EDITOR
            proxy.hideFlags = HideFlags.HideInInspector | 
                              HideFlags.DontSaveInEditor | 
                              HideFlags.DontSaveInBuild;
#endif
            _udonManager.Blacklist(proxy, false);
            proxy.EventReceiver = this;
            proxy.enabled = enabled;

            _eventProxies.Add(proxy);
        }

        private void ProcessEntryPoints()
        {
            if (_program.EntryPoints.HasExportedSymbol("_interact"))
            {
                _hasInteractiveEvents = true;
            }

            if (_program.EntryPoints.HasExportedSymbol("_update"))
            {
                _hasUpdateEvent = true;
            }

            if (_program.EntryPoints.HasExportedSymbol("_lateUpdate"))
            {
                _hasLateUpdateEvent = true;
            }

            if (_program.EntryPoints.HasExportedSymbol("_fixedUpdate"))
            {
                _hasFixedUpdateEvent = true;
            }
            
            if (_program.EntryPoints.HasExportedSymbol("_postLateUpdate"))
            {
                _hasPostLateUpdateEvent = true;
            }

            DetectExistingProxies();
            if (_program.EntryPoints.HasExportedSymbol("_onRenderObject"))
            {
                RegisterEventProxy<OnRenderObjectProxy>();
            }

            if (_program.EntryPoints.HasExportedSymbol("_onWillRenderObject"))
            {
                RegisterEventProxy<OnWillRenderObjectProxy>();
            }

            if (_program.EntryPoints.HasExportedSymbol("_onTriggerStay") ||
                _program.EntryPoints.HasExportedSymbol("_onPlayerTriggerStay") ||
                _program.EntryPoints.HasExportedSymbol("_onDroneTriggerStay"))
            {
                RegisterEventProxy<OnTriggerStayProxy>();
            }

            if (_program.EntryPoints.HasExportedSymbol("_onCollisionStay") ||
                _program.EntryPoints.HasExportedSymbol("_onPlayerCollisionStay"))
            {
                RegisterEventProxy<OnCollisionStayProxy>();
            }
            
            if (_program.EntryPoints.HasExportedSymbol("_onAnimatorMove"))
            {
                RegisterEventProxy<OnAnimatorMoveProxy>();
            }

            if(_program.EntryPoints.HasExportedSymbol("_onAudioFilterRead"))
            {
                RegisterEventProxy<OnAudioFilterReadProxy>();
            }

            RegisterUpdate();

            _eventTable.Clear();
            foreach (string entryPoint in _program.EntryPoints.GetExportedSymbols())
            {
                uint address = _program.EntryPoints.GetAddressFromSymbol(entryPoint);

                if (!_eventTable.ContainsKey(entryPoint))
                {
                    _eventTable.Add(entryPoint, new List<uint>());
                }

                _eventTable[entryPoint].Add(address);

                _udonManager.RegisterInput(this, entryPoint, true);
                
                // check whether this is a variableChangedEvent
                if (entryPoint.StartsWith(VariableChangedEvent.EVENT_PREFIX))
                {
                    string variableName = entryPoint.Remove(0, VariableChangedEvent.EVENT_PREFIX.Length);
                    // ensure the variable with the matching name exists
                    if (_program.SymbolTable.TryGetAddressFromSymbol(variableName, out uint variableAddress))
                    {
                        // the old variable is only added if it's used, so just store default if it's not
                        _program.SymbolTable.TryGetAddressFromSymbol(string.Concat(VariableChangedEvent.OLD_VALUE_PREFIX, variableName), out uint oldVariableAddress);
                        
                        // add variable > event address lookup
                        _variableToChangeEvent.Add(variableAddress, (address, oldVariableAddress));
                    }
                }
            }
        }
        
        // GameObjects may sometimes already have proxies for this UdonBehaviour.
        // For example if the GameObject was cloned from a scene GameObject,
        // or the component was for some reason added in the editor (this is not an expected workflow).
        // In either case Unity will serialize the reference from the proxy to this UdonBehaviour,
        // but will not serialize the contents of the _eventProxies List so we need to build it.
        // If we don't do this then we may create a proxy where one already exists and events will run twice.
        private void DetectExistingProxies()
        {
            GetComponents(_eventProxies);
            for(int i = _eventProxies.Count - 1; i >= 0; i--)
            {
                AbstractUdonBehaviourEventProxy proxy = _eventProxies[i];
                if(proxy == null)
                {
                    _eventProxies.RemoveAt(i);
                    continue;
                }

                UdonBehaviour proxyEventReceiver = proxy.EventReceiver;

                // Destroy and remove all copied proxy components which don't have an EventReceiver.
                if(proxyEventReceiver == null)
                {
                    Destroy(proxy);
                    _eventProxies.RemoveAt(i);
                    continue;
                }

                // Remove all copied proxy components which aren't for this UdonBehaviour.
                if(proxyEventReceiver == this)
                {
                    continue;
                }

                _eventProxies.RemoveAt(i);
            }
        }

        private bool ResolveUdonHeapReferences(IUdonSymbolTable symbolTable, IUdonHeap heap)
        {
            bool success = true;
            foreach (string symbolName in symbolTable.GetSymbols())
            {
                try
                {
                    uint symbolAddress = symbolTable.GetAddressFromSymbol(symbolName);
                    object heapValue = heap.GetHeapVariable(symbolAddress);
                    if (!(heapValue is UdonBaseHeapReference udonBaseHeapReference))
                    {
                        continue;
                    }

                    if (!ResolveUdonHeapReference(heap, symbolAddress, udonBaseHeapReference))
                    {
                        success = false;
                    }
                }
                catch (Exception e)
                {
#if UNITY_EDITOR
                    Logger.LogError($"{e.Message}\n{e.StackTrace}");
#else
                    Logger.LogError(e.Message);
#endif
                    success = false;
                }
            }

            return success;
        }

        private bool ResolveUdonHeapReference(IUdonHeap heap, uint symbolAddress,
            UdonBaseHeapReference udonBaseHeapReference)
        {
            switch (udonBaseHeapReference)
            {
                case UdonGameObjectComponentHeapReference udonGameObjectComponentHeapReference:
                {
                    Type referenceType = udonGameObjectComponentHeapReference.type;
                    if (referenceType == typeof(GameObject))
                    {
                        heap.SetHeapVariable(symbolAddress, gameObject);
                        return true;
                    }
                    else if (referenceType == typeof(Transform))
                    {
                        heap.SetHeapVariable(symbolAddress, gameObject.transform);
                        return true;
                    }
                    else if (referenceType == typeof(UdonBehaviour) || referenceType == typeof(IUdonBehaviour))
                    {
                        heap.SetHeapVariable(symbolAddress, this);
                        return true;
                    }
                    else if (referenceType == typeof(Object))
                    {
                        heap.SetHeapVariable(symbolAddress, this);
                        return true;
                    }
                    else
                    {
                        Logger.Log(
                            $"Unsupported GameObject/Component reference type: {udonBaseHeapReference.GetType().Name}. Only GameObject, Transform, and UdonBehaviour are supported.",
                            _categoryName,
                            this);

                        return false;
                    }
                }
                default:
                {
                    Logger.Log(
                        $"Unknown heap reference type: {udonBaseHeapReference.GetType().Name}",
                        _categoryName,
                        this);

                    return false;
                }
            }
        }

        #endregion

        #region Managed Unity Events

        internal void ManagedUpdate()
        {
            using (_managedUpdateProfilerMarker.Auto())
            {
                if (!_hasDoneStart && _isReady)
                {
                    _hasDoneStart = true;
                    RunEvent("_onEnable");
                    RunEvent("_start");
                    if (!_hasUpdateEvent)
                    {
                        _udonManager.UnregisterUdonBehaviourUpdate(this);
                    }
                }

                RunEvent("_update");
            }
        }

        internal void ManagedLateUpdate()
        {
            using (_managedLateUpdateProfilerMarker.Auto())
            {
                RunEvent("_lateUpdate");
            }
        }

        internal void ManagedFixedUpdate()
        {
            using (_managedFixedUpdateProfilerMarker.Auto())
            {
                RunEvent("_fixedUpdate");
            }
        }

        internal void PostLateUpdate()
        {
            using (_postLateUpdateProfilerMarker.Auto())
            {
                RunEvent("_postLateUpdate");
            }
        }

        #endregion

        #region Unity Events

        public void OnAnimatorIK(int layerIndex)
        {
            RunEvent("_onAnimatorIK", ("layerIndex", layerIndex));
        }

        internal void ProxyOnAnimatorMove()
        {
            RunEvent("_onAnimatorMove");
        }

        internal void ProxyOnAudioFilterRead(float[] data, int channels)
        {
            RunEvent("_onAudioFilterRead", ("data", data), ("channels", channels));
        }

        public void OnBecameInvisible()
        {
            RunEvent("_onBecameInvisible");
        }

        public void OnBecameVisible()
        {
            RunEvent("_onBecameVisible");
        }

        public void OnCollisionEnter(Collision other)
        {
            var player = VRCPlayerApi.GetPlayerByGameObject(other.gameObject);
            if (player != null)
            {
                RunEvent("_onPlayerCollisionEnter", ("player", player));
            }
             else if (!UdonManager.Instance.IsBlacklisted(other)) 
            {
                RunEvent("_onCollisionEnter", ("other", other));
            }
        }

        public void OnCollisionEnter2D(Collision2D other)
        {
            if (!UdonManager.Instance.IsBlacklisted(other))
            {
                RunEvent("_onCollisionEnter2D", ("other", other));
            }
        }

        public void OnCollisionExit(Collision other)
        {
            var player = VRCPlayerApi.GetPlayerByGameObject(other.gameObject);
            if (player != null)
            {
                RunEvent("_onPlayerCollisionExit", ("player", player));
            }
            else if (!UdonManager.Instance.IsBlacklisted(other))
            {
                RunEvent("_onCollisionExit", ("other", other));
            }
        }

        public void OnCollisionExit2D(Collision2D other)
        {
            if (!UdonManager.Instance.IsBlacklisted(other))
            {
                RunEvent("_onCollisionExit2D", ("other", other));
            }
        }

        internal void ProxyOnCollisionStay(Collision other)
        {
            var player = VRCPlayerApi.GetPlayerByGameObject(other.gameObject);
            if (player != null)
            {
                RunEvent("_onPlayerCollisionStay", ("player", player));
            }
            else if (!UdonManager.Instance.IsBlacklisted(other))
            {
                RunEvent("_onCollisionStay", ("other", other));
            }
        }

        public void OnCollisionStay2D(Collision2D other)
        {
            if (!UdonManager.Instance.IsBlacklisted(other))
            {
                RunEvent("_onCollisionStay2D", ("other", other));
            }
        }

        public void OnDestroy()
        {
            serializedPublicVariablesBytesString = null;
            publicVariablesUnityEngineObjects = null;
            
            foreach (AbstractUdonBehaviourEventProxy proxy in _eventProxies)
            {
                if (proxy)
                {
                    Destroy(proxy);
                }
            }

            _eventProxies.Clear();

            if(_program == null)
            {
                return;
            }
            
            foreach (string entryPoint in _program.EntryPoints.GetExportedSymbols())
            {
                _udonManager.RegisterInput(this, entryPoint, false);
            }

            RunEvent("_onDestroy");

            _udonVM = null;
            _program = null;
        }

        public void OnDisable()
        {
            UnregisterUpdate();

            RunEvent("_onDisable");
        }

        public void OnDrawGizmos()
        {
            RunEvent("_onDrawGizmos");
        }

        public void OnDrawGizmosSelected()
        {
            RunEvent("_onDrawGizmosSelected");
        }

        public void OnEnable()
        {
            if (_initialized)
            {
                RegisterUpdate();
            }

            RunEvent("_onEnable");
        }

        public void OnJointBreak(float breakForce)
        {
            RunEvent("_onJointBreak", ("force", breakForce));
        }

        public void OnJointBreak2D(Joint2D brokenJoint)
        {
            RunEvent("_onJointBreak2D", ("joint", brokenJoint));
        }

        public void OnMouseDown()
        {
            RunEvent("_onMouseDown");
        }

        public void OnMouseDrag()
        {
            RunEvent("_onMouseDrag");
        }

        public void OnMouseEnter()
        {
            RunEvent("_onMouseEnter");
        }

        public void OnMouseExit()
        {
            RunEvent("_onMouseExit");
        }

        public void OnMouseOver()
        {
            RunEvent("_onMouseOver");
        }

        public void OnMouseUp()
        {
            RunEvent("_onMouseUp");
        }

        public void OnMouseUpAsButton()
        {
            RunEvent("_onMouseUpAsButton");
        }

        public void OnParticleCollision(GameObject other)
        {
            var player = VRCPlayerApi.GetPlayerByGameObject(other.gameObject);
            if (player != null)
            {
                RunEvent("_onPlayerParticleCollision", ("player", player));
            }
            else
            {
            RunEvent("_onParticleCollision", ("other", other));
        }
        }

        public void OnParticleTrigger()
        {
            RunEvent("_onParticleTrigger");
        }

        public void OnPostRender()
        {
            RunEvent("_onPostRender");
        }

        public void OnPreCull()
        {
            RunEvent("_onPreCull");
        }

        public void OnPreRender()
        {
            RunEvent("_onPreRender");
        }

        public void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (!_eventTable.ContainsKey("_onRenderImage") || _eventTable["_onRenderImage"].Count == 0)
            {
                Graphics.Blit(src, dest);
                return;
            }

            RunEvent("_onRenderImage", ("src", src), ("dest", dest));
        }

        internal void ProxyOnRenderObject()
        {
            RunEvent("_onRenderObject");
        }

        public void OnTransformChildrenChanged()
        {
            RunEvent("_onTransformChildrenChanged");
        }

        public void OnTransformParentChanged()
        {
            RunEvent("_onTransformParentChanged");
        }

        public void OnTriggerEnter(Collider other)
        {
            var player = VRCPlayerApi.GetPlayerByGameObject(other.gameObject);
            if (player != null)
            {
                RunEvent("_onPlayerTriggerEnter", ("player", player));
                return;
            }

            if(VRCDroneApi.TryGetDroneFromGameObject(other.gameObject, out var drone))
            {
                RunEvent(
                    "_onDroneTriggerEnter", 
                    ("drone", drone)
                );
                return;
            }
            
            if (!UdonManager.Instance.IsBlacklisted(other))
            {
                RunEvent("_onTriggerEnter", ("other", other));
            }
        }

        public void OnTriggerEnter2D(Collider2D other)
        {
            if (!UdonManager.Instance.IsBlacklisted(other))
            {
                RunEvent("_onTriggerEnter2D", ("other", other));
            }
        }

        public void OnTriggerExit(Collider other)
        {
            var player = VRCPlayerApi.GetPlayerByGameObject(other.gameObject);
            if (player != null)
            {
                RunEvent("_onPlayerTriggerExit", ("player", player));
                return;
            }
            
            if(VRCDroneApi.TryGetDroneFromGameObject(other.gameObject, out var drone))
            {
                RunEvent(
                    "_onDroneTriggerExit", 
                    ("drone", drone)
                );
                return;
            }
            
            if (!UdonManager.Instance.IsBlacklisted(other))
            {
                RunEvent("_onTriggerExit", ("other", other));
            }
        }

        public void OnTriggerExit2D(Collider2D other)
        {
            if (!UdonManager.Instance.IsBlacklisted(other))
            {
                RunEvent("_onTriggerExit2D", ("other", other));
            }
        }

        internal void ProxyOnTriggerStay(Collider other)
        {
            var player = VRCPlayerApi.GetPlayerByGameObject(other.gameObject);
            if (player != null)
            {
                RunEvent("_onPlayerTriggerStay", ("player", player));
                return;
            }
            
            if(VRCDroneApi.TryGetDroneFromGameObject(other.gameObject, out var drone))
            {
                RunEvent(
                    "_onDroneTriggerStay", 
                    ("drone", drone)
                );
                return;
            }
            
            if (!UdonManager.Instance.IsBlacklisted(other))
            {
                RunEvent("_onTriggerStay", ("other", other));
            }
        }

        public void OnTriggerStay2D(Collider2D other)
        {
            if (!UdonManager.Instance.IsBlacklisted(other))
            {
                RunEvent("_onTriggerStay2D", ("other", other));
            }
        }

        public void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if(hit.gameObject == null) return;

            var tempPlayer = VRCPlayerApi.GetPlayerByGameObject(hit.gameObject);
            if(Utilities.IsValid(tempPlayer))
            {
                ControllerColliderPlayerHit playerHit = new ControllerColliderPlayerHit()
                {
                    player = tempPlayer,
                    moveDirection = hit.moveDirection,
                    moveLength = hit.moveLength,
                    normal = hit.normal,
                    point = hit.point,
                };

                RunEvent("_onControllerColliderHitPlayer", ("hit", playerHit));
            }
            else if(!UdonManager.Instance.IsBlacklisted(hit.gameObject))
            {
                RunEvent("_onControllerColliderHit", ("hit", hit));
            }
        }

        public void OnValidate()
        {
            RunEvent("_onValidate");
        }

        internal void ProxyOnWillRenderObject()
        {
            RunEvent("_onWillRenderObject");
        }

        #endregion

        #region VRCSDK Events

#if VRC_CLIENT
        [PublicAPI]
        public void OnNetworkReady()
        {
            _isReady = true;
        }
#endif

        //Called through Interactable interface
        public override void Interact()
        {
            RunEvent("_interact");
        }

        public override void OnDrop()
        {
            RunEvent("_onDrop");
        }

        public override void OnPickup()
        {
            RunEvent("_onPickup");
        }

        public override void OnPickupUseDown()
        {
            RunEvent("_onPickupUseDown");
        }

        public override void OnPickupUseUp()
        {
            RunEvent("_onPickupUseUp");
        }

        //Called via delegate by UdonSync
        [PublicAPI]
        public void OnPreSerialization()
        {
            if(_syncMethod == SyncType.None)
            {
                return;
            }

            if (!RunEvent(UdonManager.UDON_EVENT_ONPRESERIALIZATION)
                && !_hasError
                && _eventTable.ContainsKey(UdonManager.UDON_EVENT_ONPRESERIALIZATION))
                Logger.LogErrorFormat("OnPreSerialization event failed for {0}", gameObject.name);
        }

        //Called via delegate by UdonSync
        [PublicAPI]
        public void OnPostSerialization(SerializationResult result)
        {
            if(_syncMethod == SyncType.None)
            {
                return;
            }

            if (!RunEvent(UdonManager.UDON_EVENT_ONPOSTSERIALIZATION, ("result", result))
                && !_hasError
                && _eventTable.ContainsKey(UdonManager.UDON_EVENT_ONPOSTSERIALIZATION))
                Logger.LogErrorFormat("OnPostSerialization event failed for {0}", gameObject.name);
        }

        //Called via delegate by UdonSync
        [PublicAPI]
        public void OnDeserialization(DeserializationResult result)
        {
            if(_syncMethod == SyncType.None)
            {
                return;
            }

            if (!RunEvent(UdonManager.UDON_EVENT_ONDESERIALIZATION, ("result", result))
                && !_hasError
                && _eventTable.ContainsKey(UdonManager.UDON_EVENT_ONDESERIALIZATION))
                Logger.LogErrorFormat("OnDeserialization event failed for {0}", gameObject.name);
        }

        #endregion

        #region RunProgram Methods

        [PublicAPI]
        public override void RunProgram(string eventName)
        {
            if (_program == null)
            {
                return;
            }

            if(!_program.EntryPoints.HasExportedSymbol(eventName))
            {
                return;
            }

            uint address = _program.EntryPoints.GetAddressFromSymbol(eventName);
            RunProgram(address);
        }

        private void RunProgram(uint entryPoint)
        {
            if (_hasError)
            {
                return;
            }

            if (_udonVM == null)
            {
                return;
            }

            uint originalAddress = _udonVM.GetProgramCounter();
            UdonBehaviour originalExecuting = _udonManager.currentlyExecuting;

            _udonVM.SetProgramCounter(entryPoint);
            _udonManager.currentlyExecuting = this;

            _udonVM.DebugLogging = _udonManager.DebugLogging;

            try
            {
                _udonManager.IncrementDepthCount();
                uint result = _udonVM.Interpret();
                if (result != 0)
                {
                    Logger.LogError(
                        $"Udon VM execution errored, this UdonBehaviour will be halted.",
                        _categoryName,
                        this);

                    _hasError = true;
                    enabled = false;
                }
            }
            catch (UdonVMException error)
            {
                Logger.LogError(
                    "An exception occurred during Udon execution, this UdonBehaviour will be halted.\n" + error,
                    _categoryName,
                    this);

                _hasError = true;
                enabled = false;
            }
            finally
            {
                _udonManager.DecrementDepthCount();
            }

            // pi: note that _udonVM can be null here if the UdonBehaviour destroys itself during the Interpret call
            _udonManager.currentlyExecuting = originalExecuting;
            if (originalAddress < 0xFFFFFFFC && _udonVM != null)
            {
                _udonVM.SetProgramCounter(originalAddress);
            }
        }

        [PublicAPI]
        public ImmutableArray<string> GetPrograms()
        {
            return _program?.EntryPoints.GetExportedSymbols() ?? ImmutableArray<string>.Empty;
        }

        #endregion

        #region Serialization

        [SerializeField]
        private string serializedPublicVariablesBytesString;

        [SerializeField]
        private List<Object> publicVariablesUnityEngineObjects;

        [SerializeField]
        private DataFormat publicVariablesSerializationDataFormat = DataFormat.Binary;

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            DeserializePublicVariables();
        }

        private void DeserializePublicVariables()
        {
            byte[] serializedPublicVariablesBytes =
                Convert.FromBase64String(serializedPublicVariablesBytesString ?? "");

            publicVariables = SerializationUtility.DeserializeValue<IUdonVariableTable>(
                serializedPublicVariablesBytes,
                publicVariablesSerializationDataFormat,
                publicVariablesUnityEngineObjects
            ) ?? new UdonVariableTable();

            // Validate that the type of the value can actually be cast to the declaredType to avoid InvalidCastExceptions later.
            foreach (string publicVariableSymbol in publicVariables.VariableSymbols.ToArray())
            {
                if (!publicVariables.TryGetVariableValue(publicVariableSymbol, out object value))
                {
                    continue;
                }

                if (value == null)
                {
                    continue;
                }

                if (!publicVariables.TryGetVariableType(publicVariableSymbol, out Type declaredType))
                {
                    continue;
                }

                if (declaredType.IsInstanceOfType(value))
                {
                    continue;
                }

                if (declaredType.IsValueType)
                {
                    publicVariables.TrySetVariableValue(publicVariableSymbol, Activator.CreateInstance(declaredType));
                }
                else
                {
                    publicVariables.TrySetVariableValue(publicVariableSymbol, null);
                }
            }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            SerializePublicVariables();
        }

        private void SerializePublicVariables()
        {
            byte[] serializedPublicVariablesBytes = SerializationUtility.SerializeValue(
                publicVariables,
                publicVariablesSerializationDataFormat,
                out publicVariablesUnityEngineObjects);

            serializedPublicVariablesBytesString = Convert.ToBase64String(serializedPublicVariablesBytes);
        }

        #endregion

        #region IUdonBehaviour Interface

        [PublicAPI]
        public bool TryToInterrogateUdon<T0, T1>(string eventName, out object returnValue, (string symbolName, T0 value) parameter0, (string symbolName, T1 value) parameter1)
        {
            return TryToInterrogateUdon<object, T0, T1>(eventName, out returnValue, parameter0, parameter1);
        }

        private bool TryToInterrogateUdon<TOut, T0, T1>(string eventName, out TOut returnValue, (string symbolName, T0 value) parameter0, (string symbolName, T1 value) parameter1)
        {
            if (!_initialized || !enabled || !_hasDoneStart)
            {
#if VRC_CLIENT || UNITY_EDITOR
                if (UdonManager.Instance.DebugLogging)
                {
                    Logger.Log($"{gameObject.name} not ready to respond to {eventName}: initialized={_initialized} enabled={enabled} hasStarted={_hasDoneStart}", _categoryName);
                }
#endif

                returnValue = default;
                return false;
            }
            
            if (!_eventTable.ContainsKey(eventName))
            {
#if VRC_CLIENT || UNITY_EDITOR
                if (UdonManager.Instance.DebugLogging)
                {
                    Logger.Log($"{gameObject.name} will not respond to {eventName}", _categoryName);
                }
#endif

                returnValue = default;
                return false;
            }

            if(!RunEvent(eventName, parameter0, parameter1))
            {
#if VRC_CLIENT || UNITY_EDITOR
                if (UdonManager.Instance.DebugLogging)
                {
                    Logger.LogError($"{gameObject.name} failed to respond to {eventName}", _categoryName);
                }
#endif

                returnValue = default;
                return false;
            }

            returnValue = GetProgramVariable<TOut>(ReturnVariableName);
            return true;
        }

        public override bool RunEvent(string eventName) => RunEventAdvanced(eventName, canRunBeforeStart: false);
        public override bool RunEventAdvanced(string eventName, bool canRunBeforeStart)
        {
            if(DisableEventProcessing)
            {
                return false;
            }

            if(!_isReady)
            {
                return false;
            }

            if(!_hasDoneStart && !canRunBeforeStart)
            {
                return false;
            }

            if(_hasError)
            {
                return false;
            }

            if(_udonVM == null)
            {
                return false;
            }

            if(!_eventTable.TryGetValue(eventName, out List<uint> entryPoints))
            {
                return false;
            }

            foreach(uint entryPoint in entryPoints)
            {
                RunProgram(entryPoint);
            }

            return true;
        }

        #region RunEvent Parameter Overloads

        public override bool RunEvent<T0>(string eventName, (string symbolName, T0 value) parameter0) => RunEventAdvanced<T0>(eventName, true, false, parameter0);
        public override bool RunEventAdvanced<T0>(string eventName, bool mangleParameterNames, bool canRunBeforeStart, (string symbolName, T0 value) parameter0)
        {
            if(DisableEventProcessing)
            {
                return false;
            }

            if(!_isReady)
            {
                return false;
            }

            if(!_hasDoneStart && !canRunBeforeStart)
            {
                return false;
            }

            if(_hasError)
            {
                return false;
            }

            if(_udonVM == null)
            {
                return false;
            }

            if(!_eventTable.TryGetValue(eventName, out List<uint> entryPoints))
            {
                return false;
            }

            SetProgramVariable(mangleParameterNames ? GetEventParameterName(eventName, parameter0.symbolName) : parameter0.symbolName, parameter0.value);
            foreach(uint entryPoint in entryPoints)
            {
                RunProgram(entryPoint);
            }

            return true;
        }

        public override bool RunEvent<T0, T1>(string eventName, (string symbolName, T0 value) parameter0, (string symbolName, T1 value) parameter1) => RunEventAdvanced<T0, T1>(eventName, true, false, parameter0, parameter1);
        public override bool RunEventAdvanced<T0, T1>(string eventName, bool mangleParameterNames, bool canRunBeforeStart, (string symbolName, T0 value) parameter0, (string symbolName, T1 value) parameter1)
        {
            if(DisableEventProcessing)
            {
                return false;
            }

            if(!_isReady)
            {
                return false;
            }

            if(!_hasDoneStart && !canRunBeforeStart)
            {
                return false;
            }

            if(_hasError)
            {
                return false;
            }

            if(_udonVM == null)
            {
                return false;
            }

            if(!_eventTable.TryGetValue(eventName, out List<uint> entryPoints))
            {
                return false;
            }

            SetProgramVariable(mangleParameterNames ? GetEventParameterName(eventName, parameter0.symbolName) : parameter0.symbolName, parameter0.value);
            SetProgramVariable(mangleParameterNames ? GetEventParameterName(eventName, parameter1.symbolName) : parameter1.symbolName, parameter1.value);
            foreach(uint entryPoint in entryPoints)
            {
                RunProgram(entryPoint);
            }

            return true;
        }

        public override bool RunEvent<T0, T1, T2>(string eventName, (string symbolName, T0 value) parameter0, (string symbolName, T1 value) parameter1,
            (string symbolName, T2 value) parameter2) => RunEventAdvanced<T0, T1, T2>(eventName, true, false, parameter0, parameter1, parameter2);
        public override bool RunEventAdvanced<T0, T1, T2>(string eventName, bool mangleParameterNames, bool canRunBeforeStart, (string symbolName, T0 value) parameter0,
            (string symbolName, T1 value) parameter1, (string symbolName, T2 value) parameter2)
        {
            if(DisableEventProcessing)
            {
                return false;
            }

            if(!_isReady)
            {
                return false;
            }

            if(!_hasDoneStart && !canRunBeforeStart)
            {
                return false;
            }

            if(_hasError)
            {
                return false;
            }

            if(_udonVM == null)
            {
                return false;
            }

            if(!_eventTable.TryGetValue(eventName, out List<uint> entryPoints))
            {
                return false;
            }

            SetProgramVariable(mangleParameterNames ? GetEventParameterName(eventName, parameter0.symbolName) : parameter0.symbolName, parameter0.value);
            SetProgramVariable(mangleParameterNames ? GetEventParameterName(eventName, parameter1.symbolName) : parameter1.symbolName, parameter1.value);
            SetProgramVariable(mangleParameterNames ? GetEventParameterName(eventName, parameter2.symbolName) : parameter2.symbolName, parameter2.value);
            foreach(uint entryPoint in entryPoints)
            {
                RunProgram(entryPoint);
            }

            return true;
        }

        public override bool RunEvent<T0, T1, T2, T3>(string eventName, (string symbolName, T0 value) parameter0, (string symbolName, T1 value) parameter1,
            (string symbolName, T2 value) parameter2, (string symbolName, T3 value) parameter3) => RunEventAdvanced<T0, T1, T2, T3>(eventName, true, false, parameter0, parameter1, parameter2, parameter3);
        public override bool RunEventAdvanced<T0, T1, T2, T3>(string eventName, bool mangleParameterNames, bool canRunBeforeStart, (string symbolName, T0 value) parameter0,
            (string symbolName, T1 value) parameter1, (string symbolName, T2 value) parameter2, (string symbolName, T3 value) parameter3)
        {
            if(DisableEventProcessing)
            {
                return false;
            }

            if(!_isReady)
            {
                return false;
            }

            if(!_hasDoneStart && !canRunBeforeStart)
            {
                return false;
            }

            if(_hasError)
            {
                return false;
            }

            if(_udonVM == null)
            {
                return false;
            }

            if(!_eventTable.TryGetValue(eventName, out List<uint> entryPoints))
            {
                return false;
            }

            SetProgramVariable(mangleParameterNames ? GetEventParameterName(eventName, parameter0.symbolName) : parameter0.symbolName, parameter0.value);
            SetProgramVariable(mangleParameterNames ? GetEventParameterName(eventName, parameter1.symbolName) : parameter1.symbolName, parameter1.value);
            SetProgramVariable(mangleParameterNames ? GetEventParameterName(eventName, parameter2.symbolName) : parameter2.symbolName, parameter2.value);
            SetProgramVariable(mangleParameterNames ? GetEventParameterName(eventName, parameter3.symbolName) : parameter3.symbolName, parameter3.value);
            foreach(uint entryPoint in entryPoints)
            {
                RunProgram(entryPoint);
            }

            return true;
        }

        public override bool RunEvent<T0, T1, T2, T3, T4>(string eventName, (string symbolName, T0 value) parameter0, (string symbolName, T1 value) parameter1,
            (string symbolName, T2 value) parameter2, (string symbolName, T3 value) parameter3, (string symbolName, T4 value) parameter4) => RunEventAdvanced<T0, T1, T2, T3, T4>(eventName, true, false, parameter0, parameter1, parameter2, parameter3, parameter4);
        public override bool RunEventAdvanced<T0, T1, T2, T3, T4>(string eventName, bool mangleParameterNames, bool canRunBeforeStart, (string symbolName, T0 value) parameter0,
            (string symbolName, T1 value) parameter1, (string symbolName, T2 value) parameter2, (string symbolName, T3 value) parameter3, (string symbolName, T4 value) parameter4)
        {
            if(DisableEventProcessing)
            {
                return false;
            }

            if(!_isReady)
            {
                return false;
            }

            if(!_hasDoneStart && !canRunBeforeStart)
            {
                return false;
            }

            if(_hasError)
            {
                return false;
            }

            if(_udonVM == null)
            {
                return false;
            }

            if(!_eventTable.TryGetValue(eventName, out List<uint> entryPoints))
            {
                return false;
            }

            SetProgramVariable(mangleParameterNames ? GetEventParameterName(eventName, parameter0.symbolName) : parameter0.symbolName, parameter0.value);
            SetProgramVariable(mangleParameterNames ? GetEventParameterName(eventName, parameter1.symbolName) : parameter1.symbolName, parameter1.value);
            SetProgramVariable(mangleParameterNames ? GetEventParameterName(eventName, parameter2.symbolName) : parameter2.symbolName, parameter2.value);
            SetProgramVariable(mangleParameterNames ? GetEventParameterName(eventName, parameter3.symbolName) : parameter3.symbolName, parameter3.value);
            SetProgramVariable(mangleParameterNames ? GetEventParameterName(eventName, parameter4.symbolName) : parameter4.symbolName, parameter4.value);
            foreach(uint entryPoint in entryPoints)
            {
                RunProgram(entryPoint);
            }

            return true;
        }

        public override bool RunEvent<T0, T1, T2, T3, T4, T5>(string eventName, (string symbolName, T0 value) parameter0, (string symbolName, T1 value) parameter1,
            (string symbolName, T2 value) parameter2, (string symbolName, T3 value) parameter3, (string symbolName, T4 value) parameter4, (string symbolName, T5 value) parameter5) 
            => RunEventAdvanced<T0, T1, T2, T3, T4, T5>(eventName, true, false, parameter0, parameter1, parameter2, parameter3, parameter4, parameter5);
        public override bool RunEventAdvanced<T0, T1, T2, T3, T4, T5>(string eventName, bool mangleParameterNames, bool canRunBeforeStart, 
            (string symbolName, T0 value) parameter0, (string symbolName, T1 value) parameter1, (string symbolName, T2 value) parameter2, 
            (string symbolName, T3 value) parameter3, (string symbolName, T4 value) parameter4, (string symbolName, T5 value) parameter5)
        {
            if(DisableEventProcessing)
            {
                return false;
            }

            if(!_isReady)
            {
                return false;
            }

            if(!_hasDoneStart && !canRunBeforeStart)
            {
                return false;
            }

            if(_hasError)
            {
                return false;
            }

            if(_udonVM == null)
            {
                return false;
            }

            if(!_eventTable.TryGetValue(eventName, out List<uint> entryPoints))
            {
                return false;
            }

            SetProgramVariable(mangleParameterNames ? GetEventParameterName(eventName, parameter0.symbolName) : parameter0.symbolName, parameter0.value);
            SetProgramVariable(mangleParameterNames ? GetEventParameterName(eventName, parameter1.symbolName) : parameter1.symbolName, parameter1.value);
            SetProgramVariable(mangleParameterNames ? GetEventParameterName(eventName, parameter2.symbolName) : parameter2.symbolName, parameter2.value);
            SetProgramVariable(mangleParameterNames ? GetEventParameterName(eventName, parameter3.symbolName) : parameter3.symbolName, parameter3.value);
            SetProgramVariable(mangleParameterNames ? GetEventParameterName(eventName, parameter4.symbolName) : parameter4.symbolName, parameter4.value);
            SetProgramVariable(mangleParameterNames ? GetEventParameterName(eventName, parameter5.symbolName) : parameter5.symbolName, parameter5.value);
            foreach(uint entryPoint in entryPoints)
            {
                RunProgram(entryPoint);
            }

            return true;
        }

        public override bool RunEvent<T0, T1, T2, T3, T4, T5, T6>(string eventName, (string symbolName, T0 value) parameter0, (string symbolName, T1 value) parameter1,
            (string symbolName, T2 value) parameter2, (string symbolName, T3 value) parameter3, (string symbolName, T4 value) parameter4, 
            (string symbolName, T5 value) parameter5, (string symbolName, T6 value) parameter6) 
            => RunEventAdvanced<T0, T1, T2, T3, T4, T5, T6>(eventName, true, false, parameter0, parameter1, parameter2, parameter3, parameter4, parameter5, parameter6);
        public override bool RunEventAdvanced<T0, T1, T2, T3, T4, T5, T6>(string eventName, bool mangleParameterNames, bool canRunBeforeStart, 
            (string symbolName, T0 value) parameter0, (string symbolName, T1 value) parameter1, (string symbolName, T2 value) parameter2, 
            (string symbolName, T3 value) parameter3, (string symbolName, T4 value) parameter4, (string symbolName, T5 value) parameter5, 
            (string symbolName, T6 value) parameter6)
        {
            if(DisableEventProcessing)
            {
                return false;
            }

            if(!_isReady)
            {
                return false;
            }

            if(!_hasDoneStart && !canRunBeforeStart)
            {
                return false;
            }

            if(_hasError)
            {
                return false;
            }

            if(_udonVM == null)
            {
                return false;
            }

            if(!_eventTable.TryGetValue(eventName, out List<uint> entryPoints))
            {
                return false;
            }

            SetProgramVariable(mangleParameterNames ? GetEventParameterName(eventName, parameter0.symbolName) : parameter0.symbolName, parameter0.value);
            SetProgramVariable(mangleParameterNames ? GetEventParameterName(eventName, parameter1.symbolName) : parameter1.symbolName, parameter1.value);
            SetProgramVariable(mangleParameterNames ? GetEventParameterName(eventName, parameter2.symbolName) : parameter2.symbolName, parameter2.value);
            SetProgramVariable(mangleParameterNames ? GetEventParameterName(eventName, parameter3.symbolName) : parameter3.symbolName, parameter3.value);
            SetProgramVariable(mangleParameterNames ? GetEventParameterName(eventName, parameter4.symbolName) : parameter4.symbolName, parameter4.value);
            SetProgramVariable(mangleParameterNames ? GetEventParameterName(eventName, parameter5.symbolName) : parameter5.symbolName, parameter5.value);
            SetProgramVariable(mangleParameterNames ? GetEventParameterName(eventName, parameter6.symbolName) : parameter6.symbolName, parameter6.value);
            foreach(uint entryPoint in entryPoints)
            {
                RunProgram(entryPoint);
            }

            return true;
        }

        public override bool RunEvent<T0, T1, T2, T3, T4, T5, T6, T7>(string eventName, (string symbolName, T0 value) parameter0, (string symbolName, T1 value) parameter1,
            (string symbolName, T2 value) parameter2, (string symbolName, T3 value) parameter3, (string symbolName, T4 value) parameter4, 
            (string symbolName, T5 value) parameter5, (string symbolName, T6 value) parameter6, (string symbolName, T7 value) parameter7) 
            => RunEventAdvanced<T0, T1, T2, T3, T4, T5, T6, T7>(eventName, true, false, parameter0, parameter1, parameter2, parameter3, parameter4, parameter5, parameter6, parameter7);
        public override bool RunEventAdvanced<T0, T1, T2, T3, T4, T5, T6, T7>(string eventName, bool mangleParameterNames, bool canRunBeforeStart, 
            (string symbolName, T0 value) parameter0, (string symbolName, T1 value) parameter1, (string symbolName, T2 value) parameter2, 
            (string symbolName, T3 value) parameter3, (string symbolName, T4 value) parameter4, (string symbolName, T5 value) parameter5, 
            (string symbolName, T6 value) parameter6, (string symbolName, T7 value) parameter7)
        {
            if(DisableEventProcessing)
            {
                return false;
            }

            if(!_isReady)
            {
                return false;
            }

            if(!_hasDoneStart && !canRunBeforeStart)
            {
                return false;
            }

            if(_hasError)
            {
                return false;
            }

            if(_udonVM == null)
            {
                return false;
            }

            if(!_eventTable.TryGetValue(eventName, out List<uint> entryPoints))
            {
                return false;
            }

            SetProgramVariable(mangleParameterNames ? GetEventParameterName(eventName, parameter0.symbolName) : parameter0.symbolName, parameter0.value);
            SetProgramVariable(mangleParameterNames ? GetEventParameterName(eventName, parameter1.symbolName) : parameter1.symbolName, parameter1.value);
            SetProgramVariable(mangleParameterNames ? GetEventParameterName(eventName, parameter2.symbolName) : parameter2.symbolName, parameter2.value);
            SetProgramVariable(mangleParameterNames ? GetEventParameterName(eventName, parameter3.symbolName) : parameter3.symbolName, parameter3.value);
            SetProgramVariable(mangleParameterNames ? GetEventParameterName(eventName, parameter4.symbolName) : parameter4.symbolName, parameter4.value);
            SetProgramVariable(mangleParameterNames ? GetEventParameterName(eventName, parameter5.symbolName) : parameter5.symbolName, parameter5.value);
            SetProgramVariable(mangleParameterNames ? GetEventParameterName(eventName, parameter6.symbolName) : parameter6.symbolName, parameter6.value);
            SetProgramVariable(mangleParameterNames ? GetEventParameterName(eventName, parameter7.symbolName) : parameter7.symbolName, parameter7.value);
            foreach(uint entryPoint in entryPoints)
            {
                RunProgram(entryPoint);
            }

            return true;
        }

        public override bool RunEvent(string eventName, params (string symbolName, object value)[] programVariables) => RunEventAdvanced(eventName, true, false, programVariables);
        public override bool RunEventAdvanced(string eventName, bool mangleParameterNames, bool canRunBeforeStart, params (string symbolName, object value)[] programVariables)
        {
            if(DisableEventProcessing)
            {
                return false;
            }

            if(!_isReady)
            {
                return false;
            }

            if (!_hasDoneStart && !canRunBeforeStart)
            {
                return false;
            }

            if (_hasError)
            {
                return false;
            }

            if (_udonVM == null)
            {
                return false;
            }

            if (!_eventTable.TryGetValue(eventName, out List<uint> entryPoints))
            {
                return false;
            }

            //TODO: Replace with a non-boxing interface before exposing to users
            foreach ((string symbolName, object value) in programVariables)
            {
                var newSymbolName = mangleParameterNames ? GetEventParameterName(eventName, symbolName) : symbolName;
                SetProgramVariable(newSymbolName, value);
            }

            foreach (uint entryPoint in entryPoints)
            {
                RunProgram(entryPoint);
            }

            return true;
        }

        #endregion

        public override void RunInputEvent(string eventName, UdonInputEventArgs args)
        {
            if (!_isReady)
            {
                return;
            }

            if (!_hasDoneStart)
            {
                return;
            }

            if(!_program.EntryPoints.HasExportedSymbol(eventName))
            {
                return;
            }

            // Set value arg
            switch (args.eventType)
            {
                case UdonInputEventType.AXIS:
                    SetProgramVariable(GetEventParameterName(eventName, "floatValue"), args.floatValue);
                    break;
                case UdonInputEventType.BUTTON:
                    SetProgramVariable(GetEventParameterName(eventName, "boolValue"), args.boolValue);
                    break;
            }

            // Set event args
            SetProgramVariable(GetEventParameterName(eventName, "args"), args);
            RunProgram(eventName);
        }

        private string GetEventParameterName(string eventName, string symbolName)
        {
            if (!_symbolNameCache.TryGetValue((eventName, symbolName), out string newSymbolName))
            {
                newSymbolName = $"{eventName.Substring(1)}{char.ToUpper(symbolName.First())}{symbolName.Substring(1)}";
                _symbolNameCache.Add((eventName, symbolName), newSymbolName);
            }

            return newSymbolName;
        }

        private ProfilerMarker _preloadUdonProgramProfilerMarker = new ProfilerMarker("UdonBehaviour.PreloadUdonProgram");

        public void PreloadUdonProgram(IUdonSignatureVerifier signatureVerifier)
        {
            using(_preloadUdonProgramProfilerMarker.Auto())
            {
                if(serializedProgramAsset == null)
                {
                    return;
                }

                if(_program != null)
                {
                    return;
                }

                signatureVerifier.VerifySignature(serializedProgramAsset as IUdonSignatureHolder);
                _program = serializedProgramAsset.RetrieveProgram();
            }
        }

        private ProfilerMarker _initializeUdonContentProfilerMarker = new ProfilerMarker("UdonBehaviour.InitializeUdonContent");

        private T SearchUdonInterface<T>() where T : class
        {
            // while a scene is loading we do not allow searching the hierarchy
            if (_udonManager.IsSceneLoading)
                return _udonManager as T;
            var foundInParent = GetComponentInParent<T>();
            if (foundInParent == null || (foundInParent as Component) == null)
                return _udonManager as T;
            return foundInParent;
        }

        public override void InitializeUdonContent()
        {
            using(_initializeUdonContentProfilerMarker.Auto())
            {
                if(_initialized)
                {
                    return;
                }

                SetupLogging();

                _udonManager = UdonManager.Instance;
                if(_udonManager == null)
                {
                    enabled = false;
                    Logger.LogError(
                        $"Could not find the UdonManager; the UdonBehaviour on '{gameObject.name}' will not run.",
                        _categoryName,
                        this);

                    return;
                }

                IUdonSignatureVerifier signatureVerifier = SearchUdonInterface<IUdonSignatureVerifier>();
                if(!LoadProgram(signatureVerifier))
                {
                    enabled = false;
                    Logger.Log(
                        $"Could not load the program; the UdonBehaviour on '{gameObject.name}' will not run.",
                        _categoryName,
                        this);

                    return;
                }

                // Let UdonManager apply any processing or scans.
                _udonManager.ProcessUdonProgram(_program);

                IUdonSymbolTable symbolTable = _program?.SymbolTable;
                IUdonHeap heap = _program?.Heap;
                if(symbolTable == null || heap == null)
                {
                    enabled = false;
                    Logger.Log(
                        $"Invalid program; the UdonBehaviour on '{gameObject.name}' will not run.",
                        _categoryName,
                        this);

                    return;
                }

                if(!ResolveUdonHeapReferences(symbolTable, heap))
                {
                    enabled = false;
                    Logger.Log(
                        $"Failed to resolve a GameObject/Component Reference; the UdonBehaviour on '{gameObject.name}' will not run.",
                        _categoryName,
                        this);

                    return;
                }

                IUdonClientInterface clientInterface = SearchUdonInterface<IUdonClientInterface>();
                _udonVM = clientInterface.ConstructUdonVM();

                if(_udonVM == null)
                {
                    enabled = false;
                    Logger.LogError(
                        $"No UdonVM; the UdonBehaviour on '{gameObject.name}' will not run.",
                        _categoryName,
                        this);

                    return;
                }

                _udonVM.LoadProgram(_program);

                ProcessEntryPoints();

                #if !VRC_CLIENT
                _isReady = true;
                #else
                if(!_isNetworkingSupported)
                {
                    _isReady = true;
                }
                #endif

                _initialized = true;

                RunOnInit();
            }
        }

        [PublicAPI]
        public void RunOnInit()
        {
            if (OnInit == null)
            {
                return;
            }

            try
            {
                OnInit(this, _program);
            }
            catch (Exception exception)
            {
                enabled = false;
                Logger.LogError(
                    $"An exception '{exception.Message}' occurred during initialization; the UdonBehaviour on '{gameObject.name}' will not run. Exception:\n{exception}",
                    _categoryName,
                    this
                );
            }
        }

        private void RegisterUpdate()
        {
            if (_udonManager == null)
            {
                return;
            }

            if (!isActiveAndEnabled)
            {
                return;
            }

            if (_hasUpdateEvent || !_hasDoneStart)
            {
                _udonManager.RegisterUdonBehaviourUpdate(this);
            }

            if (_hasLateUpdateEvent)
            {
                _udonManager.RegisterUdonBehaviourLateUpdate(this);
            }

            if (_hasFixedUpdateEvent)
            {
                _udonManager.RegisterUdonBehaviourFixedUpdate(this);
            }

            if (_hasPostLateUpdateEvent)
            {
                _udonManager.RegisterUdonBehaviourPostLateUpdate(this);
            }
            
            foreach (AbstractUdonBehaviourEventProxy proxy in _eventProxies)
            {
                proxy.enabled = true;
            }
        }

        private void UnregisterUpdate()
        {
            if (_udonManager == null)
            {
                return;
            }

            if (_hasUpdateEvent)
            {
                _udonManager.UnregisterUdonBehaviourUpdate(this);
            }

            if (_hasLateUpdateEvent)
            {
                _udonManager.UnregisterUdonBehaviourLateUpdate(this);
            }

            if (_hasFixedUpdateEvent)
            {
                _udonManager.UnregisterUdonBehaviourFixedUpdate(this);
            }

            if (_hasPostLateUpdateEvent)
            {
                _udonManager.UnregisterUdonBehaviourPostLateUpdate(this);
            }
            
            foreach (AbstractUdonBehaviourEventProxy proxy in _eventProxies)
            {
                proxy.enabled = false;
            }
        }

        #region IUdonEventReceiver and IUdonSyncTarget Interface

        #region IUdonEventReceiver Only

        public override void SendCustomEvent(string eventName)
        {
            RunProgram(eventName);
        }

        public override void SendCustomNetworkEvent(NetworkEventTarget target, string eventName)
            => NetworkCalling.SendCustomNetworkEvent(this, target, eventName);
        public override void SendCustomNetworkEvent(NetworkEventTarget target, string eventName, object parameter0)
            => NetworkCalling.SendCustomNetworkEvent(this, target, eventName, parameter0);
        public override void SendCustomNetworkEvent(NetworkEventTarget target, string eventName, object parameter0, object parameter1)
            => NetworkCalling.SendCustomNetworkEvent(this, target, eventName, parameter0, parameter1);
        public override void SendCustomNetworkEvent(NetworkEventTarget target, string eventName, object parameter0, object parameter1, object parameter2)
            => NetworkCalling.SendCustomNetworkEvent(this, target, eventName, parameter0, parameter1, parameter2);
        public override void SendCustomNetworkEvent(NetworkEventTarget target, string eventName, object parameter0, object parameter1, object parameter2, object parameter3)
            => NetworkCalling.SendCustomNetworkEvent(this, target, eventName, parameter0, parameter1, parameter2, parameter3);
        public override void SendCustomNetworkEvent(NetworkEventTarget target, string eventName, object parameter0, object parameter1, object parameter2, object parameter3, object parameter4)
            => NetworkCalling.SendCustomNetworkEvent(this, target, eventName, parameter0, parameter1, parameter2, parameter3, parameter4);
        public override void SendCustomNetworkEvent(NetworkEventTarget target, string eventName, object parameter0, object parameter1, object parameter2, object parameter3, object parameter4, object parameter5)
            => NetworkCalling.SendCustomNetworkEvent(this, target, eventName, parameter0, parameter1, parameter2, parameter3, parameter4, parameter5);
        public override void SendCustomNetworkEvent(NetworkEventTarget target, string eventName, object parameter0, object parameter1, object parameter2, object parameter3, object parameter4, object parameter5, object parameter6)
            => NetworkCalling.SendCustomNetworkEvent(this, target, eventName, parameter0, parameter1, parameter2, parameter3, parameter4, parameter5, parameter6);
        public override void SendCustomNetworkEvent(NetworkEventTarget target, string eventName, object parameter0, object parameter1, object parameter2, object parameter3, object parameter4, object parameter5, object parameter6, object parameter7)
            => NetworkCalling.SendCustomNetworkEvent(this, target, eventName, parameter0, parameter1, parameter2, parameter3, parameter4, parameter5, parameter6, parameter7);

        public override void RequestSerialization()
        {
            RequestSerializationHook?.Invoke(this);
        }
        
        public override void SendCustomEventDelayedSeconds(string eventName, float delaySeconds, EventTiming eventTiming = EventTiming.Update)
        {
            UdonManager.Instance.ScheduleDelayedEvent(this, eventName, delaySeconds, eventTiming);
        }

        public override void SendCustomEventDelayedFrames(string eventName, int delayFrames, EventTiming eventTiming = EventTiming.Update)
        {
            UdonManager.Instance.ScheduleDelayedEvent(this, eventName, delayFrames, eventTiming);
        }

        public override string InteractionText
        {
            get => interactText;
            set => interactText = value;
        }

        #endregion

        #region IUdonSyncTarget

        public override IUdonSyncMetadataTable SyncMetadataTable => _program?.SyncMetadataTable;

        #endregion

        #region Shared

        public override Type GetProgramVariableType(string symbolName)
        {
            if (!_program.SymbolTable.HasAddressForSymbol(symbolName))
            {
                return null;
            }

            uint symbolAddress = _program.SymbolTable.GetAddressFromSymbol(symbolName);
            return _program.Heap.GetHeapVariableType(symbolAddress);
        }

        public override void SetProgramVariable<T>(string symbolName, T value)
        {
            if (_program == null)
            {
                return;
            }

            if (!_program.SymbolTable.TryGetAddressFromSymbol(symbolName, out uint symbolAddress))
            {
                return;
            }

            SetHeapVariable(symbolAddress, value);
        }

        public override void SetProgramVariable(string symbolName, object value)
        {
            if (_program == null)
            {
                return;
            }

            if (!_program.SymbolTable.TryGetAddressFromSymbol(symbolName, out uint symbolAddress))
            {
                return;
            }

            SetHeapVariable( symbolAddress, value);
        }

        private void SetHeapVariable<T>(uint symbolAddress, T newValue)
        {
            if (_variableToChangeEvent.TryGetValue(symbolAddress, out (uint eventAddress, uint oldVariableAddress) data))
            {
                // cache value before changing
                T value = _program.Heap.GetHeapVariable<T>(symbolAddress);

                // check for change and trigger event
                if(!value?.Equals(newValue) ?? newValue != null)
                {
                    // change the variable on the heap
                    _program.Heap.SetHeapVariable(symbolAddress, newValue);
                    
                    // change the old variable on the heap
                    if (data.oldVariableAddress != uint.MaxValue)
                    {
                        _program.Heap.SetHeapVariable(data.oldVariableAddress, value);
                    }

                    // trigger the event
                    RunProgram(data.eventAddress);
                }
            }
            else
            {
                // just change the variable on the heap
                _program.Heap.SetHeapVariable(symbolAddress, newValue);
            }
        }

        public override T GetProgramVariable<T>(string symbolName)
        {
            if (_program == null)
            {
                return default;
            }

            if (!_program.SymbolTable.TryGetAddressFromSymbol(symbolName, out uint symbolAddress))
            {
                return default;
            }

            return _program.Heap.GetHeapVariable<T>(symbolAddress);
        }

        public override object GetProgramVariable(string symbolName)
        {
            if (_program == null)
            {
                return null;
            }

            if (!_program.SymbolTable.TryGetAddressFromSymbol(symbolName, out uint symbolAddress))
            {
#if UNITY_EDITOR
                Logger.LogError($"Could not find symbol {symbolName}; available: [{string.Join(",", _program.SymbolTable.GetSymbols())}]", _categoryName);
#endif
                return null;
            }

            return _program.Heap.GetHeapVariable(symbolAddress);
        }

        public override bool TryGetProgramVariable<T>(string symbolName, out T value)
        {
            value = default;
            if (_program == null)
            {
                return false;
            }

            if (!_program.SymbolTable.TryGetAddressFromSymbol(symbolName, out uint symbolAddress))
            {
                return false;
            }

            return _program.Heap.TryGetHeapVariable(symbolAddress, out value);
        }

        public override bool TryGetProgramVariable(string symbolName, out object value)
        {
            value = null;
            if (_program == null)
            {
                return false;
            }

            if (!_program.SymbolTable.TryGetAddressFromSymbol(symbolName, out uint symbolAddress))
            {
                return false;
            }

            return _program.Heap.TryGetHeapVariable(symbolAddress, out value);
        }

        #endregion

        #endregion

        #endregion

        #region Logging Methods

        private void SetupLogging()
        {
            _categoryName = "UdonBehaviour";
            if (Logger.CategoryIsDescribed(_categoryName))
            {
                return;
            }

            Logger.DescribeCategory(_categoryName);
            Logger.EnableCategory(_categoryName);
        }

        #endregion

        #region Manual Initialization Methods

        [PublicAPI]
        public void AssignProgramAndVariables(AbstractSerializedUdonProgramAsset compiledAsset,
            IUdonVariableTable variables)
        {
            serializedProgramAsset = compiledAsset;
            publicVariables = variables;
        }

        #endregion
    }
}
