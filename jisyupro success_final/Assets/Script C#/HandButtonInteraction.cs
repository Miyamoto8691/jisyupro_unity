using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class HandButtonInteraction : MonoBehaviour
{
    public OVRHand hand; // 手のトラッキング
    public Camera uiCamera; // UI用のカメラ（OVRCameraRigのカメラを指定）
    public float pinchThreshold = 0.8f; // ピンチ強度の閾値

    void Update()
    {
        // ピンチが強い場合
        if (hand.GetFingerPinchStrength(OVRHand.HandFinger.Index) > pinchThreshold)
        {
            // レイを発射してUI要素を検出
            Ray ray = new Ray(hand.transform.position, hand.transform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                // UIのヒット判定を行う
                PointerEventData pointerData = new PointerEventData(EventSystem.current)
                {
                    position = uiCamera.WorldToScreenPoint(hit.point)
                };

                // ヒットしたオブジェクトに対してクリックイベントを送信
                ExecuteEvents.Execute(hit.collider.gameObject, pointerData, ExecuteEvents.pointerClickHandler);
            }
        }
    }
}
