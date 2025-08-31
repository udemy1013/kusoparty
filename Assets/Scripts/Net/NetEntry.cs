using UnityEngine;

public class NetEntry : MonoBehaviour
{
    [SerializeField] string environmentName = "dev";

    async void Awake()
    {
        // 最初のシーンで一度だけ呼ばれるよう、このオブジェクトはルートに1つだけ置く
        await UgsBootstrap.InitAndSignInAsync(environmentName);
        DontDestroyOnLoad(gameObject);
    }
}