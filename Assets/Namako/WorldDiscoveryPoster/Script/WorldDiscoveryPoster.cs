
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.StringLoading;
using VRC.SDK3.Image;
using VRC.Udon.Common.Interfaces;
using UnityEngine.UI;
using VRC.SDK3.Data;
using System;

namespace Namako
{
    public enum Platform
    {
        CrossPlatform,
        PC,
        Quest
    }
    public enum Mode
    {
        Light,
        Dark
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class WorldDiscoveryPoster : UdonSharpBehaviour
    {
        const int REFLESH_DURATION_SECONDS = 1800;
        const int ROW_NUM = 3;
        const int COLUMN_NUM = 2;
        private string[] VERSION = new string[2]{"v1.3", "v1.1"};

        [Header("Property")]
        [SerializeField] private Platform m_targetPlatform = Platform.CrossPlatform;
        [SerializeField] private Mode m_Mode = Mode.Light;

        [Header("Reference")]
        [SerializeField] private RawImage m_rawImage;
        [SerializeField] private Text m_infoText;
        [SerializeField] private VRC_PortalMarker m_portalMarker;
        [SerializeField] private Button m_togglePortalButton;

#region URLs
        private VRCUrl[] m_imageURLs;
        private VRCUrl[] m_CrossPlatformURLs = new VRCUrl[] {
            new VRCUrl("https://namanonamako.github.io/world-discover-poster/PC_0.jpg"),
            new VRCUrl("https://namanonamako.github.io/world-discover-poster/Quest_0.jpg"),
            new VRCUrl("https://namanonamako.github.io/world-discover-poster/PC_1.jpg"),
            new VRCUrl("https://namanonamako.github.io/world-discover-poster/Quest_1.jpg"),
            new VRCUrl("https://namanonamako.github.io/world-discover-poster/PC_2.jpg"),
            new VRCUrl("https://namanonamako.github.io/world-discover-poster/Quest_2.jpg")
        };

        private VRCUrl[] m_PCURLs = new VRCUrl[] {
            new VRCUrl("https://namanonamako.github.io/world-discover-poster/PC_0.jpg"),
            new VRCUrl("https://namanonamako.github.io/world-discover-poster/PC_1.jpg"),
            new VRCUrl("https://namanonamako.github.io/world-discover-poster/PC_2.jpg"),
            new VRCUrl("https://namanonamako.github.io/world-discover-poster/PC_3.jpg"),
            new VRCUrl("https://namanonamako.github.io/world-discover-poster/PC_4.jpg"),
            new VRCUrl("https://namanonamako.github.io/world-discover-poster/PC_5.jpg")
        };

        private VRCUrl[] m_QuestURLs = new VRCUrl[] {
            new VRCUrl("https://namanonamako.github.io/world-discover-poster/Quest_0.jpg"),
            new VRCUrl("https://namanonamako.github.io/world-discover-poster/Quest_1.jpg"),
            new VRCUrl("https://namanonamako.github.io/world-discover-poster/Quest_2.jpg"),
            new VRCUrl("https://namanonamako.github.io/world-discover-poster/Quest_3.jpg"),
            new VRCUrl("https://namanonamako.github.io/world-discover-poster/Quest_4.jpg"),
            new VRCUrl("https://namanonamako.github.io/world-discover-poster/Quest_5.jpg")
        };
        private VRCUrl[] m_CrossPlatformURLs_d = new VRCUrl[] {
            new VRCUrl("https://namanonamako.github.io/world-discover-poster/PC_d_0.jpg"),
            new VRCUrl("https://namanonamako.github.io/world-discover-poster/Quest_d_0.jpg"),
            new VRCUrl("https://namanonamako.github.io/world-discover-poster/PC_d_1.jpg"),
            new VRCUrl("https://namanonamako.github.io/world-discover-poster/Quest_d_1.jpg"),
            new VRCUrl("https://namanonamako.github.io/world-discover-poster/PC_d_2.jpg"),
            new VRCUrl("https://namanonamako.github.io/world-discover-poster/Quest_d_2.jpg")
        };

        private VRCUrl[] m_PCURLs_d = new VRCUrl[] {
            new VRCUrl("https://namanonamako.github.io/world-discover-poster/PC_d_0.jpg"),
            new VRCUrl("https://namanonamako.github.io/world-discover-poster/PC_d_1.jpg"),
            new VRCUrl("https://namanonamako.github.io/world-discover-poster/PC_d_2.jpg"),
            new VRCUrl("https://namanonamako.github.io/world-discover-poster/PC_d_3.jpg"),
            new VRCUrl("https://namanonamako.github.io/world-discover-poster/PC_d_4.jpg"),
            new VRCUrl("https://namanonamako.github.io/world-discover-poster/PC_d_5.jpg")
        };

        private VRCUrl[] m_QuestURLs_d = new VRCUrl[] {
            new VRCUrl("https://namanonamako.github.io/world-discover-poster/Quest_d_0.jpg"),
            new VRCUrl("https://namanonamako.github.io/world-discover-poster/Quest_d_1.jpg"),
            new VRCUrl("https://namanonamako.github.io/world-discover-poster/Quest_d_2.jpg"),
            new VRCUrl("https://namanonamako.github.io/world-discover-poster/Quest_d_3.jpg"),
            new VRCUrl("https://namanonamako.github.io/world-discover-poster/Quest_d_4.jpg"),
            new VRCUrl("https://namanonamako.github.io/world-discover-poster/Quest_d_5.jpg")
        };

        private VRCUrl m_posterInfoURL = new VRCUrl("https://namanonamako.github.io/world-discover-poster/posterInfo.json");
#endregion

        private VRCPlayerApi _localplayer;
        private int m_loadedIndex = 0;
        private Texture2D[] m_downloadedTextures;
        private VRCImageDownloader m_imageDownloader;
        private IUdonEventReceiver m_udonEventReceiver;
        private DataList m_worldIds = new DataList();
        private string m_worldId = "";
        private DateTime m_nextUpdateTime;

        [UdonSynced, FieldChangeCallback(nameof(posterIndex))] private int m_posterIndex = 0;
        [UdonSynced, FieldChangeCallback(nameof(isPortalEnabled))] private bool m_isPortalEnabled = false;

        public int posterIndex
        {
            get => m_posterIndex;
            set
            {
                int pagecount = m_imageURLs.Length * ROW_NUM * COLUMN_NUM;
                m_posterIndex = (value + pagecount) % (pagecount);
                UpdatePosterView();

                if (!Networking.IsOwner(_localplayer, gameObject)) return;
                isPortalEnabled = false;
                RequestSerialization();
            }
        }

        public bool isPortalEnabled
        {
            get => m_isPortalEnabled;
            set
            {
                m_isPortalEnabled = value;
                m_portalMarker.gameObject.SetActive(m_isPortalEnabled);
            }
        }

        private void SetWorldIds(DataDictionary _json)
        {
            if (!_json.TryGetValue("PCWorldID", out DataToken PCWorlds))
            {
                m_infoText.text = "Error";
                return;
            }

            if (!_json.TryGetValue("QuestWorldID", out DataToken questWorlds))
            {
                m_infoText.text = "Error";
                return;
            }

            switch (m_targetPlatform)
            {
                case Platform.CrossPlatform:
                    m_worldIds.AddRange(PCWorlds.DataList.GetRange(0, 6));
                    m_worldIds.AddRange(questWorlds.DataList.GetRange(0, 6));
                    m_worldIds.AddRange(PCWorlds.DataList.GetRange(6, 6));
                    m_worldIds.AddRange(questWorlds.DataList.GetRange(6, 6));
                    m_worldIds.AddRange(PCWorlds.DataList.GetRange(12, 6));
                    m_worldIds.AddRange(questWorlds.DataList.GetRange(12, 6));
                    break;
                case Platform.PC:
                    m_worldIds = PCWorlds.DataList;
                    break;
                case Platform.Quest:
                    m_worldIds = questWorlds.DataList;
                    break;
            }
        }

        private void UpdatePosterView()
        {
            m_rawImage.uvRect = new Rect(1f / ROW_NUM * m_posterIndex % ROW_NUM, 0.5f + 1f / COLUMN_NUM * Mathf.FloorToInt(m_posterIndex / ROW_NUM), 1f / ROW_NUM, 1f / COLUMN_NUM);
            m_rawImage.texture = m_downloadedTextures[Mathf.FloorToInt(m_posterIndex / (ROW_NUM * COLUMN_NUM))];
            if (m_worldIds.Count == 0) return;
            if(m_worldIds.TryGetValue(m_posterIndex, out DataToken id))
            {
                m_worldId = id.String;
                m_portalMarker.roomId = m_worldId;
                m_togglePortalButton.interactable = (m_worldId != "");
            }
            else
            {
                Debug.Log($"【WorldDiscoveryPoster】WorldID取得に失敗 index:{m_posterIndex} List:{m_worldIds}");
            }
        }

        void Start()
        {
            m_portalMarker.gameObject.SetActive(isPortalEnabled);
            _localplayer = Networking.LocalPlayer;
            switch (m_targetPlatform)
            {
                case Platform.CrossPlatform:
                    m_imageURLs = m_Mode == Mode.Dark ? m_CrossPlatformURLs_d : m_CrossPlatformURLs;
                    break;
                case Platform.PC:
                    m_imageURLs = m_Mode == Mode.Dark ? m_PCURLs_d : m_PCURLs;
                    break;
                case Platform.Quest:
                    m_imageURLs = m_Mode == Mode.Dark ? m_QuestURLs_d : m_QuestURLs;
                    break;
            }
            m_downloadedTextures = new Texture2D[m_imageURLs.Length];
            m_imageDownloader = new VRCImageDownloader();
            m_udonEventReceiver = (IUdonEventReceiver)this;

            m_rawImage.uvRect = new Rect(1f / ROW_NUM, 0.5f + 1f / COLUMN_NUM, 0, 0);

            StartLoading();
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (DateTime.Now < m_nextUpdateTime) return;
            StartLoading();
        }

        private void StartLoading()
        {
            if(DateTime.Now < m_nextUpdateTime)
            {
                SendCustomEventDelayedSeconds(nameof(StartLoading), 180);
                return;
            }
            m_imageDownloader.Dispose();
            m_loadedIndex = 0;
            m_worldIds.Clear();
            LoadNextImage();
            VRCStringDownloader.LoadUrl(m_posterInfoURL, this.GetComponent<UdonBehaviour>());
            m_nextUpdateTime = GetNextUpdateTime(DateTime.Now);
        }

        private void LoadNextImage()
        {
            var rgbInfo = new TextureInfo();
            rgbInfo.GenerateMipMaps = true;
            m_imageDownloader.DownloadImage(m_imageURLs[m_loadedIndex], null, m_udonEventReceiver, rgbInfo);
        }

        public override void OnImageLoadSuccess(IVRCImageDownload result)
        {
            Debug.Log($"【WorldDiscoveryPoster】Image loaded: {result.SizeInMemoryBytes} bytes.");

            m_downloadedTextures[m_loadedIndex] = result.Result;
            UpdatePosterView();

            m_loadedIndex += 1;
            if (m_loadedIndex < m_imageURLs.Length)
            {
                LoadNextImage();
            }
            else
            {
                SendCustomEventDelayedSeconds(nameof(StartLoading), REFLESH_DURATION_SECONDS);
            }
        }

        public override void OnImageLoadError(IVRCImageDownload result)
        {
            Debug.Log($"【WorldDiscoveryPoster】Image not loaded: {result.Error.ToString()}: {result.ErrorMessage}.");
        }

        public override void OnStringLoadSuccess(IVRCStringDownload result)
        {
            base.OnStringLoadSuccess(result);

            if(VRCJson.TryDeserializeFromJson(result.Result, out DataToken jsonData))
            {
                if(jsonData.TokenType != TokenType.DataDictionary)
                {
                    m_infoText.text = "Error";
                    return;
                }

                if(!jsonData.DataDictionary.TryGetValue("ver", out DataToken ver))
                {
                    m_infoText.text = "Error";
                    return;
                }
                if (!Contain(VERSION,ver.String))
                {
                    m_infoText.text = "新しいバージョンが出ています。\nBoothからDLして更新をお願いします。";
                }
                else
                {
                    if (!jsonData.DataDictionary.TryGetValue("message", out DataToken message))
                    {
                        m_infoText.text = "Error";
                        return;
                    }
                    m_infoText.text = message.String;
                }

                SetWorldIds(jsonData.DataDictionary);
            }
            else
            {
                Debug.Log($"【WorldDiscoveryPoster】Failed to Deserialize json {result.Result} - {jsonData.ToString()}");
            }
        }

        public override void OnStringLoadError(IVRCStringDownload result)
        {
            m_infoText.text = "Error";
            Debug.Log($"【WorldDiscoveryPoster】{result.Error}");
        }

        private void OnDestroy()
        {
            m_imageDownloader.Dispose();
        }

        private bool Contain(string[] arr, string str)
        {
            foreach(string elm in arr)
            {
                if (elm == str) return true;
            }
            return false;
        }

        private DateTime GetNextUpdateTime(DateTime currentTime)
        {
            int minutes = currentTime.Minute;
            int roundedMinutes = (int)(Math.Round((double)minutes / 30) * 30) % 60 + 3;
            int hourAdjustment = (int)Math.Floor((double)minutes / 30);

            DateTime roundedTime = new DateTime(
                currentTime.Year,
                currentTime.Month,
                currentTime.Day,
                currentTime.Hour + hourAdjustment,
                roundedMinutes,
                30);

            return roundedTime;

        }

        #region Public Functions
        public void OpenNextPage()
        {
            SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(OpenNextPageOwner));
        }
        public void OpenPrevPage()
        {
            SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(OpenPrevPageOwner));
        }

        public void TogglePortal()
        {
            SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(TogglePortalOwner));
        }

        public void Reload()
        {
            m_nextUpdateTime = new DateTime(0);
            StartLoading();
        }

        public void OpenNextPageOwner()
        {
            posterIndex += 1;
            isPortalEnabled = false;
            RequestSerialization();
        }
        public void OpenPrevPageOwner()
        {
            posterIndex -= 1;
            isPortalEnabled = false;
            RequestSerialization();
        }

        public void TogglePortalOwner()
        {
            isPortalEnabled = !isPortalEnabled && (m_worldId != "");
            RequestSerialization();
        }

        public void ReloadImages()
        {
            StartLoading();
        }
        #endregion
    }
}