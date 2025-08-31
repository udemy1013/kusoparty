using UnityEngine;
using TMPro; // TextMeshPro を使う場合
using System.Threading.Tasks;

public class DevMenu : MonoBehaviour
{
    [SerializeField] TMP_InputField joinCodeInput;
    [SerializeField] TMP_Text joinCodeDisplay;
    SimpleRelayConnector connector = new();

    public async void OnClick_Host()
    {
        string code = await connector.StartHostAsync();
        if (joinCodeDisplay) joinCodeDisplay.text = code;
        GUIUtility.systemCopyBuffer = code; // クリップボードにコピー（MPPMで便利）
        Debug.Log("JoinCode: " + code);
    }

    public async void OnClick_Join()
    {
        if (joinCodeInput == null) return;
        bool ok = await connector.StartClientAsync(joinCodeInput.text.Trim());
        Debug.Log("Join: " + ok);
    }
}
