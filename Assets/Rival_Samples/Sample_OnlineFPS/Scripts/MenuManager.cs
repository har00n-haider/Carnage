using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using Unity.Collections;
using UnityEngine.Networking;

namespace Rival.Samples.OnlineFPS
{
    public class MenuManager : MonoBehaviour
    {
        public GameObject MainPanel;
        public GameObject JoiningPanel;
        public InputField PlayerNameField;
        public Button HostButton;
        public Button JoinButton;
        public InputField JoinIPField;
        public InputField JoinPortField;
        public InputField HostPortField;

        private EntityQuery _networkIdQuery;
        private bool _readyToLoadScene = false;
        private bool _isTryingToJoin = false;
        private float _lastJoinStartTime = float.MinValue;
        private const float _kMaxJoinTime = 5f;
        private const string _kLocalHost = "127.0.0.1";

        void Start()
        {
            // TODO: in builds, a spam of warnings slow framerate down whenever the framerate is much higher than the tickrate.
            // So we need to keep this until this is fixed in a future Netcode version
            Application.targetFrameRate = 100;

            MainPanel.SetActive(true);
            JoiningPanel.SetActive(false);

            // TODO
#if !UNITY_EDITOR && !UNITY_SERVER
            //Screen.SetResolution(800, 450, false);
#endif

            Application.runInBackground = true;

            HostButton.onClick.AddListener(OnHostButton);
            JoinButton.onClick.AddListener(OnJoinButton);

            // Start connecting to something just once so we can get a firewall prompt
            {
                World serverWorld = ClientServerBootstrap.CreateServerWorld(World.DefaultGameObjectInjectionWorld, "ServerWorld");
                NetworkEndPoint serverEndPoint = NetworkEndPoint.AnyIpv4;
                serverEndPoint.Port = ushort.Parse(HostPortField.text);
                serverWorld.GetExistingSystem<NetworkStreamReceiveSystem>().Listen(serverEndPoint);
                serverWorld.Dispose();
            }

#if UNITY_SERVER
            OnHostButton();
#endif
        }

        public void OnHostButton()
        {
#if UNITY_SERVER
            World serverWorld = ClientServerBootstrap.CreateServerWorld(World.DefaultGameObjectInjectionWorld, "ServerWorld");
#else
            World clientWorld = ClientServerBootstrap.CreateClientWorld(World.DefaultGameObjectInjectionWorld, "ClientWorld");
            World serverWorld = ClientServerBootstrap.CreateServerWorld(World.DefaultGameObjectInjectionWorld, "ServerWorld");
#endif
            // Start server listen
            NetworkEndPoint serverEndPoint = NetworkEndPoint.AnyIpv4;
            serverEndPoint.Port = ushort.Parse(HostPortField.text);
            serverWorld.GetExistingSystem<NetworkStreamReceiveSystem>().Listen(serverEndPoint);

#if !UNITY_SERVER
            // Connect client
            NetworkEndPoint clientEndPoint = NetworkEndPoint.Parse(_kLocalHost, ushort.Parse(HostPortField.text), NetworkFamily.Ipv4);
            clientWorld.GetExistingSystem<NetworkStreamReceiveSystem>().Connect(clientEndPoint);

            var localGameData = OnlineFPSUtilities.GetOrCreateSingleton<LocalGameData>(clientWorld);
            localGameData.PlayerName = PlayerNameField.text;
            clientWorld.Systems[0].SetSingleton<LocalGameData>(localGameData);
#endif

            _readyToLoadScene = true;
        } 

        public void OnJoinButton()
        {
            World clientWorld = ClientServerBootstrap.CreateClientWorld(World.DefaultGameObjectInjectionWorld, "ClientWorld");

            // Connect client
            NetworkEndPoint clientEndPoint = NetworkEndPoint.Parse(JoinIPField.text, ushort.Parse(JoinPortField.text), NetworkFamily.Ipv4);
            clientWorld.GetExistingSystem<NetworkStreamReceiveSystem>().Connect(clientEndPoint);

            var localGameData = OnlineFPSUtilities.GetOrCreateSingleton<LocalGameData>(clientWorld);
            localGameData.PlayerName = PlayerNameField.text;
            clientWorld.Systems[0].SetSingleton<LocalGameData>(localGameData);

            _networkIdQuery = clientWorld.EntityManager.CreateEntityQuery(typeof(NetworkIdComponent));
            _isTryingToJoin = true;
            _lastJoinStartTime = Time.time;
            MainPanel.SetActive(false);
            JoiningPanel.SetActive(true);
        }

        private void Update()
        {
            if(_isTryingToJoin)
            {
                if(Time.time < _lastJoinStartTime + _kMaxJoinTime)
                {
                    if (_networkIdQuery.CalculateEntityCount() > 0)
                    {
                        _readyToLoadScene = true;
                    }
                }
                else
                {
                    _isTryingToJoin = false;

                    MainPanel.SetActive(true);
                    JoiningPanel.SetActive(false);
                }
            }

            if(_readyToLoadScene)
            {
                SceneManager.LoadScene(OnlineFPSGameData.Load().GameSceneName);
            }
        }
    }
}