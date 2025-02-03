using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using TMPro;
using UnityEditor;


public class PlatformController : MonoBehaviour
{
    private bool firsthori = false;
    public float rotationSpeed = 45f;
    public float fogtime = 0f;
    public AudioSource audioSource;
    public Transform cameraTransform;
    private Vector3 localCamPosition;
    private int hintvib = 0;
    public CapsuleCollider cameraCollider;
    public BoxCollider foundation;
    public BoxCollider frame1;
    public BoxCollider frame2;
    public BoxCollider frame3;
    public BoxCollider frame4;
    public Canvas canvas;
    public Button clockwise1Button;
    public Button clockwise2Button;
    public Button counterclockwise1Button;
    public Button counterclockwise2Button;

    public GameObject Maze;
    public GameObject rotatewall;
    public GameObject checkpoint;
    public GameObject checkpointFrame;
    private Coroutine rotateCoroutine;
    private static System.Random random = new System.Random();
    public bool reachCheckpoint = true;
    public int checktimes = 0;
    private int clearpoint = 10;
    private bool isCooldownActive = false; //チャタリング防止のクールダウン
    private float cooldownTimer = 0f;

    private SerialPort serialPort;

    public float FBranchtime = 0f;
    private bool reachFBranch = false;
    private float reedFtime = 0f;
    private bool reedF = false;
    public float GBranchtime = 0f;
    private bool reachGBranch = false;
    private float reedGtime = 0f;
    private bool reedG = false;
    public float HBranchtime = 0f;
    private bool reachHBranch = false;
    private float reedHtime = 0f;
    private bool reedH = false;
    public float IBranchtime = 0f;
    private bool reachIBranch = false;
    private float reedItime = 0f;
    private bool reedI = false;
    public float JBranchtime = 0f;
    private bool reachJBranch = false;
    private float reedJtime = 0f;
    private bool reedJ = false;


    private List<Joycon> joycons;
    private Joycon joyconR;
    private Joycon joyconL;
	public float[] stick;
	public Vector3 gyro;
	public Vector3 accel;
	public int jc_ind = 0;
	public Quaternion orientation;

    private Queue<Quaternion> rotationQueue = new Queue<Quaternion>(); //移動平均(LPF)をとるための配列
    private const int maxQueueSize = 10;

    public PostProcessVolume postProcessVolume;
    private ChromaticAberration chromaticAberration;
    private ColorGrading colorGrading;
    private int x;
    private int z;
    private int prex;
    private int prez;

    public Camera camera;
    public Canvas cameracanvas;
    public GameObject textObject;
    public TMP_Text elapsedTimeText;
    public float gametime = 0f;
    private float timelimit = 300f;

    public GameObject pocketwatch;
    private int Wx;
    private int Wz;
    public bool reachPocketwatch = true;
    private bool descendWatch = false;

    public GameObject CameraSphere;
    public GameObject OVRCamerarig;
    
   
    void Start()
    {
        serialPort = new SerialPort("COM3", 9600); //Windows右のUSB
        try
        {
            serialPort.Open();
            Debug.Log("Serial port opened successfully.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to open serial port: {e.Message}");
        }

        clockwise1Button.onClick.RemoveAllListeners();
        clockwise1Button.onClick.AddListener(() =>
        {
            RotateCube(90f);
            SendSerialCommand('c');
        });

        clockwise2Button.onClick.RemoveAllListeners();
        clockwise2Button.onClick.AddListener(() =>
        {
            RotateCube(90f);
            SendSerialCommand('c');
        });

        counterclockwise1Button.onClick.RemoveAllListeners();
        counterclockwise1Button.onClick.AddListener(() =>
        {
            RotateCube(-90f);
            SendSerialCommand('a');
        });

        counterclockwise2Button.onClick.RemoveAllListeners();
        counterclockwise2Button.onClick.AddListener(() =>
        {
            RotateCube(-90f);
            SendSerialCommand('a');
        });

        gyro = new Vector3(0, 0, 0);
		accel = new Vector3(0, 0, 0);
		joycons = JoyconManager.Instance.j;
		if (joycons.Count < jc_ind + 1)
		{
			Destroy(gameObject);
		}
        joyconL = joycons.Find( c =>  c.isLeft );
        joyconR = joycons.Find( c => !c.isLeft );



        if (postProcessVolume != null)
        {
            postProcessVolume.profile.TryGetSettings(out chromaticAberration);
            if (chromaticAberration != null)
            {
                chromaticAberration.intensity.value = 0f; 
            }

            postProcessVolume.profile.TryGetSettings(out colorGrading);
            if (colorGrading != null)
            {
                colorGrading.active = false; 
            }
        }
        else
        {
            Debug.LogWarning("PostProcessVolume is not assigned.");
        }
        
        textObject.SetActive(false);

    }

    void Update()
    {
        float camrotationY = 0f;

        if (joycons.Count > 0)
		{
			Joycon j = joyconR;

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
			
			//Maze.transform.rotation = Quaternion.Euler(90f, 0f, 0f) * Quaternion.Euler(0f, 0f, 90f) * swapped;

            //前10フレームの移動平均をとる
            Quaternion joyrotation = Quaternion.Euler(90f, 0f, 0f) * Quaternion.Euler(0f, 0f, 90f) * swapped;
            rotationQueue.Enqueue(joyrotation);
            if (rotationQueue.Count > maxQueueSize)
            {
                rotationQueue.Dequeue();
            }
            Quaternion averageRotation = MovingAverage();
            Maze.transform.rotation = averageRotation;

            // 最初に水平がとれるまでボールをリリースしない
            if (Math.Abs(Maze.transform.rotation.eulerAngles.x) < 20.0f && Math.Abs(Maze.transform.rotation.eulerAngles.z) < 20.0f)
            {
                firsthori = true;
            }

            if (!firsthori)
            {
                cameraTransform.position = new Vector3(-1.0f, 11.26f, -9.0f);
            }
        }

        if (firsthori)
        {
            gametime += Time.deltaTime;
        }
        elapsedTimeText.text = $"Time: {timelimit - gametime:F1} s\n{clearpoint +1 - checktimes} more to clear";





        if (Input.GetKey(KeyCode.RightArrow)) camrotationY = rotationSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.LeftArrow)) camrotationY = -rotationSpeed * Time.deltaTime;

        cameraTransform.Rotate(0f, camrotationY, 0f, Space.World);
        AdjustCameraPosition();

        if (reachCheckpoint && !isCooldownActive)
        {
            reachCheckpoint = false;
            checktimes += 1;
            isCooldownActive = true;
            checkpoint.transform.localPosition = RandomCheckPoint();
            checkpointFrame.transform.localPosition = checkpoint.transform.localPosition;
        }

        if (isCooldownActive)
        {
            cooldownTimer += Time.deltaTime;
            if (cooldownTimer >= 0.3f)
            {
                reachCheckpoint = false;
                isCooldownActive = false;
                cooldownTimer = 0f;
            }
        }
        
        fogtime += Time.deltaTime;
        RenderSettings.fogDensity = density(fogtime);
        audioSource.volume = density(fogtime);

        localCamPosition = Maze.transform.InverseTransformPoint(cameraTransform.position);

        // y座標をローカル座標上で0にするとかなり安定するが、傾き変化に対する慣性がやや不自然?
        localCamPosition.y = 0f;
        cameraTransform.position = Maze.transform.TransformPoint(localCamPosition);
        
        if (localCamPosition.x > checkpoint.transform.localPosition.x + 0.5f
        && localCamPosition.z > checkpoint.transform.localPosition.z + 0.5f
        && hintvib != 1)
        {
            SendSerialCommand('p');
            StopCoroutine(RForwardVib());
            StopCoroutine(LBackVib());
            StopCoroutine(RBackVib());
            StartCoroutine(LForwardVib());
        }
        if (localCamPosition.x > checkpoint.transform.localPosition.x + 0.5f
        && localCamPosition.z < checkpoint.transform.localPosition.z - 0.5f
        && hintvib != 2)
        {
            SendSerialCommand('q');
            StopCoroutine(LForwardVib());
            StopCoroutine(LBackVib());
            StopCoroutine(RBackVib());
            StartCoroutine(RForwardVib());
        }
        if (localCamPosition.x < checkpoint.transform.localPosition.x - 0.5f
        && localCamPosition.z > checkpoint.transform.localPosition.z + 0.5f
        && hintvib != 3)
        {
            SendSerialCommand('r');
            StopCoroutine(LForwardVib());
            StopCoroutine(RForwardVib());
            StopCoroutine(RBackVib());
            StartCoroutine(LBackVib());
        }
        if (localCamPosition.x < checkpoint.transform.localPosition.x - 0.5f
        && localCamPosition.z < checkpoint.transform.localPosition.z - 0.5f
        && hintvib != 4)
        {
            SendSerialCommand('s');
            StopCoroutine(LForwardVib());
            StopCoroutine(RForwardVib());
            StopCoroutine(LBackVib());
            StartCoroutine(RBackVib());
        }

        // 分岐をきちんと通れたか判定
        if (Math.Abs(localCamPosition.x - (-3.0f)) < 0.3f
        && Math.Abs(localCamPosition.z - (-9.0f)) < 0.3f)
        {
            reachFBranch = true;
        }
        if(reachFBranch)
        {
            FBranchtime += Time.deltaTime;
            if(FBranchtime > 3.0f)
            {
                reachFBranch = false;
                FBranchtime = 0f;
            }
        }
        
        if (Math.Abs(localCamPosition.x - 3.0f) < 0.3f
        && Math.Abs(localCamPosition.z - (-9.0f)) < 0.3f)
        {
            reachGBranch = true;
        }
        if(reachGBranch)
        {
            GBranchtime += Time.deltaTime;
            if(GBranchtime > 3.0f)
            {
                reachGBranch = false;
                GBranchtime = 0f;
            }
        }

        if (Math.Abs(localCamPosition.x - 5.0f) < 0.3f
        && Math.Abs(localCamPosition.z - (-5.0f)) < 0.3f)
        {
            reachHBranch = true;
        }
        if(reachHBranch)
        {
            HBranchtime += Time.deltaTime;
            if(HBranchtime > 3.0f)
            {
                reachHBranch = false;
                HBranchtime = 0f;
            }
        }

        if (Math.Abs(localCamPosition.x - (-5.0f)) < 0.3f
        && Math.Abs(localCamPosition.z - (-1.0f)) < 0.3f)
        {
            reachIBranch = true;
        }
        if(reachIBranch)
        {
            IBranchtime += Time.deltaTime;
            if(IBranchtime > 3.0f)
            {
                reachIBranch = false;
                IBranchtime = 0f;
            }
        }

        if (Math.Abs(localCamPosition.x - 1.0f) < 0.3f
        && Math.Abs(localCamPosition.z - 7.0f) < 0.3f)
        {
            reachJBranch = true;
        }
        if(reachJBranch)
        {
            JBranchtime += Time.deltaTime;
            if(JBranchtime > 3.0f)
            {
                reachJBranch = false;
                JBranchtime = 0f;
            }
        }

        // シリアルコマンドを読む
        char readCommand = ReadSerialCommand();
        if((readCommand == 'f' || Input.GetKey(KeyCode.F)) && firsthori)
        {
            reedF = true;
        }
        if(reedF)
        {
            reedFtime += Time.deltaTime;
            if(reedFtime <= 3.0f && reachFBranch)
            {
                reedF = false;
                reedFtime = 0f;
            }
            else if(reedFtime > 3.0f)
            {
                // Unity上で分岐を前後３秒以内に通れていなければ位置修正
                reedF = false;
                reedFtime = 0f;
                Debug.Log("Misalignment at branch F: Corrected position.");
                StartCoroutine(Warp(new Vector3(-3.0f, 0f, -9.0f)));
            }
        }

        if(readCommand == 'g')
        {
            reedG = true;
        }
        if(reedG)
        {
            reedGtime += Time.deltaTime;
            if(reedGtime <= 3.0f && reachGBranch)
            {
                reedG = false;
                reedGtime = 0f;
            }
            else if(reedGtime > 3.0f)
            {
                // Unity上で分岐を前後３秒以内に通れていなければ位置修正
                reedG = false;
                reedGtime = 0f;
                Debug.Log("Misalignment at branch G: Corrected position.");
                StartCoroutine(Warp(new Vector3(3.0f, 0f, -9.0f)));
            }
        }

        if(readCommand == 'h')
        {
            reedH = true;
        }
        if(reedH)
        {
            reedHtime += Time.deltaTime;
            if(reedHtime <= 3.0f && reachHBranch)
            {
                reedH = false;
                reedHtime = 0f;
            }
            else if(reedHtime > 3.0f)
            {
                // Unity上で分岐を前後３秒以内に通れていなければ位置修正
                reedH = false;
                reedHtime = 0f;
                Debug.Log("Misalignment at branch H: Corrected position.");
                StartCoroutine(Warp(new Vector3(5.0f, 0f, -5.0f)));
            }
        }

        if(readCommand == 'i')
        {
            reedI = true;
        }
        if(reedI)
        {
            reedItime += Time.deltaTime;
            if(reedItime <= 3.0f && reachIBranch)
            {
                reedI = false;
                reedItime = 0f;
            }
            else if(reedItime > 3.0f)
            {
                // Unity上で分岐を前後３秒以内に通れていなければ位置修正
                reedI = false;
                reedItime = 0f;
                Debug.Log("Misalignment at branch I: Corrected position.");
                StartCoroutine(Warp(new Vector3(-5.0f, 0f, -1.0f)));
            }
        }

        if(readCommand == 'j')
        {
            reedJ = true;
        }
        if(reedJ)
        {
            reedJtime += Time.deltaTime;
            if(reedJtime <= 3.0f && reachJBranch)
            {
                reedJ = false;
                reedJtime = 0f;
            }
            else if(reedJtime > 3.0f)
            {
                // Unity上で分岐を前後３秒以内に通れていなければ位置修正
                reedJ = false;
                reedJtime = 0f;
                Debug.Log("Misalignment at branch J: Corrected position.");
                StartCoroutine(Warp(new Vector3(1.0f, 0f, 7.0f)));
            }
        }

        if (checktimes == clearpoint)
        {
            Renderer checkpointRenderer = checkpoint.GetComponent<Renderer>();
            if (checkpointRenderer != null)
            {
                checkpointRenderer.material.color = Color.green;
            }
            else
            {
                Debug.LogWarning("Renderer not found on checkpoint.");
            }
        }
        if (checktimes > clearpoint)
        {
            #if UNITY_EDITOR
            // エディタでの動作中にアプリを停止するには、以下を使用
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            // 実行ファイルでの動作中にアプリを終了
            Application.Quit();
            #endif
        }

        if(gametime > timelimit)
        {
            #if UNITY_EDITOR
            // エディタでの動作中にアプリを停止するには、以下を使用
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            // 実行ファイルでの動作中にアプリを終了
            Application.Quit();
            #endif
        }

        cameracanvas.transform.position = camera.transform.position + camera.transform.forward * 0.4f;
        cameracanvas.transform.rotation = camera.transform.rotation;

        if (reachPocketwatch)
        {
            pocketwatch.transform.localPosition = new Vector3(0f, 2f, 0f);
        }

        if ((checktimes == Math.Ceiling((float)clearpoint * 1/4) || checktimes == Math.Ceiling((float)clearpoint/2) 
        || checktimes == Math.Ceiling((float)clearpoint * 3/4)) && reachPocketwatch)
        {
            reachPocketwatch = false;
            descendWatch = false;
            pocketwatch.transform.localPosition = RandomPocketwatch();
        }
        if ((checktimes == Math.Ceiling((float)clearpoint * 1/4) || checktimes == Math.Ceiling((float)clearpoint/2) 
        || checktimes == Math.Ceiling((float)clearpoint * 3/4)) && !reachPocketwatch)
        {
            if (Math.Abs(localCamPosition.x - pocketwatch.transform.localPosition.x) +
            Math.Abs(localCamPosition.z - pocketwatch.transform.localPosition.z) <= 9.0f)
            {
                descendWatch = true;
            }
            if (descendWatch)
            {
                pocketwatch.transform.localPosition -= new Vector3(0f, Time.deltaTime/10, 0f);
            }
            
        }
        if ((checktimes != Math.Ceiling((float)clearpoint * 1/4) && checktimes != Math.Ceiling((float)clearpoint/2) 
        && checktimes != Math.Ceiling((float)clearpoint * 3/4)) && !reachPocketwatch)
        {
            reachPocketwatch = true;
            descendWatch = false;
        }
        
        pocketwatch.transform.localRotation = Quaternion.Euler(0f, gametime * 18.0f , 0f);

        //追従
        OVRCamerarig.transform.position = CameraSphere.transform.position;
        
           
    }

    private Quaternion MovingAverage()
    {
        Quaternion average = rotationQueue.Peek();
        foreach (Quaternion q in rotationQueue)
        {
            average = Quaternion.Slerp(average, q, 1.0f/rotationQueue.Count);
        }
        return average;
    }

    public float density(float time)
    {
        if (time <= 40f)
        {
            //return (1.0f + (float)Math.Sin((time - 20f) * Math.PI /40f))/ 2.0f;
            return 0.75f * (1.0f + (float)Math.Sin((time - 20f) * Math.PI / 40f)) / 2.0f;
        }
        else
        {
            //return 1.0f;
            return 0.75f;
        }
    }

    void AdjustCameraPosition()
    {
        Collider checkpointCollider = checkpoint.GetComponent<Collider>();
        Collider pocketwatchCollider = pocketwatch.GetComponent<Collider>();
        Collider[] overlappingColliders = Physics.OverlapCapsule(
            cameraCollider.bounds.center + Vector3.up * (cameraCollider.height / 2 - cameraCollider.radius),
            cameraCollider.bounds.center - Vector3.up * (cameraCollider.height / 2 - cameraCollider.radius),
            cameraCollider.radius);

        foreach (var collider in overlappingColliders)
        {
            if (collider == foundation)
            {
                //法線方向に位置を修正
                Vector3 closestPoint = foundation.ClosestPoint(cameraTransform.position);
                Vector3 normal = (cameraTransform.position - closestPoint).normalized;
                cameraTransform.position += normal * 0.2f;
                Debug.Log("Detect penetration: Corrected position.");
                break;
            }
            if (collider == frame1)
            {
                Vector3 closestPoint = frame1.ClosestPoint(cameraTransform.position);
                Vector3 normal = (cameraTransform.position - closestPoint).normalized;
                cameraTransform.position += normal * 0.52f;
                Debug.Log("Detect penetration: Corrected position.");
                break;
            }
            if (collider == frame2)
            {
                Vector3 closestPoint = frame2.ClosestPoint(cameraTransform.position);
                Vector3 normal = (cameraTransform.position - closestPoint).normalized;
                cameraTransform.position += normal * 0.52f;
                Debug.Log("Detect penetration: Corrected position.");
                break;
            }
            if (collider == frame3)
            {
                Vector3 closestPoint = frame3.ClosestPoint(cameraTransform.position);
                Vector3 normal = (cameraTransform.position - closestPoint).normalized;
                cameraTransform.position += normal * 0.52f;
                Debug.Log("Detect penetration: Corrected position.");
                break;
            }
            if (collider == frame4)
            {
                Vector3 closestPoint = frame4.ClosestPoint(cameraTransform.position);
                Vector3 normal = (cameraTransform.position - closestPoint).normalized;
                cameraTransform.position += normal * 0.52f;
                Debug.Log("Detect penetration: Corrected position.");
                break;
            }
            if (collider == checkpointCollider && !reachCheckpoint)
            {
                reachCheckpoint = true;
                fogtime = 0f;
                break;
            }
            if (collider == pocketwatchCollider)
            {
                reachPocketwatch = true;
                gametime -= 30f;
                fogtime = 0f;
                break;
            }
        }
    }

    void RotateCube(float angle)
    {
        if (rotateCoroutine != null)
        {
            StopCoroutine(rotateCoroutine);
        }

        rotateCoroutine = StartCoroutine(SmoothMove(rotatewall, angle, 3.0f));
    }

    private IEnumerator SmoothMove(GameObject cylinder, float rotateangle, float duration)
    {
        HideButtons();
        Quaternion startRotation = cylinder.transform.localRotation;
        Quaternion targetRotation = startRotation * Quaternion.Euler(0f, rotateangle, 0f);
        Quaternion startRotation1 = canvas.transform.localRotation;
        Quaternion targetRotation1 = startRotation1 * Quaternion.Euler(0f, rotateangle, 0f);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cylinder.transform.localRotation = Quaternion.Lerp(startRotation, targetRotation, elapsed / duration);
            canvas.transform.localRotation = Quaternion.Lerp(startRotation1, targetRotation1, elapsed / duration);
            yield return null;
        }
        ShowButtons();
    }

    private void ShowButtons()
    {
        clockwise1Button.gameObject.SetActive(true);
        clockwise2Button.gameObject.SetActive(true);
        counterclockwise1Button.gameObject.SetActive(true);
        counterclockwise2Button.gameObject.SetActive(true);
    }
    private void HideButtons()
    {
        clockwise1Button.gameObject.SetActive(false);
        clockwise2Button.gameObject.SetActive(false);
        counterclockwise1Button.gameObject.SetActive(false);
        counterclockwise2Button.gameObject.SetActive(false);
    }

    void SendSerialCommand(char command)
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            try
            {
                serialPort.Write(command.ToString());
                Debug.Log($"Sent command: {command}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to send command: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("Serial port is not open.");
        }
    }
    char ReadSerialCommand()
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            try
            {
                if (serialPort.BytesToRead > 0) // 受信があるときだけ読まないとSendSerialCommandとブロックしてしまう
                {
                    string line = serialPort.ReadExisting(); // 最初ReadLineを使ったが受信してない時にブロックしてしまう
                    if (!string.IsNullOrEmpty(line))
                    {
                        return line[0];
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to read command: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("Serial port is not open.");
        }
        return '\0';
    }
    public Vector3 RandomCheckPoint()
    {
        random = new System.Random(); //毎度違うインスタンスを作って擬似乱数のパターンを変える
        while (checktimes <= clearpoint)
        {
            x = random.Next(-7,8);
            z = random.Next(-11,12);
            if(x%2==0 || z%2==0)
            {
                continue;
            }
            if(x >= -3 && x <= 3 && z >= -3 && z <= 3)
            {
                continue;
            }
            if(Math.Abs(x - prex) + Math.Abs(z - prez) <= 3)
            {
                continue;
            }
            break;
        }
        prex = x;
        prez = z;
        return new Vector3(x, 0, z);
    }

    public Vector3 RandomPocketwatch()
    {
        random = new System.Random(); 
        while (true)
        {
            Wx = random.Next(-7,8);
            Wz = random.Next(-11,12);
            if(Wx%2==0 || Wz%2==0)
            {
                continue;
            }
            if(Wx >= -3 && Wx <= 3 && Wz >= -3 && Wz <= 3)
            {
                continue;
            }
            if(Math.Abs(Wx - localCamPosition.x) + Math.Abs(Wz - localCamPosition.z) <= 10)
            {
                continue;
            }
            break;
        }
        return new Vector3(Wx, 0, Wz);

    }

    private IEnumerator LForwardVib()
    {
        hintvib = 1;
        for (int i = 0; i < 10; i++)
        {
            joyconL.SetRumble(0.0f, 320.0f, 0.65f, 20);
            yield return new WaitForSeconds(0.02f);
            joyconL.SetRumble(0.0f, 160.0f, 0.4f, 50);
            yield return new WaitForSeconds(0.05f);
        }
    }
    private IEnumerator RForwardVib()
    {
        hintvib = 2;
        for (int i = 0; i < 10; i++)
        {
            joyconR.SetRumble(0.0f, 320.0f, 0.65f, 20);
            yield return new WaitForSeconds(0.02f);
            joyconR.SetRumble(0.0f, 160.0f, 0.4f, 50);
            yield return new WaitForSeconds(0.05f);
        }
    }
    private IEnumerator LBackVib()
    {
        hintvib = 3;
        for (int i = 0; i < 10; i++)
        {
            joyconL.SetRumble(0.0f, 160.0f, 0.3f, 20);
            yield return new WaitForSeconds(0.02f);
            joyconL.SetRumble(0.0f, 100.0f, 0.3f, 50);
            yield return new WaitForSeconds(0.05f);
        }
    }
    private IEnumerator RBackVib()
    {
        hintvib = 4;
        for (int i = 0; i < 10; i++)
        {
            joyconR.SetRumble(0.0f, 160.0f, 0.3f, 20);
            yield return new WaitForSeconds(0.02f);
            joyconR.SetRumble(0.0f, 100.0f, 0.3f, 50);
            yield return new WaitForSeconds(0.05f);
        }
    }

    private IEnumerator Warp(Vector3 targetposition)
    {
        Vector3 startposition = localCamPosition;
        float elapsed = 0f;
        gametime += 10.0f;

        if (chromaticAberration != null)
        {
            chromaticAberration.intensity.value = 1f;
        }
        if (colorGrading != null)
        {
            colorGrading.active = true;
        }

        while (elapsed < 1.0f)
        {
            elapsed += Time.deltaTime;
            cameraTransform.position = Maze.transform.TransformPoint(Vector3.Lerp(startposition, targetposition, elapsed / 1.0f));
            yield return null;
        }
        cameraTransform.position = Maze.transform.TransformPoint(targetposition);

        if (chromaticAberration != null)
        {
            chromaticAberration.intensity.value = 0f;
        }
        if (colorGrading != null)
        {
            colorGrading.active = false;
        }
        textObject.SetActive(true);
        while(elapsed < 4.0f)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        textObject.SetActive(false);
    }


    void OnDestroy()
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            SendSerialCommand('e');
            serialPort.Close();
            Debug.Log("Initialize rotation. Serial port closed.");
        }
    }
}


