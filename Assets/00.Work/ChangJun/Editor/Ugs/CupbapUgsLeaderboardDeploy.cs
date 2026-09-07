using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Unity.Services.Core.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace ChangJun.EditorTools
{
    /// <summary>cupbap-money 리더보드 배포. (도메인 리로드 없이 REST만 수행)</summary>
    public static class CupbapUgsLeaderboardDeploy
    {
        const string LeaderboardId = "cupbap-money";
        const string LeaderboardName = "Cupbap Money";

        // 권한 확인된 org/project/env (changjun1201)
        const string FallbackOrgId = "changjun1201";
        const string FallbackProjectId = "d4b09d6a-687b-453d-b170-e6745c767d90";
        const string FallbackEnvId = "4f5323b8-82c9-4706-b210-e21a5ed03afc";
        const string LinkedProjectName = "CupbapSocial";

        [MenuItem("Cupbap/Deploy UGS Money Leaderboard")]
        public static async void Deploy()
        {
            string logPath = Path.Combine(Application.temporaryCachePath, "cupbap-ugs-leaderboard.txt");
            try
            {
                File.WriteAllText(logPath, "start\n");
                File.AppendAllText(logPath,
                    "Application.cloudProjectId=" + Application.cloudProjectId +
                    " CloudProjectSettings.projectId=" + CloudProjectSettings.projectId + "\n");

                string token = await new AccessTokens().GetServicesGatewayTokenAsync();
                if (string.IsNullOrEmpty(token))
                    token = AccessTokens.GetGenesisToken();

                File.AppendAllText(logPath, "tokenLen=" + (token?.Length ?? 0) + "\n");
                if (string.IsNullOrEmpty(token))
                {
                    Debug.LogError("[UGS] 토큰 없음\n" + logPath);
                    return;
                }

                // 런타임/에디터가 실제로 붙는 프로젝트 ID 우선
                string projectId = Application.cloudProjectId;
                if (string.IsNullOrEmpty(projectId))
                    projectId = CloudProjectSettings.projectId;
                string orgId = CloudProjectSettings.organizationId;
                File.AppendAllText(logPath, "linked project=" + projectId + " org=" + orgId + "\n");

                string envId = await TryGetEnv(projectId, orgId, token, logPath);
                if (string.IsNullOrEmpty(envId) ||
                    string.Equals(projectId, "0862350a-1232-4171-a3d7-f0756d2cd7c2", StringComparison.OrdinalIgnoreCase))
                {
                    projectId = FallbackProjectId;
                    orgId = FallbackOrgId;
                    envId = FallbackEnvId;
                    File.AppendAllText(logPath,
                        "using fallback project=" + projectId + " env=" + envId + "\n");
                }

                string post = await PostBoard(projectId, envId, token);
                string patch = await PatchBoard(projectId, envId, token);
                File.AppendAllText(logPath, post + patch);

                bool ok = post.Contains("status=201") || post.Contains("status=409") ||
                          patch.Contains("status=204");
                File.AppendAllText(logPath, ok ? "board_ok\n" : "board_fail\n");

                WriteProjectSettings(projectId, orgId, LinkedProjectName);
                File.AppendAllText(logPath, "projectsettings_written\ndone\n");

                if (ok)
                {
                    Debug.Log("[UGS] cupbap-money ready. project=" + projectId + " env=" + envId +
                              " appCloudId=" + Application.cloudProjectId);
                    EditorApplication.delayCall += () =>
                    {
                        AssetDatabase.Refresh();
                        EditorUtility.DisplayDialog("UGS",
                            "cupbap-money 준비 완료\nproject=" + projectId +
                            "\napp.cloudProjectId=" + Application.cloudProjectId, "OK");
                    };
                }
                else
                {
                    Debug.LogError("[UGS] 리더보드 생성 실패\n" + logPath);
                    EditorUtility.RevealInFinder(logPath);
                }
            }
            catch (Exception e)
            {
                File.AppendAllText(logPath, "exception=" + e + "\n");
                Debug.LogError("[UGS] " + e.Message + "\n" + logPath);
            }
        }

        static void WriteProjectSettings(string projectId, string orgId, string projectName)
        {
            string path = "ProjectSettings/ProjectSettings.asset";
            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"cloudProjectId:.*", "cloudProjectId: " + projectId);
            text = Regex.Replace(text, @"organizationId:.*", "organizationId: " + orgId);
            text = Regex.Replace(text, @"projectName:.*", "projectName: " + projectName);
            text = Regex.Replace(text, @"cloudEnabled:.*", "cloudEnabled: 1");
            File.WriteAllText(path, text);
        }

        static async Task<string> TryGetEnv(string projectId, string orgId, string token, string logPath)
        {
            if (string.IsNullOrEmpty(projectId)) return null;
            string url = "https://services.unity.com/api/unity/legacy/v1/projects/" + projectId + "/environments";
            using var req = UnityWebRequest.Get(url);
            req.SetRequestHeader("Authorization", "Bearer " + token);
            await Send(req);
            File.AppendAllText(logPath,
                "GET env => " + req.responseCode + " " + Trunc(req.downloadHandler.text) + "\n");
            if (req.responseCode < 200 || req.responseCode >= 300) return null;
            return PreferProductionEnv(req.downloadHandler.text);
        }

        static string PreferProductionEnv(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            var matches = Regex.Matches(json,
                "\\{[^{}]*\"id\"\\s*:\\s*\"([0-9a-fA-F-]{36})\"[^{}]*\\}");
            string first = null;
            foreach (Match m in matches)
            {
                if (first == null) first = m.Groups[1].Value;
                if (m.Value.IndexOf("production", StringComparison.OrdinalIgnoreCase) >= 0)
                    return m.Groups[1].Value;
            }

            var g = Regex.Match(json,
                "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");
            return g.Success ? g.Value : first;
        }

        static async Task<string> PostBoard(string projectId, string envId, string token)
        {
            string url =
                "https://services.unity.com/api/leaderboards/v1/projects/" + projectId +
                "/environments/" + envId + "/leaderboards";
            string body =
                "{\"id\":\"" + LeaderboardId + "\",\"name\":\"" + LeaderboardName +
                "\",\"sortOrder\":\"desc\",\"updateType\":\"keepBest\"}";
            return LeaderboardId + " " + await SendJson(url, "POST", token, body) + "\n";
        }

        static async Task<string> PatchBoard(string projectId, string envId, string token)
        {
            string url =
                "https://services.unity.com/api/leaderboards/v1/projects/" + projectId +
                "/environments/" + envId + "/leaderboards/" + LeaderboardId;
            string body =
                "{\"name\":\"" + LeaderboardName + "\",\"sortOrder\":\"desc\",\"updateType\":\"keepBest\"}";
            return "patch " + LeaderboardId + " " + await SendJson(url, "PATCH", token, body) + "\n";
        }

        static async Task<string> SendJson(string url, string method, string token, string body)
        {
            using var req = new UnityWebRequest(url, method);
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Authorization", "Bearer " + token);
            req.SetRequestHeader("Content-Type", "application/json");
            await Send(req);
            return "status=" + req.responseCode + " body=" + Trunc(req.downloadHandler.text);
        }

        static async Task Send(UnityWebRequest req)
        {
            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();
        }

        static string Trunc(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Length <= 500 ? s : s.Substring(0, 500);
        }
    }
}
