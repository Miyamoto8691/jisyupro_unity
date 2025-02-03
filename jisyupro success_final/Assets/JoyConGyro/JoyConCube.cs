using UnityEngine;
using System.Collections.Generic;

public class JoyConCube : MonoBehaviour
{
	private List<Joycon> joycons;

	// Values made available via Unity
	public float[] stick;
	public Vector3 gyro;
	public Vector3 accel;
	public int jc_ind = 0;
	public Quaternion orientation;

	void Start()
	{
		gyro = new Vector3(0, 0, 0);
		accel = new Vector3(0, 0, 0);
		joycons = JoyconManager.Instance.j;
		if (joycons.Count < jc_ind + 1)
		{
			Destroy(gameObject);
		}
	}

	// Update is called once per frame
	void Update()
	{
		// make sure the Joycon only gets checked if attached
		if (joycons.Count > 0)
		{
			Joycon j = joycons[jc_ind];

			// Bボタンでセンター位置のリセット
			if (j.GetButtonDown(Joycon.Button.DPAD_DOWN))
			{
				j.Recenter();
			}

			gyro = j.GetGyro();
			accel = j.GetAccel();

			orientation = j.GetVector();
			
			Matrix4x4 rotationMatrix = Matrix4x4.Rotate(orientation);

			// x軸とz軸を入れ替えた回転行列を作成
			Matrix4x4 swappedMatrix = rotationMatrix;
			swappedMatrix.SetColumn(0, rotationMatrix.GetColumn(2)); // x軸にz軸を設定
			swappedMatrix.SetColumn(2, rotationMatrix.GetColumn(0)); // z軸にx軸を設定

			// 新しい回転行列を四元数に戻す
			Quaternion swapped = Quaternion.LookRotation(swappedMatrix.GetColumn(2), swappedMatrix.GetColumn(1));
			
			gameObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f) * Quaternion.Euler(0f, 0f, 90f) * swapped;

		}
	}
}

