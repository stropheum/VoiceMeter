// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Linq;
// using System.Net;
// using System.Net.Http;
// using System.Security.Cryptography;
// using System.Text;
// using System.Threading;
// using System.Threading.Tasks;
// using Discord.Sdk;
// using Newtonsoft.Json;
// using UnityEngine;
// using UnityEngine.Serialization;
// using UnityEngine.UIElements;
// using VoiceMeter.Discord;
//
// #if UNITY_ANDROID
// using UnityEngine.Android;
// #endif
//
// // Friends list filters approximating the Discord UI definitions, e.g. "All" is
// // all of your actual friends, not including pending/blocked.
// public enum FriendsListFilter
// {
//     Online,
//     All,
//     Pending,
//     Blocked
// }
//
// public enum HomeListEntryType
// {
//     FriendsList,
//     Header,
//     Lobby,
//     JoinRequest,
//     JoinInvite,
//     UnityLogs,
//     DiscordLogs,
//     DiscordConnectedUserInfo
// }
//
// public class HomeListEntry
// {
//     public HomeListEntryType Type { get; set; }
//     public ulong Id { get; set; }
//     public string Name { get; set; }
//     public IDisposable Handle { get; set; }
//     public Action AddAction { get; set; }
//
//     public bool IsSame(HomeListEntry other) => other != null && Type == other.Type && Id == other.Id;
// }
//
// public class Friend : IDisposable
// {
//     public RelationshipHandle Relationship { get; }
//     public UserHandle User { get; }
//
//     public bool MatchesFilter(FriendsListFilter filter)
//     {
//         RelationshipType relType = Relationship.DiscordRelationshipType();
//         switch (filter)
//         {
//             case FriendsListFilter.Online:
//                 return relType == RelationshipType.Friend &&
//                        User.Status()
//                            is StatusType.Online or StatusType.Idle or StatusType.Dnd or StatusType.Streaming;
//             case FriendsListFilter.All:
//                 return relType == RelationshipType.Friend;
//             case FriendsListFilter.Pending:
//                 return relType is RelationshipType.PendingIncoming or RelationshipType.PendingOutgoing;
//             case FriendsListFilter.Blocked:
//                 return relType == RelationshipType.Blocked;
//             default:
//                 return false;
//         }
//     }
//
//     public Friend(RelationshipHandle rel)
//     {
//         Relationship = rel;
//         User = rel.User();
//     }
//
//     public void Dispose()
//     {
//         Relationship.Dispose();
//         User?.Dispose();
//     }
// }
//
// public class Message : IDisposable
// {
//     public MessageHandle Handle { get; }
//     public Guid? LocalId { get; set; }
//     public ulong? Id { get; set; }
//     public string Content { get; set; }
//     public string SenderName { get; set; }
//
//     public Message(Client client, MessageHandle handle)
//     {
//         Handle = handle;
//         UpdateFromHandle(client);
//     }
//
//     public Message(string senderName, string content)
//     {
//         LocalId = Guid.NewGuid();
//         Content = content;
//         SenderName = senderName;
//     }
//
//     public void Dispose()
//     {
//         Handle?.Dispose();
//     }
//
//     public void UpdateFromHandle(Client client)
//     {
//         if (Handle == null)
//         {
//             return;
//         }
//
//         Id = Handle.Id();
//         Content = Handle.Content();
//         using UserHandle user = client.GetUser(Handle.AuthorId());
//         if (user != null)
//         {
//             SenderName = user.Username();
//         }
//     }
// }
//
// [Serializable]
// public class SampleViewSettings
// {
//     [FormerlySerializedAs("token")] public string Token;
//     public Dictionary<ulong, string> LobbySecrets;
//     public string SelectedOutputDevice = "default";
//     public string SelectedInputDevice = "default";
//     public float OutputVolume = 100.0f;
//     public float InputVolume = 100.0f;
//     public bool PttEnabled = false;
// }
//
// public enum LogCategory
// {
//     DiscordPartnerSdk,
//     Unity
// }
//
// public class LogMessage
// {
//     public LogType Type { get; set; }
//     public string Text { get; set; }
//     public string StackTrace { get; set; }
// }
//
// public class SampleView : MonoBehaviour
// {
//     [FormerlySerializedAs("config")] public DiscordConfig Config;
//
//     // Templates
//     [FormerlySerializedAs("friendsListTemplate")] [SerializeField]
//     private VisualTreeAsset _friendsListTemplate;
//
//     [FormerlySerializedAs("friendsListEntryTemplate")] [SerializeField]
//     private VisualTreeAsset _friendsListEntryTemplate;
//
//     [FormerlySerializedAs("lobbyViewTemplate")] [SerializeField]
//     private VisualTreeAsset _lobbyViewTemplate;
//
//     [FormerlySerializedAs("inviteFriendModalTemplate")] [SerializeField]
//     private VisualTreeAsset _inviteFriendModalTemplate;
//
//     [FormerlySerializedAs("joinLobbyTemplate")] [SerializeField]
//     private VisualTreeAsset _joinLobbyTemplate;
//
//     [FormerlySerializedAs("voiceSettingsTemplate")] [SerializeField]
//     private VisualTreeAsset _voiceSettingsTemplate;
//
//     // The thing.
//     private Client _client;
//     private Call _activeCall;
//
//     // OAuth2 state
//     private string _codeVerifier;
//     private string _oauth2State;
//
//     // UI elements: root view
//     private VisualElement _root;
//     private Label _currentStatus;
//     private Button _connectButton;
//     private Button _getTokenButton;
//     private Button _disconnectButton;
//     private Button _annoyModeButton;
//     private TextField _token;
//     private ScrollView _homeListView;
//     private VisualElement _contentWell;
//     private VisualElement _modal;
//
//     // UI elements: voice control cluster
//     private VisualElement _voiceControls;
//     private VisualElement _voiceChannelControls;
//     private Label _voiceStatusText;
//     private Label _voiceChannelText;
//     private Label _voiceUsernameText;
//     private Button _pttActiveButton;
//     private Button _voiceDisconnectButton;
//     private Button _muteButton;
//     private Button _deafenButton;
//     private Button _settingsButton;
//
//     // UI state
//     private SampleViewSettings _settings;
//     private List<HomeListEntry> _homeListEntries = new();
//     private HomeListEntry _selectedHomeListEntry;
//     private FriendsListFilter _friendsListFilter;
//     private Dictionary<ulong, List<Message>> _messages = new();
//     private Dictionary<Guid, Message> _pendingMessages = new();
//     private Dictionary<ulong, string> _lobbySecrets = new();
//     private List<ActivityInvite> _activityInvites = new();
//
//     private Dictionary<LogCategory, List<LogMessage>> _logs = new();
//
//     // Retained friend handles for friend list content well and invite views
//     private DisposableArray<Friend>? _friendsList;
//     private DisposableArray<Friend>? _invitedFriendsList;
//     private bool _messageListPendingScrollDown;
//     private bool _annoyModeEnabled;
//     private IDisposable _modalController;
//     private ImageLoader _imageLoader = new();
//     private UserHandle _discordClientConnectedUser;
//
//     private void OpenModal(VisualElement content, IDisposable controller = null)
//     {
//         CloseModal();
//         _modalController = controller;
//         var modal = new VisualElement();
//         var wrapper = new VisualElement();
//         var background = new VisualElement();
//         background.AddToClassList("modal-background");
//         modal.AddToClassList("modal");
//         modal.RegisterCallback<PointerDownEvent>((evt) =>
//         {
//             if (evt.target == background)
//             {
//                 evt.StopImmediatePropagation();
//                 CloseModal();
//             }
//         });
//         wrapper.AddToClassList("modal-content");
//         wrapper.Add(content);
//         modal.Add(background);
//         modal.Add(wrapper);
//         _modal = modal;
//         var uiDocument = GetComponent<UIDocument>();
//         uiDocument.rootVisualElement.Add(_modal);
//     }
//
//     private void CloseModal()
//     {
//         _modalController?.Dispose();
//         _modalController = null;
//         _modal?.RemoveFromHierarchy();
//         _modal = null;
//     }
//
//     private void Update()
//     {
//         var audioSource = GetComponent<AudioSource>();
//         if (audioSource == null)
//         {
//             return;
//         }
//
//         if (_annoyModeEnabled)
//         {
//             if (!audioSource.isPlaying)
//             {
//                 audioSource.Play();
//             }
//         }
//         else
//         {
//             audioSource.Stop();
//         }
//     }
//
// #region Initialization
//
//     private IEnumerator Start()
//     {
//         yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
// #if UNITY_ANDROID
//         if (!Permission.HasUserAuthorizedPermission(Permission.Microphone)) {
//             Permission.RequestUserPermission(Permission.Microphone);
//         }
// #endif
//     }
//
//     private void OnEnable()
//     {
//         var uiDocument = GetComponent<UIDocument>();
//         VisualElement root = uiDocument.rootVisualElement;
//         _root = root;
//         _root.RegisterCallback<GeometryChangedEvent>((evt) => { UpdateSafeArea(); });
//         _currentStatus = root.Q<Label>("currentStatus");
//         _connectButton = root.Q<Button>("connectButton");
//         _getTokenButton = root.Q<Button>("getTokenButton");
//         _disconnectButton = root.Q<Button>("disconnectButton");
//         _annoyModeButton = root.Q<Button>("annoyModeButton");
//         _homeListView = root.Q<ScrollView>("homeList");
//         _token = root.Q<TextField>("token");
//         _contentWell = root.Q<VisualElement>("contentWell");
//         _voiceControls = root.Q<VisualElement>("voiceControls");
//         _voiceChannelControls = _voiceControls.Q<VisualElement>("channelControls");
//         _pttActiveButton = _voiceControls.Q<Button>("pttActiveButton");
//         _voiceStatusText = _voiceControls.Q<Label>("status");
//         _voiceUsernameText = _voiceControls.Q<Label>("username");
//         _voiceChannelText = _voiceControls.Q<Label>("channelName");
//         _voiceDisconnectButton = _voiceControls.Q<Button>("disconnectButton");
//         _muteButton = _voiceControls.Q<Button>("muteButton");
//         _deafenButton = _voiceControls.Q<Button>("deafenButton");
//         _settingsButton = _voiceControls.Q<Button>("settingsButton");
//         _connectButton.clicked += OnConnectClicked;
//         _disconnectButton.clicked += OnDisconnectClicked;
//         _getTokenButton.clicked += OnGetTokenClicked;
//         _voiceDisconnectButton.clicked += OnVoiceDisconnectClicked;
//         _muteButton.clicked += OnMuteClicked;
//         _deafenButton.clicked += OnDeafenClicked;
//         _settingsButton.clicked += OnSettingsClicked;
//         _annoyModeButton.clicked += OnAnnoyModeClicked;
// #if !UNITY_ANDROID && !UNITY_IOS
//         _annoyModeButton.style.display = DisplayStyle.None;
// #endif
//         _pttActiveButton.RegisterCallback<PointerUpEvent>(OnPttActiveUp, TrickleDown.TrickleDown);
//         _pttActiveButton.RegisterCallback<PointerDownEvent>(OnPttActiveDown,
//             TrickleDown.TrickleDown);
//         Application.logMessageReceived += OnUnityLogMessageReceived;
//         ResetClient();
//         LoadSettings();
//         RenderHomeList();
//     }
//
//     private void UpdateSafeArea()
//     {
//         float scale = GetComponent<UIDocument>().panelSettings.scale;
//         Rect safeArea = Screen.safeArea;
//         _root.style.paddingTop = safeArea.y / scale;
//         _root.style.paddingBottom = (Screen.height - safeArea.yMax) / scale;
//         _root.style.paddingLeft = safeArea.x / scale;
//         _root.style.paddingRight = (Screen.width - safeArea.xMax) / scale;
//     }
//
//     private void OnDisable()
//     {
//         Application.logMessageReceived -= OnUnityLogMessageReceived;
//         _client.Disconnect();
//     }
//
//     private void OnDestroy()
//     {
//         _client?.Dispose();
//         _client = null;
//         _imageLoader.Dispose();
//     }
//
//     private void OnUnityLogMessageReceived(string condition, string stackTrace, LogType type)
//     {
//         _logs.Activate(LogCategory.Unity, () => new List<LogMessage>())
//             .Add(new LogMessage { Type = type, Text = condition, StackTrace = stackTrace });
//     }
//
//     private void LoadSettings()
//     {
//         SampleViewSettings settings = PlayerPrefs.HasKey("SampleView")
//             ? JsonConvert.DeserializeObject<SampleViewSettings>(PlayerPrefs.GetString("SampleView"))
//             : null;
//         if (settings == null)
//         {
//             settings = new SampleViewSettings();
//         }
//
//         _settings = settings;
//         _token.value = settings.Token ?? "";
//         _lobbySecrets = settings.LobbySecrets ?? new Dictionary<ulong, string>();
//         _client.SetOutputDevice(settings.SelectedOutputDevice, (result) => { });
//         _client.SetInputDevice(settings.SelectedInputDevice, (result) => { });
//     }
//
//     private void SaveSettings()
//     {
//         _settings.Token = _token.value;
//         _settings.LobbySecrets = _lobbySecrets;
//         Debug.Log($"Saving settings with output device {_settings.SelectedOutputDevice} " +
//                   $"and input device {_settings.SelectedInputDevice}");
//         PlayerPrefs.SetString("SampleView", JsonConvert.SerializeObject(_settings));
//         PlayerPrefs.Save();
//     }
//
//     private void ResetClient()
//     {
//         if (_client != null)
//         {
//             _client.Disconnect();
//         }
//         else
//         {
//             var options = new ClientCreateOptions();
//             // some fun voice options you can try.
//             // options.SetExperimentalAudioSystem(AudioSystem.Game);
//             // options.SetExperimentalAndroidPreventCommsForBluetooth(true);
//             _client = new Client(options);
//         }
//
//         _client.UpdateToken(AuthorizationTokenType.Bearer, _token.text, (result) => { });
//         _client.SetEchoCancellation(true);
//         _client.SetAutomaticGainControl(true);
//         _client.SetNoiseSuppression(true);
//         _client.SetStatusChangedCallback(OnStatusChanged);
//         _client.SetRelationshipCreatedCallback(OnRelationshipChanged);
//         _client.SetRelationshipDeletedCallback(OnRelationshipChanged);
//         _client.SetUserUpdatedCallback(OnUserChanged);
//         _client.SetLobbyCreatedCallback(OnLobbyCreated);
//         _client.SetLobbyDeletedCallback(OnLobbyDeleted);
//         _client.SetLobbyMemberAddedCallback(OnLobbyMemberChanged);
//         _client.SetLobbyMemberUpdatedCallback(OnLobbyMemberChanged);
//         _client.SetLobbyMemberRemovedCallback(OnLobbyMemberChanged);
//         _client.SetActivityInviteCreatedCallback(OnActivityInvite);
//         _client.SetMessageCreatedCallback(OnMessageCreated);
//         _client.SetMessageUpdatedCallback(OnMessageUpdated);
//         _client.AddLogCallback(OnDiscordLogMessageReceived, LoggingSeverity.Verbose);
//         FetchDiscordClientConnectedUser();
//         RenderStatus();
//     }
//
// #endregion
//
// #region Client events
//
//     private void OnStatusChanged(Client.Status status, Client.Error error, int errorDetail)
//     {
//         Debug.Log($"[Discord] SetStatusChangedCallback {status} {error} {errorDetail}");
//         if (_client == null)
//         {
//             return;
//         }
//
//         switch (status)
//         {
//             case Client.Status.Ready:
//                 OnReady();
//                 break;
//             case Client.Status.Disconnected:
//                 OnDisconnected();
//                 break;
//         }
//
//         RenderStatus();
//     }
//
//     private void OnReady()
//     {
//         SaveSettings();
//         SetRichPresence(null);
//         RenderHomeList();
//         RenderContentWell();
//     }
//
//     private void OnDisconnected()
//     {
//         RenderHomeList();
//         RenderContentWell();
//     }
//
//     private void OnRelationshipChanged(ulong userId, bool isDiscordRelationship)
//     {
//         if (_client == null)
//         {
//             return;
//         }
//
//         RenderFriendsList();
//     }
//
//     private void OnUserChanged(ulong userId)
//     {
//         if (_client == null)
//         {
//             return;
//         }
//
//         RenderFriendsList();
//         RenderStatus();
//     }
//
//     private void OnPresenceChanged(ulong userId)
//     {
//         if (_client == null)
//         {
//             return;
//         }
//
//         RenderFriendsList();
//     }
//
//     private void OnLobbyCreated(ulong lobbyId)
//     {
//         if (_client == null)
//         {
//             return;
//         }
//
//         RenderHomeList();
//     }
//
//     private void OnLobbyDeleted(ulong lobbyId)
//     {
//         if (_client == null)
//         {
//             return;
//         }
//
//         RenderHomeList();
//         if (lobbyId == _selectedHomeListEntry?.Id &&
//             _selectedHomeListEntry.Type == HomeListEntryType.Lobby)
//         {
//             SetSelectedHomeListEntry(null);
//         }
//     }
//
//     private void OnLobbyMemberChanged(ulong lobbyId, ulong userId)
//     {
//         if (_client == null)
//         {
//             return;
//         }
//
//         Debug.Log($"Lobby member changed: {lobbyId} {userId}");
//         RenderMemberList();
//     }
//
//     private void OnLobbyJoined(ulong lobbyId, string secret, ClientResult result)
//     {
//         if (_client == null)
//         {
//             return;
//         }
//
//         Debug.Log($"Lobby join result: {lobbyId} {secret} {result.Status()} {result.Error()}");
//         _lobbySecrets[lobbyId] = secret;
//         SaveSettings();
//         RenderHomeList();
//         SetSelectedHomeListEntry(_homeListEntries.FirstOrDefault(e => e.Id == lobbyId && e.Type == HomeListEntryType.Lobby));
//     }
//
//     private void OnActivityInvite(ActivityInvite invite)
//     {
//         if (_client == null)
//         {
//             return;
//         }
//
//         Debug.Log($"Activity invite: {invite.SenderId()} {invite.Type()}");
//         _activityInvites.Add(invite);
//         RenderHomeList();
//     }
//
//     private void OnActivityInviteAccepted(ClientResult result, string joinSecret)
//     {
//         if (_client == null)
//         {
//             return;
//         }
//
//         Debug.Log(
//             $"Activity invite accept result: [{joinSecret}] [{result.Status()}] [{result.Error()}]");
//         if (result.Status() == Discord.Sdk.HttpStatusCode.Ok)
//         {
//             CreateOrJoinLobby(joinSecret);
//         }
//
//         result.Dispose();
//     }
//
//     private void OnMessageCreated(ulong messageId)
//     {
//         if (_client == null)
//         {
//             return;
//         }
//
//         Debug.Log($"Message created: {messageId}");
//         using MessageHandle handle = _client.GetMessageHandle(messageId);
//         if (handle == null)
//         {
//             return;
//         }
//
//         ulong channelId = handle.ChannelId();
//         var message = new Message(_client, handle);
//         var messages = _messages.Activate(channelId, () => new List<Message>());
//         messages.Add(message);
//         if (_selectedHomeListEntry?.Type == HomeListEntryType.Lobby &&
//             _selectedHomeListEntry.Id == channelId)
//         {
//             var messageList = _contentWell.Q<ScrollView>("messageList");
//             RenderMessage(messageList, message, true);
//         }
//     }
//
//     private void OnMessageSent(ClientResult result, ulong messageId)
//     {
//         if (_client == null)
//         {
//             return;
//         }
//
//         Debug.Log($"Message sent: [{messageId}] [{result.Status()}] [{result.Error()}]");
//         result.Dispose();
//     }
//
//     private void OnMessageUpdated(ulong messageId)
//     {
//         if (_client == null)
//         {
//             return;
//         }
//
//         Debug.Log($"Message updated: {messageId}");
//     }
//
//     private void OnDiscordLogMessageReceived(string message, LoggingSeverity severity)
//     {
//         if (_client == null)
//         {
//             return;
//         }
//
//         LogType type = severity switch
//         {
//             LoggingSeverity.Warning => LogType.Warning,
//             LoggingSeverity.Error => LogType.Error,
//             _ => LogType.Log
//         };
//         _logs.Activate(LogCategory.DiscordPartnerSdk, () => new List<LogMessage>())
//             .Add(new LogMessage { Type = type, Text = message });
//     }
//
// #endregion
//
// #region Status bar / voice controls
//
//     private void RenderStatus()
//     {
//         Client.Status status = _client.GetStatus();
//         if (status == Client.Status.Ready)
//         {
//             using UserHandle user = _client.GetCurrentUserV2();
//             if (user != null)
//             {
//                 string name = user.Username();
//                 _currentStatus.text = $"Ready ({name})";
//             }
//             else
//             {
//                 _currentStatus.text = "Ready (No user info)";
//             }
//
//             _voiceUsernameText.text = name;
//         }
//         else
//         {
//             _currentStatus.text = _client.GetStatus().ToString();
//         }
//
//         if (_activeCall == null)
//         {
//             _voiceChannelControls.style.display = DisplayStyle.None;
//             _pttActiveButton.style.display = DisplayStyle.None;
//             return;
//         }
//
//         _muteButton.EnableInClassList("voice-control-active", _activeCall.GetSelfMute());
//         _deafenButton.EnableInClassList("voice-control-active", _activeCall.GetSelfDeaf());
//         _pttActiveButton.style.display =
//             _settings.PttEnabled ? DisplayStyle.Flex : DisplayStyle.None;
//         _voiceChannelControls.style.display = DisplayStyle.Flex;
//         _voiceStatusText.text = _activeCall.GetStatus().ToString();
//         ulong channelId = _activeCall.GetChannelId();
//         if (_lobbySecrets.TryGetValue(channelId, out string secret))
//         {
//             _voiceChannelText.text = secret;
//         }
//         else
//         {
//             _voiceChannelText.text = channelId.ToString();
//         }
//     }
//
//     private void OnConnectClicked()
//     {
//         _client.UpdateToken(
//             AuthorizationTokenType.Bearer, _token.text, (result) => { _client.Connect(); });
//     }
//
//     private void OnDisconnectClicked()
//     {
//         ResetClient();
//     }
//
//     private void OnVoiceDisconnectClicked()
//     {
//         if (_activeCall != null)
//         {
//             _client.EndCalls(() => { });
//             _activeCall.Dispose();
//             _activeCall = null;
//         }
//
//         RenderStatus();
//     }
//
//     private void OnMuteClicked()
//     {
//         if (_activeCall != null)
//         {
//             _activeCall.SetSelfMute(!_activeCall.GetSelfMute());
//         }
//
//         RenderStatus();
//     }
//
//     private void OnDeafenClicked()
//     {
//         if (_activeCall != null)
//         {
//             _activeCall.SetSelfDeaf(!_activeCall.GetSelfDeaf());
//         }
//
//         RenderStatus();
//     }
//
//     private void OnSettingsClicked()
//     {
//         TemplateContainer content = _voiceSettingsTemplate.CloneTree();
//         var controller = new VoiceSettingsController(_client, _settings, _activeCall, content);
//         controller.OnClosed += () =>
//         {
//             SaveSettings();
//             RenderStatus();
//         };
//         OpenModal(content, controller);
//     }
//
//     private void OnAnnoyModeClicked()
//     {
//         _annoyModeEnabled = !_annoyModeEnabled;
//     }
//
//     private void OnPttActiveUp(PointerUpEvent evt)
//     {
//         Debug.Log("OnPttActiveUp");
//         if (_activeCall != null)
//         {
//             _activeCall.SetPTTActive(false);
//         }
//     }
//
//     private void OnPttActiveDown(PointerDownEvent evt)
//     {
//         Debug.Log("OnPttActiveDown");
//         if (_activeCall != null)
//         {
//             _activeCall.SetPTTActive(true);
//         }
//     }
//
//     private void FetchDiscordClientConnectedUser()
//     {
//         _client.GetDiscordClientConnectedUser(Config.applicationId, (result, user) =>
//         {
//             _discordClientConnectedUser = user;
//             RenderContentWell();
//         });
//     }
//
// #endregion
//
// #region OAuth2
//
//     private static string UrlSafeBase64Encode(byte[] data) => Convert.ToBase64String(data).Replace('+', '-').Replace('/', '_').TrimEnd('=');
//
//     private void GetTokenFromCode(string code, string redirectUri)
//     {
//         Client client = _client;
//         if (client == null)
//         {
//             return;
//         }
//
//         client.GetToken(Config.applicationId,
//             code,
//             _codeVerifier,
//             redirectUri,
//             (result, token, refreshToken, tokenType, expiresIn, scope) =>
//             {
//                 if (token != "")
//                 {
//                     OnReceivedToken(token);
//                 }
//                 else
//                 {
//                     OnRetrieveTokenFailed();
//                 }
//             });
//     }
//
//     private void OnReceivedToken(string token)
//     {
//         _token.value = token;
//         _client.UpdateToken(
//             AuthorizationTokenType.Bearer, token, (result) => { _client.Connect(); });
//     }
//
//     private void OnRetrieveTokenFailed()
//     {
//         _token.value = "Failed to retrieve token";
//     }
//
//     private void OnGetTokenClicked()
//     {
//         var args = new AuthorizationArgs();
//         AuthorizationCodeVerifier codeVerifier = _client.CreateAuthorizationCodeVerifier();
//         _codeVerifier = codeVerifier.Verifier();
//         args.SetClientId(Config.applicationId);
//         args.SetScopes(Client.GetDefaultCommunicationScopes());
//         args.SetCodeChallenge(codeVerifier.Challenge());
//         _client.Authorize(args, OnAuthorizeResult);
//     }
//
//     private void OnAuthorizeResult(ClientResult result, string code, string redirectUri)
//     {
//         Debug.Log($"Authorization result: [{result.Error()}] [{code}] [{redirectUri}]");
//         if (!result.Successful())
//         {
//             return;
//         }
//
//         GetTokenFromCode(code, redirectUri);
//     }
//
// #endregion
//
// #region Home list
//
//     private void AddHomeListEntry(HomeListEntry entry)
//     {
//         _homeListEntries.Add(entry);
//     }
//
//     private void RenderHomeList()
//     {
//         RefreshHomeList();
//         _homeListView.Clear();
//         foreach (HomeListEntry entry in _homeListEntries)
//         {
//             switch (entry.Type)
//             {
//                 case HomeListEntryType.Header:
//                     var header = new Label(entry.Name);
//                     var headerContainer = new VisualElement();
//                     headerContainer.AddToClassList("list-section-header");
//                     headerContainer.Add(header);
//                     if (entry.AddAction != null)
//                     {
//                         var addButton = new Button(entry.AddAction) { text = "+" };
//                         headerContainer.Add(addButton);
//                     }
//
//                     _homeListView.Add(headerContainer);
//                     break;
//                 case HomeListEntryType.FriendsList:
//                 case HomeListEntryType.Lobby:
//                 case HomeListEntryType.JoinRequest:
//                 case HomeListEntryType.JoinInvite:
//                 case HomeListEntryType.UnityLogs:
//                 case HomeListEntryType.DiscordLogs:
//                 case HomeListEntryType.DiscordConnectedUserInfo:
//                     _homeListView.Add(
//                         new Button(() => OnHomeListEntryClicked(entry)) { text = entry.Name });
//                     break;
//             }
//         }
//     }
//
//     private void RefreshHomeList()
//     {
//         foreach (HomeListEntry entry in _homeListEntries)
//         {
//             entry.Handle?.Dispose();
//         }
//
//         _homeListEntries.Clear();
//         if (_client.GetStatus() == Client.Status.Ready)
//         {
//             AddOnlineHomeListEntries();
//         }
//
//         AddHomeListEntry(
//             new HomeListEntry { Type = HomeListEntryType.Header, Name = "Debug Logs" });
//         AddHomeListEntry(
//             new HomeListEntry { Type = HomeListEntryType.DiscordLogs, Name = "Discord" });
//         AddHomeListEntry(new HomeListEntry { Type = HomeListEntryType.UnityLogs, Name = "Unity" });
//         AddHomeListEntry(new HomeListEntry
//         {
//             Type = HomeListEntryType.DiscordConnectedUserInfo,
//             Name = "Discord Client Connected User"
//         });
//         bool replacedSelectedEntry = false;
//         foreach (HomeListEntry entry in _homeListEntries)
//         {
//             if (entry.IsSame(_selectedHomeListEntry))
//             {
//                 _selectedHomeListEntry = entry;
//                 replacedSelectedEntry = true;
//                 break;
//             }
//         }
//
//         if (!replacedSelectedEntry)
//         {
//             _selectedHomeListEntry = null;
//         }
//     }
//
//     private void AddOnlineHomeListEntries()
//     {
//         AddHomeListEntry(
//             new HomeListEntry { Type = HomeListEntryType.FriendsList, Name = "Friends" });
//         AddHomeListEntry(new HomeListEntry
//         {
//             Type = HomeListEntryType.Header, Name = "Lobbies", AddAction = OnAddLobbyClicked
//         });
//         ulong[] lobbies = _client.GetLobbyIds();
//         foreach (ulong lobbyId in lobbies)
//         {
//             LobbyHandle handle = _client.GetLobbyHandle(lobbyId);
//             string name;
//             if (_lobbySecrets.TryGetValue(lobbyId, out string secret))
//             {
//                 name = $"{secret}";
//             }
//             else
//             {
//                 name = lobbyId.ToString();
//             }
//
//             AddHomeListEntry(new HomeListEntry
//             {
//                 Type = HomeListEntryType.Lobby, Id = handle.Id(), Name = name, Handle = handle
//             });
//         }
//
//         ILookup<ActivityActionTypes, ActivityInvite> requests = _activityInvites.ToLookup(ar => ar.Type());
//         foreach (IGrouping<ActivityActionTypes, ActivityInvite> group in requests)
//         {
//             ActivityInvite[] items = group.ToArray();
//             if (items.Length > 0)
//             {
//                 ActivityActionTypes type = items[0].Type();
//                 string name = type switch
//                 {
//                     ActivityActionTypes.JoinRequest => "Join Requests",
//                     ActivityActionTypes.Join => "Invites",
//                     _ => "Unknown"
//                 };
//                 AddHomeListEntry(
//                     new HomeListEntry { Type = HomeListEntryType.Header, Name = name });
//                 foreach (ActivityInvite item in items)
//                 {
//                     using UserHandle user = _client.GetUser(item.SenderId());
//                     AddHomeListEntry(new HomeListEntry
//                     {
//                         Type =
//                             type switch
//                             {
//                                 ActivityActionTypes.JoinRequest =>
//                                     HomeListEntryType.JoinRequest,
//                                 ActivityActionTypes.Join => HomeListEntryType.JoinInvite,
//                                 _ => throw new InvalidOperationException()
//                             },
//                         Name = user?.Username() ?? "Unknown", Id = item.SenderId()
//                     });
//                 }
//             }
//         }
//     }
//
//     private void OnAddLobbyClicked()
//     {
//         TemplateContainer content = _joinLobbyTemplate.CloneTree();
//         var joinButton = content.Q<Button>("joinButton");
//         var roomName = content.Q<TextField>("roomName");
//         joinButton.clicked += () =>
//         {
//             string secret = roomName.value;
//             CreateOrJoinLobby(secret);
//             CloseModal();
//         };
//         OpenModal(content);
//     }
//
//     private void CreateOrJoinLobby(string secret)
//     {
//         Debug.Log($"CreateOrJoinLobby: {secret}");
//         var userMetadata = new Dictionary<string, string>();
//         var lobbyMetadata = new Dictionary<string, string>();
//         using UserHandle me = _client.GetCurrentUserV2();
//         if (me != null)
//         {
//             lobbyMetadata["creator"] = $"{me.Id()}";
//             userMetadata["displayName"] = me.DisplayName();
//         }
//         else
//         {
//             Debug.LogError("Cannot create lobby: no current user available");
//             return;
//         }
//
//         userMetadata["avatarUrl"] =
//             me.AvatarUrl(UserHandle.AvatarType.Png, UserHandle.AvatarType.Png);
//         _client.CreateOrJoinLobbyWithMetadata(secret,
//             lobbyMetadata,
//             userMetadata,
//             (apiResult, lobbyId) =>
//                 OnLobbyJoined(lobbyId, secret, apiResult));
//     }
//
//     private void SetSelectedHomeListEntry(HomeListEntry entry)
//     {
//         _selectedHomeListEntry = entry;
//         RenderContentWell();
//         if (entry?.Type == HomeListEntryType.Lobby)
//         {
//             SetRichPresence(entry.Id);
//             _contentWell.Q<TextField>("messageInput").Focus();
//         }
//         else if (entry?.Type == HomeListEntryType.DiscordConnectedUserInfo)
//         {
//             FetchDiscordClientConnectedUser();
//         }
//         else
//         {
//             SetRichPresence(null);
//         }
//     }
//
//     private void SetRichPresence(ulong? lobbyId)
//     {
//         using LobbyHandle lobbyHandle = lobbyId != null ? _client.GetLobbyHandle(lobbyId.Value) : null;
//         var activity = new Activity();
//         activity.SetName("Discord x Unity");
//         activity.SetType(ActivityTypes.Playing);
//         if (lobbyHandle != null && _lobbySecrets.ContainsKey(lobbyId.Value))
//         {
//             var activityParty = new ActivityParty();
//             int partySize = lobbyHandle.LobbyMemberIds().Length;
//             activityParty.SetId(lobbyId.Value.ToString());
//             activityParty.SetCurrentSize(partySize);
//             activityParty.SetMaxSize(99);
//             var secrets = new ActivitySecrets();
//             secrets.SetJoin(_lobbySecrets[lobbyId.Value]);
//             activity.SetState($"Lobby ({partySize}/99)");
//             activity.SetParty(activityParty);
//             activity.SetSecrets(secrets);
//         }
//
//         activity.SetSupportedPlatforms(ActivityGamePlatforms.Desktop | ActivityGamePlatforms.IOS);
//         _client.UpdateRichPresence(activity, (result) => { Debug.Log($"UpdateRichPresence: {result.Successful()} {result.Error()}"); });
//     }
//
//     private void OnHomeListEntryClicked(HomeListEntry entry)
//     {
//         switch (entry.Type)
//         {
//             case HomeListEntryType.JoinInvite:
//                 ActivityInvite invite = _activityInvites.FirstOrDefault(ar => ar.SenderId() == entry.Id &&
//                                                                               ar.Type() == ActivityActionTypes.Join);
//                 _client.AcceptActivityInvite(invite, OnActivityInviteAccepted);
//                 _activityInvites.RemoveAll(ar => invite.SenderId() == entry.Id &&
//                                                  ar.Type() == ActivityActionTypes.Join);
//                 RenderHomeList();
//                 break;
//             case HomeListEntryType.JoinRequest:
//                 Debug.Log("TODO: Accept join request");
//                 break;
//             default:
//                 SetSelectedHomeListEntry(entry);
//                 break;
//         }
//     }
//
// #endregion
//
// #region Content well
//
//     private void RenderContentWell()
//     {
//         _contentWell.Clear();
//         if (_selectedHomeListEntry == null)
//         {
//             return;
//         }
//
//         switch (_selectedHomeListEntry.Type)
//         {
//             case HomeListEntryType.FriendsList:
//                 _friendsListTemplate.CloneTree(_contentWell);
//                 BindFriendsListFilters();
//                 RenderFriendsList();
//                 break;
//             case HomeListEntryType.Lobby:
//                 _lobbyViewTemplate.CloneTree(_contentWell);
//                 var lobbyTitle = _contentWell.Q<Label>("lobbyTitle");
//                 lobbyTitle.text = $"Lobby [{_selectedHomeListEntry.Name}]";
//                 RegisterLobbyViewEvents();
//                 RenderMessageList();
//                 RenderMemberList();
//                 break;
//             case HomeListEntryType.UnityLogs:
//             case HomeListEntryType.DiscordLogs:
//                 LogCategory logCategory = _selectedHomeListEntry.Type == HomeListEntryType.UnityLogs
//                     ? LogCategory.Unity
//                     : LogCategory.DiscordPartnerSdk;
//                 var logList = new ScrollView();
//                 logList.AddToClassList("log-list");
//                 if (_logs.TryGetValue(logCategory, out List<LogMessage> messages))
//                 {
//                     foreach (LogMessage message in messages)
//                     {
//                         var label = new Label($"{message.Type}: {message.Text} {message.StackTrace}");
//                         label.AddToClassList("log-message");
//                         logList.Add(label);
//                     }
//                 }
//
//                 logList.schedule.Execute(() => { logList.verticalScroller.value = logList.verticalScroller.highValue; });
//                 _contentWell.Add(logList);
//                 break;
//             case HomeListEntryType.DiscordConnectedUserInfo:
//                 var userContent = new VisualElement();
//                 if (_discordClientConnectedUser == null)
//                 {
//                     userContent.Add(new Label("No user detected"));
//                 }
//                 else
//                 {
//                     userContent.Add(new Label($"Username: {_discordClientConnectedUser.Username()}"));
//                     userContent.Add(new Label($"Id: {_discordClientConnectedUser.Id()}"));
//                 }
//
//                 _contentWell.Add(userContent);
//                 break;
//         }
//     }
//
//     private void BindFriendsListFilters()
//     {
//         var filterButtons = _contentWell.Q<VisualElement>("friendsListFilters");
//         filterButtons.Clear();
//         foreach (FriendsListFilter filter in Enum.GetValues(typeof(FriendsListFilter))
//                      .Cast<FriendsListFilter>())
//         {
//             filterButtons.Add(
//                 new Button(() => OnFriendsListFilterClicked(filter)) { text = filter.ToString() });
//         }
//     }
//
//     private void RenderFriendsList()
//     {
//         if (_selectedHomeListEntry?.Type != HomeListEntryType.FriendsList)
//         {
//             return;
//         }
//
//         var friendListView = _contentWell.Q<ScrollView>("friendsList");
//         _friendsList?.Dispose();
//         _friendsList =
//             _client.GetRelationships().Select(r => new Friend(r)).ToArray().AsDisposable();
//         var displayedFriends =
//             _friendsList.Value.ToArray()
//                 .Where(f => f.MatchesFilter(_friendsListFilter))
//                 .OrderBy(f =>
//                     f.User.DisplayName().Length > 0 ? f.User.DisplayName() : f.User.GlobalName())
//                 .ToArray();
//         friendListView.Clear();
//         foreach (var friend in displayedFriends)
//         {
//             _friendsListEntryTemplate.CloneTree(friendListView.contentContainer);
//             VisualElement last = friendListView[friendListView.childCount - 1];
//             var avatar = last.Q<Image>("avatar");
//             var displayName = last.Q<Label>("displayName");
//             var presence = last.Q<Label>("presence");
//             var backgroundButton = last.Q<Button>("backgroundButton");
//             var dmButton = last.Q<Button>("dmButton");
//             var moreButton = last.Q<Button>("moreButton");
//             displayName.text = friend.User.DisplayName();
//             presence.text = friend.User.Status().ToString() ?? "Offline";
//             LoadImageAsync(
//                     avatar, friend.User.AvatarUrl(UserHandle.AvatarType.Png, UserHandle.AvatarType.Png))
//                 .LogException();
//             backgroundButton.clicked += () => OnFriendClicked(friend);
//             dmButton.clicked += () => OnFriendClicked(friend);
//             moreButton.clicked += () => OnFriendClicked(friend);
//             dmButton.visible = false;
//         }
//     }
//
//     private async Task LoadImageAsync(Image imageElement, string uri)
//     {
//         try
//         {
//             imageElement.image = await _imageLoader.Load(uri);
//         }
//         catch (HttpRequestException e)
//         {
//             Debug.Log($"Failed to load image: {e}");
//         }
//     }
//
//     private void OnFriendClicked(Friend friend)
//     {
//         OpenFriendActionsModal(
//             friend.User.Id(), friend.User.Username(), friend.Relationship.DiscordRelationshipType());
//     }
//
//     private void OpenFriendActionsModal(ulong userId, string username, RelationshipType relType)
//     {
//         var content = new VisualElement();
//         bool hasAddFriend = false;
//         bool hasAcceptFriend = false;
//         bool hasDeclineFriend = false;
//         bool hasBlock = false;
//         bool hasUnblock = false;
//         bool hasRemoveFriend = false;
//         switch (relType)
//         {
//             case RelationshipType.Implicit:
//             case RelationshipType.Suggestion:
//             case RelationshipType.None:
//                 hasAddFriend = true;
//                 break;
//             case RelationshipType.Friend:
//                 hasRemoveFriend = true;
//                 hasBlock = true;
//                 break;
//             case RelationshipType.Blocked:
//                 hasUnblock = true;
//                 break;
//             case RelationshipType.PendingIncoming:
//                 hasAcceptFriend = true;
//                 hasDeclineFriend = true;
//                 break;
//             case RelationshipType.PendingOutgoing:
//                 hasRemoveFriend = true;
//                 break;
//         }
//
//         Client.UpdateRelationshipCallback onRelUpdate = (result) =>
//         {
//             Debug.Log($"Relationship update: {result.Status()} {result.Error()}");
//             RenderMemberList();
//             RenderFriendsList();
//         };
//         Client.SendFriendRequestCallback onFriendRequestSent = (result) =>
//         {
//             Debug.Log($"Friend request sent for {username}: {result.Status()} {result.Error()}");
//             RenderFriendsList();
//         };
//         content.Add(new Label($"Friend Actions ({relType})"));
//         if (hasAddFriend)
//         {
//             content.Add(new Button(() =>
//             {
//                 _client.SendDiscordFriendRequest(username, onFriendRequestSent);
//                 CloseModal();
//             }) { text = "Add Friend" });
//         }
//
//         if (hasAcceptFriend)
//         {
//             content.Add(new Button(() =>
//             {
//                 _client.AcceptDiscordFriendRequest(userId, onRelUpdate);
//                 CloseModal();
//             }) { text = "Accept Friend" });
//         }
//
//         if (hasBlock)
//         {
//             content.Add(new Button(() =>
//             {
//                 _client.BlockUser(userId, onRelUpdate);
//                 CloseModal();
//             }) { text = "Block" });
//         }
//
//         if (hasUnblock)
//         {
//             content.Add(new Button(() =>
//             {
//                 _client.UnblockUser(userId, onRelUpdate);
//                 CloseModal();
//             }) { text = "Unblock" });
//         }
//
//         if (hasRemoveFriend)
//         {
//             content.Add(new Button(() =>
//             {
//                 _client.RemoveDiscordAndGameFriend(userId, onRelUpdate);
//                 CloseModal();
//             }) { text = "Remove Friend" });
//         }
//
//         if (hasDeclineFriend)
//         {
//             content.Add(new Button(() =>
//             {
//                 _client.RejectDiscordFriendRequest(userId, onRelUpdate);
//                 CloseModal();
//             }) { text = "Decline Friend" });
//         }
//
//         OpenModal(content);
//     }
//
//     private void OnFriendsListFilterClicked(FriendsListFilter filter)
//     {
//         _friendsListFilter = filter;
//         RenderFriendsList();
//     }
//
//     private void RegisterLobbyViewEvents()
//     {
//         var inputBar = _contentWell.Q<VisualElement>("inputBar");
//         var messageInput = _contentWell.Q<TextField>("messageInput");
//         var messageList = _contentWell.Q<ScrollView>("messageList");
//         var sendButton = _contentWell.Q<Button>("sendButton");
//         var inviteFriendsButton = _contentWell.Q<Button>("inviteFriendsButton");
//         var joinVoiceButton = _contentWell.Q<Button>("joinVoiceButton");
//         inputBar.RegisterCallback<KeyDownEvent>((evt) =>
//         {
//             if (evt.keyCode == KeyCode.Return || evt.character == '\n')
//             {
//                 evt.StopImmediatePropagation();
//                 evt.PreventDefault();
//             }
//
//             if (evt.keyCode == KeyCode.Return && !evt.shiftKey)
//             {
//                 OnSendMessageIntent(messageInput);
//             }
//         }, TrickleDown.TrickleDown);
//         messageList.contentContainer.RegisterCallback<GeometryChangedEvent>((evt) =>
//         {
//             if (_messageListPendingScrollDown)
//             {
//                 messageList.verticalScroller.value = messageList.verticalScroller.highValue;
//                 _messageListPendingScrollDown = false;
//             }
//         });
//         sendButton.clicked += () => OnSendMessageIntent(messageInput);
//         inviteFriendsButton.clicked += () => OnInviteFriendsButtonClicked();
//         joinVoiceButton.clicked += () => OnJoinVoiceButtonClicked();
//     }
//
//     private void OnSendMessageIntent(TextField messageInput)
//     {
//         if (_selectedHomeListEntry?.Type != HomeListEntryType.Lobby)
//         {
//             return;
//         }
//
//         string message = messageInput.value;
//         if (string.IsNullOrWhiteSpace(message))
//         {
//             return;
//         }
//
//         messageInput.value = "";
//         Debug.Log($"Send message: {message}");
//         ulong lobbyId = _selectedHomeListEntry.Id;
//         using UserHandle me = _client.GetCurrentUserV2();
//         if (me == null)
//         {
//             Debug.LogError("Cannot send message: no current user available");
//             return;
//         }
//
//         var pendingMessage = new Message(me.Username(), message);
//         _client.SendLobbyMessage(lobbyId, message, OnMessageSent);
//     }
//
//     private void RenderMessageList()
//     {
//         if (_selectedHomeListEntry?.Type != HomeListEntryType.Lobby)
//         {
//             return;
//         }
//
//         var messageList = _contentWell.Q<ScrollView>("messageList");
//         messageList.Clear();
//         if (_messages.TryGetValue(_selectedHomeListEntry.Id, out List<Message> messages))
//         {
//             foreach (Message message in messages)
//             {
//                 RenderMessage(messageList, message);
//             }
//         }
//
//         _messageListPendingScrollDown = true;
//     }
//
//     private void RenderMessage(ScrollView messageList, Message message, bool scroll = false)
//     {
//         var label = new Label($"<{message.SenderName}> {message.Content}");
//         label.AddToClassList("message");
//         if (message.LocalId != null && message.Handle == null)
//         {
//             label.AddToClassList("pending");
//         }
//
//         if (scroll)
//         {
//             _messageListPendingScrollDown = true;
//         }
//
//         messageList.Add(label);
//     }
//
//     private void RenderMemberList()
//     {
//         if (_selectedHomeListEntry?.Type != HomeListEntryType.Lobby)
//         {
//             return;
//         }
//
//         var lobby = (LobbyHandle)_selectedHomeListEntry.Handle;
//         ulong[] memberIds = lobby.LobbyMemberIds();
//         var memberList = _contentWell.Q<ScrollView>("memberList");
//         memberList.Clear();
//         Dictionary<string, string> lobbyMetadata = lobby.Metadata();
//         foreach (ulong memberId in memberIds)
//         {
//             using LobbyMemberHandle memberHandle = lobby.GetLobbyMemberHandle(memberId);
//             using UserHandle user = _client.GetUser(memberId);
//             using VoiceStateHandle voiceState = _activeCall?.GetVoiceStateHandle(memberId);
//             Dictionary<string, string> memberMetadata = memberHandle.Metadata();
//             string username = user?.Username();
//             string displayName = user != null
//                 ? user.DisplayName()
//                 : memberMetadata.GetValueOrDefault("displayName", "Unknown");
//             bool connected = memberHandle.Connected();
//             bool inVoice = voiceState != null;
//             bool isOwner = lobbyMetadata.GetValueOrDefault("creator") == memberId.ToString();
//             bool isMuted = voiceState?.SelfMute() ?? false;
//             bool isDeafened = voiceState?.SelfDeaf() ?? false;
//             memberList.Add(new Button(() =>
//             {
//                 if (user != null)
//                 {
//                     OnMemberClicked(memberId, username);
//                 }
//             })
//             {
//                 text =
//                     $"{(isOwner ? "[*] " : "")}{(inVoice ? "[V] " : "")}{displayName}{(!connected ? " [linkdead]" : "")}{(isMuted ? " [M]" : "")}{(isDeafened ? " [D]" : "")}"
//             });
//         }
//     }
//
//     private void OnMemberClicked(ulong memberId, string username)
//     {
//         using RelationshipHandle rel = _client.GetRelationshipHandle(memberId);
//         var relType = RelationshipType.None;
//         if (rel != null)
//         {
//             relType = rel.DiscordRelationshipType();
//         }
//
//         OpenFriendActionsModal(memberId, username, relType);
//     }
//
//     private void OnInviteFriendsButtonClicked()
//     {
//         ulong lobbyId = _selectedHomeListEntry.Id;
//         TemplateContainer content = _inviteFriendModalTemplate.CloneTree();
//         var friendsList = content.Q<ScrollView>("friendsList");
//         var inviteButton = content.Q<Button>("inviteButton");
//         _invitedFriendsList?.Dispose();
//         _invitedFriendsList =
//             _client.GetRelationships().Select(r => new Friend(r)).ToArray().AsDisposable();
//         var displayedFriends = _invitedFriendsList.Value.ToArray()
//             .Where(f => f.MatchesFilter(FriendsListFilter.All))
//             .ToArray();
//         foreach (var friend in displayedFriends)
//         {
//             friendsList.Add(new Toggle(
//                 friend.User.Username()) { value = false, userData = friend.Relationship.Id() });
//         }
//
//         inviteButton.clicked += () =>
//         {
//             ulong[] selectedFriendIds = friendsList.Children()
//                 .Where(c => c is Toggle t && t.value)
//                 .Select(c => (ulong)c.userData)
//                 .ToArray();
//             DoInviteFriends(lobbyId, selectedFriendIds);
//         };
//         OpenModal(content);
//     }
//
//     private void OnJoinVoiceButtonClicked()
//     {
//         if (_selectedHomeListEntry?.Type != HomeListEntryType.Lobby)
//         {
//             return;
//         }
//
//         if (_activeCall != null)
//         {
//             _client.EndCalls(() => { });
//             _activeCall.Dispose();
//             _activeCall = null;
//         }
//
//         _activeCall = _client.StartCall(_selectedHomeListEntry.Id);
//         if (_activeCall == null)
//         {
//             Debug.Log("Failed to create discord call.");
//             return;
//         }
//
//         _activeCall.SetAudioMode(_settings.PttEnabled
//             ? AudioModeType.MODE_PTT
//             : AudioModeType.MODE_VAD);
//         _activeCall.SetStatusChangedCallback(OnCallStatusChanged);
//         _activeCall.SetParticipantChangedCallback(OnParticipantChanged);
//         _activeCall.SetOnVoiceStateChangedCallback(OnVoiceStateChanged);
//         RenderStatus();
//     }
//
//     private void OnCallStatusChanged(Call.Status status, Call.Error error, int errorDetail)
//     {
//         Debug.Log($"Call status changed: {status} {error} {errorDetail}");
//         RenderStatus();
//         RenderMemberList();
//     }
//
//     private void OnParticipantChanged(ulong userId, bool speaking)
//     {
//         Debug.Log($"Participant changed: {userId} {speaking}");
//         RenderMemberList();
//     }
//
//     private void OnVoiceStateChanged(ulong userId)
//     {
//         Debug.Log($"Voice state changed: {userId}");
//         RenderMemberList();
//     }
//
//     private void DoInviteFriends(ulong lobbyId, ulong[] selectedFriendIds)
//     {
//         foreach (ulong friendId in selectedFriendIds)
//         {
//             _client.SendActivityInvite(friendId, "",
//                 (result) => { Debug.Log($"Invite sent: {friendId} {result.Status()} {result.Error()}"); });
//         }
//
//         CloseModal();
//     }
//
// #endregion
// }