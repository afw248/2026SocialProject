using System.Collections.Generic;
using System.IO;
using ChangJun.Data;
using ChangJun.Economy;
using ChangJun.Judge;
using UnityEngine;

namespace ChangJun.Craft
{
    /// <summary>
    /// 플레이 시작 시 판정 로직 3케이스를 자동 실행하고 콘솔에 검증 결과를 출력한다.
    /// 검증 완료 후 이 컴포넌트는 제거해도 무방하다.
    /// </summary>
    public sealed class CraftJudgeValidator : MonoBehaviour
    {
        private void Start()
        {
            var menus     = Resources.LoadAll<MenuRecipeSO>("Craft/Menus");
            var ings      = Resources.LoadAll<IngredientSO>("Craft/Ingredients");
            var customers = Resources.LoadAll<CraftCustomerSO>("Craft/Customers");

            Debug.Log($"[Validator] Loaded: menus={menus.Length} ings={ings.Length} customers={customers.Length}");

            if (menus.Length == 0 || ings.Length == 0 || customers.Length == 0)
            {
                Debug.LogError("[Validator] SO assets missing. Run Build Craft Prototype first.");
                PlayerPrefs.SetString("ValidatorResult", "FAIL:SO_MISSING");
                return;
            }

            var ingMap = new Dictionary<string, IngredientSO>();
            foreach (var i in ings) ingMap[i.code] = i;

            // ingredient existence check
            foreach (var code in new[]{"HBF","SPC","PRK","BRT","TFU"})
                Debug.Log($"[Validator] ing[{code}]={(ingMap.ContainsKey(code) ? "OK" : "MISSING")}");

            CraftCustomerSO aisha = null;
            foreach (var c in customers)
            {
                Debug.Log($"[Validator] customer={c.customerName} diet={c.diet}");
                if (c.customerName == "\uc544\uc774\uc0e4") { aisha = c; }
            }

            if (aisha == null)
            {
                // Try first customer as fallback
                aisha = customers[0];
                Debug.LogWarning($"[Validator] Aisha not found by name, using fallback={aisha.customerName}");
            }

            var book = new RecipeBook(menus);

            // Case 1: Success (HBF+SPC)
            var sel1 = new List<IngredientSO> { ingMap["HBF"], ingMap["SPC"] };
            var r1   = RecipeJudge.Judge(sel1, aisha, book, out var m1);
            bool ok1 = r1 == CraftResult.Success;
            Debug.Log($"[Validator] CASE1 expect=Success actual={r1} menu={m1?.displayName} PASS={ok1}");

            // Case 2: TabooViolation (PRK+BRT to Halal customer)
            var sel2 = new List<IngredientSO> { ingMap["PRK"], ingMap["BRT"] };
            var r2   = RecipeJudge.Judge(sel2, aisha, book, out var m2);
            bool ok2 = r2 == CraftResult.TabooViolation;
            Debug.Log($"[Validator] CASE2 expect=TabooViolation actual={r2} menu={m2?.displayName} PASS={ok2}");

            // Case 3: WrongRecipe (HBF+TFU matches no menu)
            var sel3 = new List<IngredientSO> { ingMap["HBF"], ingMap["TFU"] };
            var r3   = RecipeJudge.Judge(sel3, aisha, book, out var m3);
            bool ok3 = r3 == CraftResult.WrongRecipe;
            Debug.Log($"[Validator] CASE3 expect=WrongRecipe actual={r3} menu={m3?.displayName ?? "null"} PASS={ok3}");

            // MoneyManager integration
            if (MoneyManager.Instance != null)
            {
                int before = MoneyManager.Instance.Money;
                int addAmt = m1 != null ? m1.price : 300;
                MoneyManager.Instance.AddMoney(addAmt);
                int after  = MoneyManager.Instance.Money;
                bool okMoney = (after - before) == addAmt;
                Debug.Log($"[Validator] MONEY before={before} after={after} delta={after-before} PASS={okMoney}");
            }

            bool allPass = ok1 && ok2 && ok3;
            string summary = allPass ? "ALL_PASS" : "SOME_FAIL";
            Debug.Log($"[Validator] FINAL={summary}");

            // PlayerPrefs 저장
            PlayerPrefs.SetString("ValidatorResult", summary);
            PlayerPrefs.SetString("Case1", $"{r1}");
            PlayerPrefs.SetString("Case2", $"{r2}");
            PlayerPrefs.SetString("Case3", $"{r3}");
            PlayerPrefs.Save();

            // 파일 출력 (MCP 콘솔 이슈 우회)
            string logPath = System.IO.Path.Combine(Application.persistentDataPath, "validator_result.txt");
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"FINAL={summary}");
            sb.AppendLine($"CASE1 expect=Success      actual={r1} PASS={ok1}  menu={m1?.displayName ?? "null"}");
            sb.AppendLine($"CASE2 expect=TabooVio.    actual={r2} PASS={ok2}  menu={m2?.displayName ?? "null"}");
            sb.AppendLine($"CASE3 expect=WrongRecipe  actual={r3} PASS={ok3}  menu={m3?.displayName ?? "null"}");
            if (MoneyManager.Instance != null)
                sb.AppendLine($"MONEY before={MoneyManager.Instance.Money - (m1!=null?m1.price:300)} after={MoneyManager.Instance.Money}");
            sb.AppendLine($"PersistentDataPath={Application.persistentDataPath}");
            File.WriteAllText(logPath, sb.ToString());
            Debug.Log($"[Validator] Result written to {logPath}");
        }
    }
}
