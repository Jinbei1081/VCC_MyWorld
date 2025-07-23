
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;
using UnityEngine.UI;
using System;
using VRC.Udon.Common;

namespace yoshio_will.VisitorsInformationBoard
{
    public enum Platform    // 256個まで
    {
        PC, Android, Linux, MacOSX, iOS, PS4, XBOXOne, tvOS, WSA, WebGL, Editor, Unknown
    }

    public enum ClassifiedInputMethod   // 128個まで
    {
        Desktop, Gamepad, SomethingVR, Vive, Oculus, Index, HPReverb, Touch, OSC, OpenXR, Pico, SteamVR2, Embodied
    }

    public enum PortraitRenderingMode
    {
        ManualRender, OnRenderObject
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class VisitorsInformationBoard : UdonSharpBehaviour
    {
        [Header("User Interface - User List")]
        [SerializeField] private string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
        [SerializeField] private string ShortDateTimeFormat = "MM-dd HH:mm";
        [SerializeField] private string JoinStatusTag = "<size=20>?</size>";
        [SerializeField] private Color ActiveUIColor = new Color(1, 1, 1, 1);
        [SerializeField] private Color InactiveUIColor = new Color(1, 1, 1, 0.25f);
        [SerializeField] private bool IsSortUserList = true;

        [Header("User Interface - Join Log")]
        [SerializeField] private string JoinString = "<color=#3C3>[join]</color>";
        [SerializeField] private string LeftString = "<color=#C33>[left]</color>";
        [SerializeField] private string LogDateTimeFormat = "MM-dd HH:mm";
        [SerializeField] private float LineHeight = 41.5f;

        [Header("Portrait Photo - System")]
        [SerializeField] private bool IsUsePhotoSystem = true;
        [SerializeField] private float PhotoShotDelay = 3f;
        [SerializeField] private float RetakeIntervalBase = 300f;
        [SerializeField] private float RetakeIntervalDiff = 30f;
        [SerializeField] private float PhotoShotCheckInterval = 1f;
        [SerializeField] private Texture DummyTexture;
        [SerializeField] private PortraitRenderingMode PortraitRenderingMode = PortraitRenderingMode.ManualRender;
        [SerializeField] private int OnRenderObjectIteration = 1;

        [Header("Portrait Photo - Picture")]
        [SerializeField] private int PhotoSize = 256;
        [SerializeField] private float CameraDistance = 2f;
        [SerializeField] private float CameraClippingDepth = 0.5f;
        [SerializeField] private float HeadOffset = 0.075f;
        [SerializeField] private float GenericAvatarOffset = 1;
        [SerializeField] private float AvatarEyeHeightBasis = 1.2f;

        private VRCPlayerApi _localPlayer;

        private TextMeshProUGUI _textInstanceCreated, _textPersonsCounter, _textJoinLogContent;
        private GameObject _playerList, _playerListTemplate, _joinLog;
        private Toggle _togglePage;

        private bool _isMyInformationRegistered = false;

        private int _currentPersons;
        [UdonSynced] private long _instanceCreatedDateTime;
        [UdonSynced] private byte[] _statuses;
        [UdonSynced] private string[] _displayNames;
        [UdonSynced] private ushort[] _environments;
        [UdonSynced] private long[] _joinDateTime;
        [UdonSynced] private long[] _leftDateTime;
        [UdonSynced] private byte[] _joinCount;
        private long _myJoinDateTime;

        private bool[] _isJoinLogs;
        private long[] _dateTimeLogs;
        private string[] _nameLogs;

        private RenderTexture[] _photos;
        private string[] _photoNames;
        private Camera _camera;
        private float _nextPhotoShotCheckTime;
        private Animator _animator;
        private int _disableCameraCounter = 0;

        private float[] _photoShotReservedTimes;
        private string[] _photoShotReservedNames;

        const int InstanceIsFreshInSeconds = 30;
        const float OnPlayerLeftDelayedInSeconds = 2.5f;
        const float MinimumCameraDistance = 0.2f;
        const float MinimumCameraNearClip = 0.1f;

        void Start()
        {
            // 変数など準備
            _localPlayer = Networking.LocalPlayer;
            _camera = GetComponentInChildren<Camera>(true);
            _camera.enabled = false;
            _animator = GetComponentInChildren<Animator>();

            // Hierarchy要素取得
            Transform[] trans = GetComponentsInChildren<Transform>(true);
            foreach (var tran in trans)
            {
                if (tran.name.Substring(0, 1) != "#") continue;
                switch (tran.name)
                {
                    case "#InstanceCreated": _textInstanceCreated = tran.GetComponent<TextMeshProUGUI>(); break;
                    case "#PersonsCounter": _textPersonsCounter = tran.GetComponent<TextMeshProUGUI>(); break;
                    case "#JoinLogContent": _textJoinLogContent = tran.GetComponent<TextMeshProUGUI>(); break;
                    case "#PlayerList": _playerList = tran.gameObject; break;
                    case "#Template": _playerListTemplate = tran.gameObject; break;
                    case "#JoinLog": _joinLog = tran.gameObject; break;
                    case "#PageToggle": _togglePage = tran.GetComponent<Toggle>(); break;
                }
            }

            // とりあえず現在日時をインスタンス作成日時として変数に入れる
            // 自分がインスタンスマスターでなかったら同期で上書きされて消える
            _instanceCreatedDateTime = DateTime.UtcNow.Ticks;

            // 自分のJoin日時はこのスクリプトが開始された時。以後二度といじらないこと
            _myJoinDateTime = DateTime.UtcNow.Ticks;

            // 配列初期化
            InitializeArrayIfNeeded();

            DebugLog(0, false, "Started ver-1.06");
        }

        private void ReservePhotoShot(string name, float time)
        {
            // 顔写真の撮影を予約する
            if (!IsUsePhotoSystem) return;

            int newIdx = _photoShotReservedTimes.Length;
            int oldLength = newIdx;
            int newLength = oldLength + 1;

            float[] newTimes = new float[newLength];
            string[] newNames = new string[newLength];

            Array.Copy(_photoShotReservedNames, newNames, oldLength);
            Array.Copy(_photoShotReservedTimes, newTimes, oldLength);

            newTimes[newIdx] = time;
            newNames[newIdx] = name;

            _photoShotReservedTimes = newTimes;
            _photoShotReservedNames = newNames;

            DebugLog(0, true, "Photoshot Reserved / Index=" + newIdx + ", Time=" + _photoShotReservedTimes[newIdx] + ", Name=" + _photoShotReservedNames[newIdx]);
        }

        private void CancelPhotoShotReservation(string name)
        {
            // 顔写真の撮影をキャンセルする
            int src = 0;
            int dst = 0;
            string[] newNames = new string[_photoShotReservedNames.Length];
            float[] newTimes = new float[_photoShotReservedTimes.Length];
            while (src < _photoShotReservedNames.Length)
            {
                if (_photoShotReservedNames[src] != name)
                {
                    newNames[dst] = _photoShotReservedNames[src];
                    newTimes[dst] = _photoShotReservedTimes[src];
                    dst++;
                }
                src++;
            }

            _photoShotReservedNames = new string[dst];
            _photoShotReservedTimes = new float[dst];
            Array.Copy(newNames, _photoShotReservedNames, dst);
            Array.Copy(newTimes, _photoShotReservedTimes, dst);
        }

        private void CancelPhotoShotReservation(int reserveIdx)
        {
            int headLength = reserveIdx;
            int tailLength = _photoShotReservedNames.Length - reserveIdx - 1;
            int oldLength = _photoShotReservedNames.Length;
            int newLength = oldLength - 1;
            string[] newNames = new string[newLength];
            float[] newTimes = new float[newLength];
            if (headLength > 0)
            {
                Array.Copy(_photoShotReservedNames, 0, newNames, 0, headLength);
                Array.Copy(_photoShotReservedTimes, 0, newTimes, 0, headLength);
            }
            if (tailLength > 0)
            {
                Array.Copy(_photoShotReservedNames, reserveIdx + 1, newNames, reserveIdx, tailLength);
                Array.Copy(_photoShotReservedTimes, reserveIdx + 1, newTimes, reserveIdx, tailLength);
            }
            _photoShotReservedNames = newNames;
            _photoShotReservedTimes = newTimes;
        }

        private void Update()
        {
            // カメラ無効化する必要あり？
            if (_disableCameraCounter > 0)
            {
                _camera.enabled = false;
                _disableCameraCounter--;
            }

            // 写真撮影の予約があれば処理する
            if (IsUsePhotoSystem && Time.time > _nextPhotoShotCheckTime && _photoShotReservedTimes.Length > 0)
            {
                int idx = 0;
                while (idx < _photoShotReservedTimes.Length)
                {
                    if (Time.time > _photoShotReservedTimes[idx])
                    {
                        ProcessPhotoShot(idx);
                        break;
                    }
                    idx++;
                }
                _nextPhotoShotCheckTime = Time.time + PhotoShotCheckInterval;
            }
        }

        private void ProcessPhotoShot(int reserveIdx)
        {
            DebugLog(0, true, "ProcessPhotoShot / ReserveIndex=" + reserveIdx);

            // 写真撮影
            if (reserveIdx < 0 || reserveIdx >= _photoShotReservedNames.Length) return;
            VRCPlayerApi player = GetPlayerByDisplayName(_photoShotReservedNames[reserveIdx]);
            if (!Utilities.IsValid(player))
            {
                if (_photoShotReservedNames[reserveIdx] != _localPlayer.displayName)
                {
                    // いなくなったプレイヤー。中止
                    CancelPhotoShotReservation(reserveIdx);
                }
                else
                {
                    // 自分だが何故か撮れていない（？）　リトライする
                    ReservePhotoShot(_localPlayer.displayName, Time.time + PhotoShotDelay);
                }
                return;
            }

            // RenderTextureを選ぶ
            int photoIdx = GetPhotoIndexByDisplayName(player.displayName);
            if (photoIdx < 0)
            {
                // 既存のRenderTextureがないなら作成
                int newElements = _photos.Length + 1;
                RenderTexture[] newPhotos = new RenderTexture[newElements];
                string[] newPhotoNames = new string[newElements];
                Array.Copy(_photos, newPhotos, _photos.Length);
                Array.Copy(_photoNames, newPhotoNames, _photoNames.Length);
                _photos = newPhotos;
                _photoNames = newPhotoNames;

                photoIdx = _photos.Length - 1;
                //_photos[photoIdx] = new RenderTexture(PhotoSize, PhotoSize, 0, RenderTextureFormat.DefaultHDR);
                _photos[photoIdx] = new RenderTexture(PhotoSize, PhotoSize, 16, RenderTextureFormat.DefaultHDR);
                _photoNames[photoIdx] = player.displayName;
            }
            DebugLog(0, true, "ProcessPhotoShot / Use RenderTexture Index=" + photoIdx);

            // 撮影
            _camera.targetTexture = _photos[photoIdx];
            float avatarScale = player.GetAvatarEyeHeightAsMeters() / AvatarEyeHeightBasis;
            Vector3 headPos = player.GetBonePosition(HumanBodyBones.Head);
            Quaternion headRot = player.GetBoneRotation(HumanBodyBones.Head);
            if (headPos != Vector3.zero)
            {
                // Humanoid
                Vector3 target = headPos + headRot * (Vector3.up * HeadOffset * avatarScale);
                _camera.transform.position = target + headRot * (Vector3.forward * Mathf.Max(CameraDistance * avatarScale, MinimumCameraDistance));
                _camera.transform.rotation = Quaternion.LookRotation(target - _camera.transform.position);
                _camera.farClipPlane = (CameraDistance + CameraClippingDepth) * avatarScale;
                _camera.nearClipPlane = Mathf.Max((CameraDistance - CameraClippingDepth) * avatarScale, MinimumCameraNearClip);
            }
            else
            {
                //Generic
                Vector3 target = player.GetPosition() + Vector3.up * GenericAvatarOffset * avatarScale;
                _camera.transform.position = player.GetRotation() * (Vector3.forward * Mathf.Max(CameraDistance * avatarScale, MinimumCameraDistance));
                _camera.transform.rotation = Quaternion.LookRotation(target - _camera.transform.position);
                _camera.farClipPlane = (CameraDistance + CameraClippingDepth) * avatarScale;
                _camera.nearClipPlane = Mathf.Max((CameraDistance - CameraClippingDepth) * avatarScale, MinimumCameraNearClip);
            }

            // カメラのレンダリング
            switch(PortraitRenderingMode)
            {
                case PortraitRenderingMode.OnRenderObject:
                    _camera.enabled = true;
                    _disableCameraCounter = OnRenderObjectIteration;
                    break;

                case PortraitRenderingMode.ManualRender:
                    _camera.Render();
                    break;
            }

            // 処理済みの予約を削除
            CancelPhotoShotReservation(reserveIdx);

            // 表示更新
            UpdateDisplay();

            // 撮影終わったらもう一度撮影予約
            ReservePhotoShot(player.displayName, Time.time + RetakeIntervalBase + UnityEngine.Random.Range(0, RetakeIntervalDiff));
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            DebugLog(0, true, "OnPlayerJoined / player=" + GetPlayerString(player));
            if (!Utilities.IsValid(player)) return;
            if (player.isLocal)
            {
                // 自分がJoinした時の処理
                RegisterMyInformation(true);
            }

            // どっちにしろやる処理
            _currentPersons = VRCPlayerApi.GetPlayerCount();
            ReservePhotoShot(player.displayName, Time.time + PhotoShotDelay);
            RefreshStatusInfo();
            AddJoinLog(true, player.displayName);
            UpdateJoinLog();
            UpdateDisplay();
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player)) return;

            // 自分がオーナーだったら退出時間を記録する
            if (Networking.IsOwner(gameObject))
            {
                int idx = GetPersonIndexByDisplayName(player.displayName);
                if (idx > 0)
                {
                    _leftDateTime[idx] = DateTime.UtcNow.Ticks;
                    RequestSerialization();
                }
            }

            if (player.isLocal) return;     // これより先はやらなくていいや

            _currentPersons = VRCPlayerApi.GetPlayerCount();
            RefreshStatusInfo();
            AddJoinLog(false, player.displayName);
            UpdateJoinLog();
            UpdateDisplay();

            SendCustomEventDelayedSeconds(nameof(OnPlayerLeftDelayed), OnPlayerLeftDelayedInSeconds);
        }

        public void OnPlayerLeftDelayed()
        {
            // プレイヤーいなくなった時の処理は数秒置いてもう一度やる（念のため）
            _currentPersons = VRCPlayerApi.GetPlayerCount();
            RefreshStatusInfo();
            UpdateDisplay();
        }

        public override void OnDeserialization()
        {
            DebugLog(0, true, "OnDeserialization");

            if (Networking.IsOwner(gameObject))
            {
                // OwnerなのにOnDeserializationを受けたってことはオーナー移譲が今終わった可能性が高い
                // なのでもう一度登録をやりなおす
                //_isMyInformationRegistered = false;
                RegisterMyInformation(!_isMyInformationRegistered);
            }

            // 配列に自分が入っているか念のため確認する
            int myIdx = GetPersonIndexByDisplayName(_localPlayer.displayName);
            if (myIdx < 0 || myIdx >= _displayNames.Length)
            {
                // 同期変数を受信したが自分が配列にいないぞ！という場合登録をする
                RegisterMyInformation(true);
            }

            UpdateDisplay();
        }

        private void RefreshStatusInfo()
        {
            // 自分がオーナーだったら配列のステータス情報を更新して同期する
            if (!Networking.IsOwner(gameObject)) return;

            for (int idx = 0; idx < _displayNames.Length; idx++)
            {
                VRCPlayerApi p = GetPlayerByDisplayName(_displayNames[idx]);
                bool isExist = (Utilities.IsValid(p));
                bool isInstanceOwner = false;
                if (isExist)
                {
                    isInstanceOwner = p.isInstanceOwner;
                }
                else
                {
                    isInstanceOwner = ((_statuses[idx] & 0x02) != 0);
                }

                _statuses[idx] = (byte)(
                    ((isExist) ? 0x01 : 0x00) |
                    ((isInstanceOwner) ? 0x02 : 0x00)
                    );

                // いないのに退出時間が書いてなかったら今ってことにする
                if (!isExist && _leftDateTime[idx] == 0) _leftDateTime[idx] = DateTime.UtcNow.Ticks;
            }

            if (IsSortUserList) SortPersonsArray();

            RequestSerialization();
        }

        private void UpdateDisplay()
        {
            DebugLog(0, true, "UpdateDisplay / persons = " + _displayNames.Length);

            Transform[] trans;
            RectTransform parent = (RectTransform)_playerListTemplate.transform.parent;

            // まずは掃除
            trans = parent.GetComponentsInChildren<Transform>(false);
            foreach (var tran in trans) if (tran.parent == parent) Destroy(tran.gameObject);

            // 簡単なやつを先にやる
            DateTime created = new DateTime(_instanceCreatedDateTime).ToLocalTime();
            _textInstanceCreated.text = created.ToString(DateTimeFormat);
            _textPersonsCounter.text = string.Format("{0} / {1}", _currentPersons, _displayNames.Length);

            // 順に追加
            Texture texture;
            RectTransform templateRect = ((RectTransform)_playerListTemplate.transform);
            Vector2 pos = templateRect.anchoredPosition;
            float height = templateRect.sizeDelta.y;
            for (int idx = 0; idx < _displayNames.Length; idx++)
            {
                GameObject newObj = Instantiate(_playerListTemplate.gameObject);
                RectTransform tran = (RectTransform)newObj.transform;
                tran.SetParent(parent, false);
                tran.anchoredPosition = pos;
                newObj.SetActive(true);
                newObj.name = idx.ToString();

                trans = tran.GetComponentsInChildren<Transform>();
                foreach(var element in trans)
                {
                    if (element.name.Substring(0, 1) != "#") continue;
                    TextMeshProUGUI text = element.GetComponent<TextMeshProUGUI>();
                    bool isExist = ((_statuses[idx] & 0x01) != 0);
                    Color uiColor = isExist ? ActiveUIColor : InactiveUIColor;
                    if (text) text.color = uiColor;
                    switch (element.name)
                    {
                        case "#Master": text.text = GetMasterIconByStatus(_statuses[idx]); break;
                        case "#Name": text.text = _displayNames[idx] + "\n" + GetJoinStatusString(idx); break;
                        case "#Environment": text.text = GetEnvironmentString(_environments[idx]); break;
                        case "#Photo":
                            int photoIdx = GetPhotoIndexByDisplayName(_displayNames[idx]);
                            if (photoIdx < 0 || photoIdx >= _photos.Length) texture = DummyTexture;
                            else texture = _photos[photoIdx];
                            RawImage img = element.GetComponent<RawImage>();
                            img.texture = texture;
                            img.color = uiColor;
                            break;
                    }
                }

                pos -= new Vector2(0, height);
            }
            parent.sizeDelta = new Vector2(parent.sizeDelta.x, height * _displayNames.Length);
        }

        private string GetJoinStatusString(int idx)
        {
            // 0 : Join/Left時刻が両方ない　←？？？
            // 1 : Join時刻だけ記録
            // 2 : Left時刻だけ記録　←？？？
            // 3 : Join/Left時刻が両方記録
            int flag =
                ((_joinDateTime[idx] != 0) ? 1 : 0) +
                ((_leftDateTime[idx] != 0) ? 2 : 0);
            string str = "";
            switch(flag)
            {
                case 1:
                    str = new DateTime(_joinDateTime[idx]).ToLocalTime().ToString(ShortDateTimeFormat) + " ～ ";
                    break;

                case 3:
                    str = new DateTime(_joinDateTime[idx]).ToLocalTime().ToString(ShortDateTimeFormat) + " ～ ";
                    str += new DateTime(_leftDateTime[idx]).ToLocalTime().ToString(ShortDateTimeFormat);
                    break;

                default:
                    str = "???";
                    break;
            }

            if (_joinCount[idx] > 1)
            {
                str += " (" + _joinCount[idx].ToString() + "回目)";
            }
            return JoinStatusTag.Replace("?", str);
        }

        private string GetMasterIconByStatus(byte status)
        {
            switch(status)
            {
                case 2:
                    return "☆";
                case 3:
                    return "★";
                default:
                    return "";
            }
        }

        private VRCPlayerApi GetPlayerByDisplayName(string displayName)
        {
            VRCPlayerApi[] players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
            players = VRCPlayerApi.GetPlayers(players);
            foreach (var player in players) if (player.displayName == displayName) return player;
            return null;
        }

        private void RegisterMyInformation(bool isMyCountUp)
        {
            if (Networking.IsOwner(gameObject))
            {
                // DebugLog(0, true, "RegisterMyInformation(Master) / persons = " + _displayNames.Length);

                // 自分がオーナーだったら
                int idx = GetPersonIndexByDisplayName(_localPlayer.displayName);
                if (idx < 0)
                {
                    // 配列にまだ自分がいなかったら配列の最後に自分を足す
                    int nowLength = _displayNames.Length;
                    int newLength = nowLength + 1;
                    byte[] newStatuses = new byte[newLength];
                    string[] newDisplayNames = new string[newLength];
                    ushort[] newEnvironments = new ushort[newLength];
                    long[] newJoinDateTime = new long[newLength];
                    long[] newLeftDateTime = new long[newLength];
                    byte[] newJoinCount = new byte[newLength];

                    Array.Copy(_statuses, newStatuses, nowLength);
                    Array.Copy(_displayNames, newDisplayNames, nowLength);
                    Array.Copy(_environments, newEnvironments, nowLength);
                    Array.Copy(_joinDateTime, newJoinDateTime, nowLength);
                    Array.Copy(_leftDateTime, newLeftDateTime, nowLength);
                    Array.Copy(_joinCount, newJoinCount, nowLength);

                    newStatuses[nowLength] = GetMyStatus();                 // ** nowLength = newLength - 1 (末尾)
                    newDisplayNames[nowLength] = _localPlayer.displayName;
                    newEnvironments[nowLength] = GetMyEnvironment();
                    newJoinDateTime[nowLength] = _myJoinDateTime;
                    newLeftDateTime[nowLength] = 0;
                    newJoinCount[nowLength] = 1;

                    _statuses = newStatuses;
                    _displayNames = newDisplayNames;
                    _environments = newEnvironments;
                    _joinDateTime = newJoinDateTime;
                    _leftDateTime = newLeftDateTime;
                    _joinCount = newJoinCount;

                    if (IsSortUserList) SortPersonsArray();
                }
                else
                {
                    // 配列に既に自分がいた場合は更新
                    if (_joinDateTime[idx] == _myJoinDateTime)
                    {
                        // 何かの間違いで重複実行
                        isMyCountUp = false;
                    }

                    _statuses[idx] = GetMyStatus();
                    _environments[idx] = GetMyEnvironment();
                    if (isMyCountUp) _joinCount[idx]++;
                    _joinDateTime[idx] = _myJoinDateTime;
                    _leftDateTime[idx] = 0;
                }

                RequestSerialization();
                UpdateDisplay();
                //_isMyInformationRegistered = true;

                DebugLog(0, true, "RegisterMyInformation(Master) Done / now persons = " + _displayNames.Length);
            }
            else
            {
                DebugLog(0, true, "RegisterMyInformation(Remote) / SetOwner");

                // 自分がオーナーでなかったら情報が登録できないのでオーナー権をくれと言う
                // →OnOwnershipRequestが動く
                Networking.SetOwner(_localPlayer, gameObject);
            }
        }

        private void SortPersonsArray()
        {
            for (int idx1 = 0; idx1 < _displayNames.Length; idx1++)
            {
                for (int idx2 = _displayNames.Length - 1; idx2 > idx1; idx2--)
                {
                    // idx2の方が偉かったら入れ替えることで先頭が一番偉くなる
                    bool isSwapNeeded = false;
                    if (_statuses[idx1] < _statuses[idx2])
                    {
                        isSwapNeeded = true;
                    }
                    else
                    {
                        if (_statuses[idx1] == _statuses[idx2])
                        {
                            if (_joinCount[idx1] < _joinCount[idx2])
                            {
                                isSwapNeeded = true;
                            }
                            else
                            {
                                if (_joinDateTime[idx1] > _joinDateTime[idx2])
                                {
                                    isSwapNeeded = true;
                                }
                            }
                        }
                    }

                    if (isSwapNeeded)
                    {
                        // 入れ替え
                        long tempLong;
                        byte tempByte;
                        ushort tempUShort;
                        string tempStr;
                        tempByte = _statuses[idx1];
                        _statuses[idx1] = _statuses[idx2];
                        _statuses[idx2] = tempByte;
                        tempStr = _displayNames[idx1];
                        _displayNames[idx1] = _displayNames[idx2];
                        _displayNames[idx2] = tempStr;
                        tempUShort = _environments[idx1];
                        _environments[idx1] = _environments[idx2];
                        _environments[idx2] = tempUShort;
                        tempLong = _joinDateTime[idx1];
                        _joinDateTime[idx1] = _joinDateTime[idx2];
                        _joinDateTime[idx2] = tempLong;
                        tempLong = _leftDateTime[idx1];
                        _leftDateTime[idx1] = _leftDateTime[idx2];
                        _leftDateTime[idx2] = tempLong;
                        tempByte = _joinCount[idx1];
                        _joinCount[idx1] = _joinCount[idx2];
                        _joinCount[idx2] = tempByte;
                    }
                }
            }
        }

        public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner)
        {
            DebugLog(0, true, "OnOwnershipRequest / requesting=" + GetPlayerString(requestingPlayer) + ", requested=" + GetPlayerString(requestedOwner));
            return true;    // 断る理由はないのでtrueを返すだけ
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            DebugLog(0, true, "OnOwnershipTransferred / " + GetPlayerString(player));
            
            if (!Utilities.IsValid(player)) return;

            // オーナー権が移譲されたら登録をやりなおす
            if (player.isLocal)
            {
                RegisterMyInformation(!_isMyInformationRegistered);
            }

            UpdateDisplay();    // とりあえず
        }

        private byte GetMyStatus()
        {
            if (_localPlayer.isInstanceOwner) return 3;
            else return 1;
        }

        private int GetPhotoIndexByDisplayName(string displayName)
        {
            for (int idx = 0; idx < _photoNames.Length; idx++) if (_photoNames[idx] == displayName) return idx;
            return -1;
        }
        private int GetPersonIndexByDisplayName(string displayName)
        {
            for (int idx = 0; idx < _displayNames.Length; idx++) if (_displayNames[idx] == displayName) return idx;
            return -1;
        }

        private string GetEnvironmentString(ushort environment)
        {
            bool isVR;
            ClassifiedInputMethod inputMethod;
            Platform platform;
            ExtractEnvironmentValue(environment, out isVR, out inputMethod, out platform);
            string isVRString = isVR ? "VR" : "nonVR";

            return PlatformToString(platform) + " / " + isVRString + "\n" + ClassifiedInputMethodToString(inputMethod);
        }

        private void ExtractEnvironmentValue(ushort environment, out bool isVR, out ClassifiedInputMethod inputMethod, out Platform platform)
        {
            isVR = ((environment & (ushort)0x8000) != 0);
            inputMethod = (ClassifiedInputMethod)((environment & (ushort)0x7f00) >> 8);
            platform = (Platform)(environment & (ushort)0xff);
        }

        private ushort GetMyEnvironment()
        {
            int vr = _localPlayer.IsUserInVR() ? 1 : 0;
            int inputMethod = (int)GetClassifiedInputMethod();
            int platform = (int)GetMyPlatform();
            ushort env = (ushort)((vr << 15) | (inputMethod << 8) | platform);
            DebugLog(0, true, "vr:" + vr + ", meth:" + inputMethod + ", platform:" + platform);
            return env;   // vmmmmmmpppppppp
        }

        private ClassifiedInputMethod GetClassifiedInputMethod()
        {
            switch((int)InputManager.GetLastUsedInputMethod())
            {
                case 0: return ClassifiedInputMethod.Desktop;
                case 1: return ClassifiedInputMethod.Desktop;
                case 2: return ClassifiedInputMethod.Gamepad;
                case 3: return ClassifiedInputMethod.SomethingVR;
                case 5: return ClassifiedInputMethod.Vive;
                case 6: return ClassifiedInputMethod.Oculus;
                case 7: return ClassifiedInputMethod.Oculus;
                case 10: return ClassifiedInputMethod.Index;
                case 11: return ClassifiedInputMethod.HPReverb;
                case 12: return ClassifiedInputMethod.OSC;
                case 13: return ClassifiedInputMethod.Oculus;
                case 14: return ClassifiedInputMethod.SomethingVR;
                case 15: return ClassifiedInputMethod.Touch;
                case 16: return ClassifiedInputMethod.OpenXR;
                case 17: return ClassifiedInputMethod.Pico;
                case 18: return ClassifiedInputMethod.SteamVR2;
                case 19: return ClassifiedInputMethod.Embodied;
                default: return ClassifiedInputMethod.SomethingVR;
            }
        }

        private string PlatformToString(Platform platform)
        {
            // なんて無駄な事を…
            switch(platform)
            {
                case Platform.PC: return "PC";
                case Platform.Android: return "Android";
                case Platform.Linux: return "Linux";
                case Platform.MacOSX: return "MacOSX";
                case Platform.iOS: return "iOS";
                case Platform.PS4: return "PS4";
                case Platform.XBOXOne: return "XBOXOne";
                case Platform.tvOS: return "tvOS";
                case Platform.WSA: return "WSA";
                case Platform.WebGL: return "WebGL";
                case Platform.Editor: return "Editor";
                default: return "Unknown";
            }
        }

        private string ClassifiedInputMethodToString(ClassifiedInputMethod meth)
        {
            // なんて無駄な事を…その2
            switch (meth)
            {
                case ClassifiedInputMethod.Desktop: return "Desktop";
                case ClassifiedInputMethod.Gamepad: return "Gamepad";
                case ClassifiedInputMethod.Vive: return "Vive";
                case ClassifiedInputMethod.Oculus: return "Oculus";
                case ClassifiedInputMethod.Index: return "Index";
                case ClassifiedInputMethod.HPReverb: return "HPReverb";
                case ClassifiedInputMethod.Touch: return "Touch";
                case ClassifiedInputMethod.OSC: return "OSC";
                case ClassifiedInputMethod.OpenXR: return "OpenXR";
                case ClassifiedInputMethod.Pico: return "Pico";
                case ClassifiedInputMethod.SteamVR2: return "SteamVR2";
                case ClassifiedInputMethod.Embodied: return "Embodied";
                default: return "SomethingVR";
            }
        }

        private Platform GetMyPlatform()
        {
#if UNITY_WEBGL
            return Platform.WebGL;
#elif UNITY_WSA
            return Platform.WSA;
#elif UNITY_TVOS
            return Platform.tvOS;
#elif UNITY_XBOXONE
            return Platform.XBOXOne;
#elif UNITY_PS4
            return Platform.PS4;
#elif UNITY_IOS
            return Platform.iOS;
#elif UNITY_STANDALONE_OSX
            return Platform.MacOSX;
#elif UNITY_STANDALONE_LINUX
            return Platform.Linux;
#elif UNITY_ANDROID
            return Platform.Android;
#elif UNITY_EDITOR
            return Platform.Editor;
#elif UNITY_STANDALONE_WIN
            return Platform.PC;
#else
            return Platform.Unknown;
#endif
        }

        private void DebugLog(int logLevel, bool isEditorOnly, string message)
        {
            isEditorOnly = false;
#if !UNITY_EDITOR
            if (isEditorOnly) return;
#endif
            string logstr = string.Format("[VisitorsInformationBoard] {0:0.00} / {1}", Time.time, message);
            switch (logLevel)
            {
                case 1: Debug.LogWarning(logstr, gameObject); break;
                case 2: Debug.LogError(logstr, gameObject); break;
                default: Debug.Log(logstr, gameObject); break;
            }
        }

        private string GetPlayerString(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player)) return "(invalid player)";
            else return player.displayName + (player.isLocal ? "(local)" : "");
        }

        public void OnChangePageToggle()
        {
            if (!_animator) return;
            _animator.SetBool("IsShowLog", _togglePage.isOn);
        }

        private void AddJoinLog(bool isJoin, string displayName)
        {
            InitializeArrayIfNeeded();

            int oldLength = _isJoinLogs.Length;
            int newLength = oldLength + 1;

            bool[] newIsJoinLogs = new bool[newLength];
            long[] newDateTimeLogs = new long[newLength];
            string[] newNameLogs = new string[newLength];

            Array.Copy(_isJoinLogs, 0, newIsJoinLogs, 1, oldLength);
            Array.Copy(_dateTimeLogs, 0, newDateTimeLogs, 1, oldLength);
            Array.Copy(_nameLogs, 0, newNameLogs, 1, oldLength);

            newIsJoinLogs[0] = isJoin;
            newDateTimeLogs[0] = DateTime.Now.Ticks;
            newNameLogs[0] = displayName;

            _isJoinLogs = newIsJoinLogs;
            _dateTimeLogs = newDateTimeLogs;
            _nameLogs = newNameLogs;
        }

        private void UpdateJoinLog()
        {
            InitializeArrayIfNeeded();

            string str = "";
            for (int idx = 0; idx < _isJoinLogs.Length; idx++)
            {
                str += new DateTime(_dateTimeLogs[idx]).ToString(LogDateTimeFormat);

                str += " ";

                if (_isJoinLogs[idx]) str += JoinString;
                else str += LeftString;

                str += " ";

                str += _nameLogs[idx];

                str += "\n";
            }

            _textJoinLogContent.text = str;

            RectTransform joinLogTran = (RectTransform)_textJoinLogContent.transform;
            Vector2 orgSize = joinLogTran.sizeDelta;
            orgSize.y = LineHeight * _isJoinLogs.Length;
            joinLogTran.sizeDelta = orgSize;
        }

        private void InitializeArrayIfNeeded()
        {
            if (_statuses == null)
            {
                _statuses = new byte[0];
                _displayNames = new string[0];
                _environments = new ushort[0];
                _joinDateTime = new long[0];
                _leftDateTime = new long[0];
                _joinCount = new byte[0];
            }

            if (_photos == null)
            {
                _photos = new RenderTexture[0];
                _photoNames = new string[0];
            }

            if (_photoShotReservedNames == null)
            {

                _photoShotReservedTimes = new float[0];
                _photoShotReservedNames = new string[0];
            }

            if (_isJoinLogs == null)
            {
                _isJoinLogs = new bool[0];
                _dateTimeLogs = new long[0];
                _nameLogs = new string[0];
            }
        }

        public override void OnPostSerialization(SerializationResult result)
        {
            if (result.success) _isMyInformationRegistered = true;
        }
    }
}