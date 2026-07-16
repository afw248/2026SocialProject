using UnityEngine;

namespace ChangJun.Craft
{
    /// <summary>
    /// 예전 판정 자동 검증용. 플레이 시 더 이상 실행하지 않는다.
    /// </summary>
    public sealed class CraftJudgeValidator : MonoBehaviour
    {
        private void Awake()
        {
            Destroy(gameObject);
        }
    }
}
