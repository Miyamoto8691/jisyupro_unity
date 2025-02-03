using UnityEngine;

public class Pocketwatch : MonoBehaviour
{
    public PlatformController platformController;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnTriggerEnter(Collider other)
    {
        if (other == platformController.foundation)
        {
            return; //�����foundation�ɓ����������̓X�L�b�v
        }
        Debug.Log("pocketwatch�ʉ�");
        platformController.reachCheckpoint = true;
        platformController.fogtime = 0f;
        platformController.gametime -= 30f;
    }

}
