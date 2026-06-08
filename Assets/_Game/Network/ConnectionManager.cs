using System;
using System.Threading.Tasks;
using UnityEngine;
#if NETCODE_PRESENT
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
#if UGS_AUTH
using Unity.Services.Core;
using Unity.Services.Authentication;
#endif
#if UGS_RELAY
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Networking.Transport.Relay;
#endif
#if UGS_LOBBY
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
#endif

namespace CarrotClash.Net
{
    /// <summary>
    /// Thin wrapper around <see cref="NetworkManager"/> for starting a host or joining as a client.
    ///
    /// When the UGS packages are present (UGS_AUTH / UGS_RELAY / UGS_LOBBY defines) the manager routes
    /// through Unity Relay (host allocates, clients join via a relay join code) so players behind NAT
    /// can connect without port-forwarding. Without those packages it falls back to the plain
    /// UnityTransport (direct IP / loopback) so local testing still works.
    ///
    /// This component only handles transport/session plumbing — all gameplay authority lives in the
    /// per-object mirror NetworkBehaviours and the spine.
    /// </summary>
    [DisallowMultipleComponent]
    public class ConnectionManager : MonoBehaviour
    {
        [Header("Match sizing")]
        [SerializeField] int maxConnections = GameConstants.MaxPlayers;

        /// <summary>Raised with the relay/lobby join code once a host is ready (empty string in plain transport mode).</summary>
        public event Action<string> OnHostReady;
        /// <summary>Raised when a connection attempt fails, with a human-readable reason.</summary>
        public event Action<string> OnConnectionFailed;

        bool servicesInitialised;

        NetworkManager Net => NetworkManager.Singleton;

        /// <summary>
        /// Start as host. With Relay present this allocates a relay slot and surfaces a join code via
        /// <see cref="OnHostReady"/>; otherwise it starts the host on the plain transport.
        /// </summary>
        public async void StartHost()
        {
            if (Net == null)
            {
                OnConnectionFailed?.Invoke("NetworkManager.Singleton is null.");
                return;
            }

            string joinCode = string.Empty;

#if UGS_RELAY
            try
            {
                await EnsureServicesAsync();
                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(Mathf.Max(1, maxConnections - 1));
                joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

                var transport = Net.GetComponent<UnityTransport>();
                if (transport != null)
                    transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));
            }
            catch (Exception e)
            {
                OnConnectionFailed?.Invoke($"Relay host allocation failed: {e.Message}");
                return;
            }
#endif

            if (!Net.StartHost())
            {
                OnConnectionFailed?.Invoke("NetworkManager.StartHost() returned false.");
                return;
            }

            OnHostReady?.Invoke(joinCode);
        }

        /// <summary>
        /// Start as client. With Relay present, <paramref name="joinCode"/> is the relay join code from
        /// the host; without Relay it is ignored and the plain transport's configured endpoint is used.
        /// </summary>
        public async void StartClient(string joinCode)
        {
            if (Net == null)
            {
                OnConnectionFailed?.Invoke("NetworkManager.Singleton is null.");
                return;
            }

#if UGS_RELAY
            if (!string.IsNullOrEmpty(joinCode))
            {
                try
                {
                    await EnsureServicesAsync();
                    JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

                    var transport = Net.GetComponent<UnityTransport>();
                    if (transport != null)
                        transport.SetRelayServerData(AllocationUtils.ToRelayServerData(joinAllocation, "dtls"));
                }
                catch (Exception e)
                {
                    OnConnectionFailed?.Invoke($"Relay join failed: {e.Message}");
                    return;
                }
            }
#else
            // Plain transport: joinCode is unused; connection uses the transport's serialized endpoint.
            await Task.CompletedTask;
#endif

            if (!Net.StartClient())
                OnConnectionFailed?.Invoke("NetworkManager.StartClient() returned false.");
        }

        /// <summary>Cleanly shut the active session down.</summary>
        public void Disconnect()
        {
            if (Net != null && (Net.IsListening || Net.IsConnectedClient))
                Net.Shutdown();
        }

#if UGS_LOBBY
        /// <summary>
        /// Create a lobby that advertises the relay join code so a lobby browser can surface this match.
        /// Requires both Lobby and Relay; call after <see cref="StartHost"/> has produced a join code.
        /// </summary>
        public async Task<Lobby> CreateLobbyAsync(string lobbyName, string relayJoinCode)
        {
            await EnsureServicesAsync();
            var options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Data = new System.Collections.Generic.Dictionary<string, DataObject>
                {
                    { "joinCode", new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) }
                }
            };
            return await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxConnections, options);
        }

        /// <summary>Join a lobby by id and return the embedded relay join code (pass to <see cref="StartClient"/>).</summary>
        public async Task<string> JoinLobbyAndGetCodeAsync(string lobbyId)
        {
            await EnsureServicesAsync();
            Lobby lobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);
            if (lobby != null && lobby.Data != null && lobby.Data.TryGetValue("joinCode", out DataObject code))
                return code.Value;
            return string.Empty;
        }
#endif

#if UGS_AUTH
        async Task EnsureServicesAsync()
        {
            if (servicesInitialised) return;
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            servicesInitialised = true;
        }
#else
        Task EnsureServicesAsync()
        {
            // No UGS auth package: nothing to initialise for plain transport.
            servicesInitialised = true;
            return Task.CompletedTask;
        }
#endif
    }
}
#endif
