using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class RelayManager : MonoBehaviour
{
    private static bool isInitializing = false;

    private async void Start()
    {
        await InitializeServicesAsync();
    }

    public async Task<bool> InitializeServicesAsync()
    {
        if (UnityServices.State == ServicesInitializationState.Initialized && AuthenticationService.Instance.IsSignedIn)
        {
            return true;
        }

        while (isInitializing)
        {
            await Task.Delay(100);
            if (UnityServices.State == ServicesInitializationState.Initialized && AuthenticationService.Instance.IsSignedIn)
                return true;
        }

        isInitializing = true;

        try
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                var options = new InitializationOptions();

#if UNITY_EDITOR
                string[] args = System.Environment.GetCommandLineArgs();
                bool isClone = false;
                foreach (string arg in args)
                {
                    if (arg == "-parrelsyncclone")
                    {
                        isClone = true;
                        break;
                    }
                }
                options.SetProfile(isClone ? "Player2" : "Player1");
#endif

                await UnityServices.InitializeAsync(options);
                Debug.Log("[Relay] Unity Services initialized successfully!");
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("[Relay] Signed in successfully!");
            }
            else
            {
                Debug.Log("[Relay] Player is already signed in.");
            }

            isInitializing = false;
            return true;
        }
        catch (System.Exception e)
        {
            isInitializing = false;
            Debug.LogError($"[Relay] Failed to initialize Unity Services: {e.Message}");
            return false;
        }
    }

    public static string JoinCode = "";

    public async Task<string> CreateRelay()
    {
        try
        {
            if (!await InitializeServicesAsync())
            {
                Debug.LogError("[Relay] Cannot create allocation: Unity Services initialization failed.");
                return null;
            }

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(4);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            NetworkManager.Singleton.StartHost();
            JoinCode = joinCode;
            Debug.Log($"[Relay] Host created! Join Code: {joinCode}");
            return joinCode;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Relay] Failed to create: {e.Message}");
            return null;
        }
    }

    public async Task<bool> JoinRelay(string joinCode)
    {
        try
        {
            if (!await InitializeServicesAsync())
            {
                Debug.LogError("[Relay] Cannot join allocation: Unity Services initialization failed.");
                return false;
            }

            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            NetworkManager.Singleton.StartClient();
            Debug.Log($"[Relay] Joined successfully!");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Relay] Failed to join: {e.Message}");
            return false;
        }
    }
}
