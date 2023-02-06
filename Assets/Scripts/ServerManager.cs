namespace Fusion.Sample.DedicatedServer
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using PlayFab;
    using Fusion.Photon.Realtime;
    using Fusion.Sockets;
    using System.Collections;
    using System.Threading.Tasks;
    using System.Net;
    
    public class ServerManager : MonoBehaviour
    {
        [SerializeField] private NetworkRunner _runnerPrefab;
        public bool Debugging = true;
        
        public void Awake()
        {
            Debug.Log("ServerManager Awake");
            if (CommandLineUtils.IsHeadlessMode() == false) {
                SceneManager.LoadScene((int)SceneDefs.CLIENTMENU, LoadSceneMode.Single);
                return;
            }
        }
        void Start () {
            Debug.Log("ServerManager Start");
            PlayFabMultiplayerAgentAPI.Start();
            PlayFabMultiplayerAgentAPI.IsDebugging = Debugging;
            PlayFabMultiplayerAgentAPI.OnServerActiveCallback += OnServerActive;
            StartCoroutine(ReadyForPlayers());
        }
        
        IEnumerator ReadyForPlayers()
        {
            Debug.Log("ServerManager ReadyForPlayers");
            yield return new WaitForSeconds(.5f);
            PlayFabMultiplayerAgentAPI.ReadyForPlayers();
        }
    
        private void OnServerActive()
        {
            Debug.Log("ServerManager OnServerActive");
            
            //For Fusion
            StartFusion();
        }
        async void StartFusion() {
          Debug.Log("ServerManager Start");
          Application.targetFrameRate = 30;

          var config = DedicatedServerConfig.Resolve();
          Debug.Log(config);
          
          var runner = Instantiate(_runnerPrefab);
          var result = await StartSimulation(
            runner,
            config.SessionName,
            config.SessionProperties,
            config.Port,
            config.Lobby,
            config.Region,
            config.PublicIP,
            config.PublicPort
          );
          
          if (result.Ok) {
            Log.Debug($"Runner Start DONE");
          } else {
            Log.Debug($"Error while starting Server: {result.ShutdownReason}");
            Application.Quit(1);
          }
        }

        private Task<StartGameResult> StartSimulation(
          NetworkRunner runner,
          string SessionName,
          Dictionary<string, SessionProperty> customProps,
          ushort port,
          string customLobby,
          string customRegion,
          string customPublicIP = null,
          ushort customPublicPort = 0
        ) {

          Debug.Log("ServerManager StartSimulation");
          
          // Build Custom Photon Config
          var photonSettings = PhotonAppSettings.Instance.AppSettings.GetCopy();

          if (string.IsNullOrEmpty(customRegion) == false) {
            photonSettings.FixedRegion = customRegion.ToLower();
          }

          // Build Custom External Addr
          NetAddress? externalAddr = null;

          if (string.IsNullOrEmpty(customPublicIP) == false && customPublicPort > 0) {
            if (IPAddress.TryParse(customPublicIP, out var _)) {
              externalAddr = NetAddress.CreateFromIpPort(customPublicIP, customPublicPort);
            } else {
              Log.Warn("Unable to parse 'Custom Public IP'");
            }
          }
          
          return runner.StartGame(new StartGameArgs() {
            SessionName = SessionName,
            GameMode = GameMode.Server,
            SceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>(),
            Scene = (int)SceneDefs.CLIENTGAME,
            SessionProperties = customProps,
            Address = NetAddress.Any(port),
            CustomPublicAddress = externalAddr,
            CustomLobbyName = customLobby,
            CustomPhotonAppSettings = photonSettings,
          });
        }
    }
}
