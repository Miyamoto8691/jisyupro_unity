using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public PlatformController platformController;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("checkpoint�ʉ�");
        platformController.reachCheckpoint = true;
        platformController.fogtime = 0f;
    }
}
