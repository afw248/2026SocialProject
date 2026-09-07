using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;

namespace ChangJun.Economy
{
    /// <summary>UGS Core + Anonymous Auth 세션.</summary>
    public sealed class UnityServicesSession : MonoBehaviour
    {
        public static UnityServicesSession Instance { get; private set; }

        [SerializeField] private string _environmentName = "production";

        public bool IsSignedIn =>
            UnityServices.State == ServicesInitializationState.Initialized &&
            AuthenticationService.Instance.IsSignedIn;

        public string PlayerId => IsSignedIn ? AuthenticationService.Instance.PlayerId : string.Empty;

        public string PlayerName
        {
            get
            {
                if (!IsSignedIn) return "나";
                string name = AuthenticationService.Instance.PlayerName;
                return string.IsNullOrEmpty(name) ? "사장" : name;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public async Task EnsureSignedInAsync(CancellationToken token = default)
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                var options = new InitializationOptions().SetEnvironmentName(_environmentName);
                await UnityServices.InitializeAsync(options);
                token.ThrowIfCancellationRequested();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                token.ThrowIfCancellationRequested();
            }

            await TryAssignPlayerNameAsync(token);
        }

        public async Task TryUpdatePlayerNameAsync(string displayName, CancellationToken token = default)
        {
            if (!IsSignedIn) return;
            displayName = (displayName ?? string.Empty).Trim();
            if (displayName.Length < 1 || displayName.Length > 50) return;

            try
            {
                await AuthenticationService.Instance.UpdatePlayerNameAsync(displayName);
                token.ThrowIfCancellationRequested();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[UGS] 플레이어 이름 갱신 실패: " + e.Message);
            }
        }

        private async Task TryAssignPlayerNameAsync(CancellationToken token)
        {
            if (!string.IsNullOrEmpty(AuthenticationService.Instance.PlayerName))
                return;

            try
            {
                string name = "Boss" + UnityEngine.Random.Range(1000, 10000);
                await AuthenticationService.Instance.UpdatePlayerNameAsync(name);
                token.ThrowIfCancellationRequested();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[UGS] 초기 이름 설정 실패: " + e.Message);
            }
        }
    }
}
