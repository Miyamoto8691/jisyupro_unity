using UnityEngine;

public class Pocketwatch : MonoBehaviour
{
    public PlatformController platformController;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnTriggerEnter(Collider other)
    {
        if (other == platformController.foundation)
        {
            return; //沈んでfoundationに当たった時はスキップ
        }
        Debug.Log("pocketwatch通過");
        platformController.reachCheckpoint = true;
        platformController.fogtime = 0f;
        platformController.gametime -= 30f;
    }

}
