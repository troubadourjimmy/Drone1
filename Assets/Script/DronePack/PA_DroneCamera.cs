using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PA_DronePack
{
    public class PA_DroneCamera : MonoBehaviour
    {
        #region Variables
        //两种相机视角，第三人称和第一人称
        public enum CameraMode { thirdPerson, firstPerson }
        //两种跟随模式 
        public enum FollowMode { firm, smooth }
        public CameraMode cameraMode = CameraMode.thirdPerson;
        public FollowMode followMode = FollowMode.firm;

        public PA_DroneController target = null;
        public Transform fpsPosition = null;

        [Range(0.03f, 0.5f)]
        public float followSmoothing = 0.1f;
        [Range(0f, 8f)]
        public float xSensitivity = 2f;
        [Range(0f, 4f)]
        public float ySensitivity = 1f;
        [Range(0f, 16f)]
        public float tiltSensitivity = 1f;

        public float height = 0.5f;
        public float distance = 1.5f;
         
        
        public float angle = 19;//相机绕x轴的旋转角度（相机视角的上下方向旋转）

        public bool findTarget = true;
        public bool autoPosition = true;
        public bool freeLook = false;//freeLook模式
        public bool findFPS = true;
        public bool invertYAxis = false;//控制摄像机视角的上下移动，当invertYAxis为false时, 鼠标Y轴向上移动是俯视，向下移动是仰视

        public List<Rigidbody> jitterRigidBodies;
        #endregion

        #region Hidden Variables
        Vector3 velocity;
        float fpsMinAngle = -20;//相机第一人称下的最小旋转角度
        float fpsMaxAngle = 70;// 相机第一人称下的最大旋转角度
        float angleV;
        float tiltForce;//鼠标滑轮，相机第一人称视角时 无人机上的摄像头角度的升降
        float turnForce;//鼠标X,相机的旋转
        float liftForce;//鼠标Y,即相机视角的上升下降
        float targetRot;//相机绕y轴的旋转角度（相机视角的左右方向的旋转）
        #endregion

        void Start()
        {
            //自动查找目标/计算距离
            #region Auto Find Targets / Calculate Distances

            //找到 GameObjet _Drone[Quad]
            if (findTarget)
            {
                //返回第一个类型为 type 的已加载的激活对象,也就是找到第一个挂载PA_DroneController脚本的Game Objet
                target = FindObjectOfType<PA_DroneController>();
                //Debug.Log("target:"+target.name);
                if (target == null)
                {
                    Debug.LogWarning("PA_DroneCamera : Could Not Find A Target");
                }
            }
            //fps view: first person view
            if (findFPS)
            {   //找到FPSView的位置，FPSView是Drone的子Object(drone上的摄像机)
                fpsPosition = GameObject.Find("FPSView").transform;
                if (fpsPosition == null)
                {
                    Debug.LogWarning("PA_DroneCamera : Could Not Find FPS Position");
                }
            }
            //开启autoPosition,在一开始运行程序时会初始化distance,height,angle,从而在最初时刻相机一个比较适宜的角度和距离
            if (autoPosition && target)
            {
                //target.transform.position是无人机的位置,transform.position是摄像机的位置（不是无人机上的摄像头位置）
                float xdist = Mathf.Abs(target.transform.position.x - transform.position.x);
                float zdist = Mathf.Abs(target.transform.position.z - transform.position.z);
                distance = (xdist > zdist) ? xdist : zdist;//以x方向的位置差和z方向中的位置差，两个值中较大的作为无人机和摄像机的distance
                Debug.Log("distance:"+distance+",xdist:"+xdist+",zdist:"+zdist);
                height = Mathf.Abs(target.transform.position.y - transform.position.y);//无人机和摄像机的高度差
                angle = transform.eulerAngles.x;
            }
            else if (!target)
            {
                Debug.LogError("PA_DroneCamera : Missing Target");
            }
            #endregion
        }

        void Update()
        {
            #region FreeLook
            /*
             * FreeLook模式：长按Alt键,移动鼠标的时候，视角不再随跟着无人机的正方向移动，而是自由在世界坐标系移动，不再以无人机坐标系为参考
             */
            if (freeLook && cameraMode == CameraMode.thirdPerson)
            {
                
                target.TurnInput(0);
            }
            #endregion
        }

        /*
           firm 模式时使用Interpolate 插值模式 使无人机的运动避免抖动(因为相机是跟随无人机的，那么也就达到了避免相机的抖动)
          所以firm 模式下无人机的运动很丝滑，没有卡顿现象，但是当我们移动相机视角的时候，会发现相机视角移动会卡顿

           smooth模式是使用 SmoothDampAngle(), SmoothDamp(), Quaternion.Lerp()对相机的运动进行平滑，避免相机抖动(相机跟随物体移动)
           所以smooth模式下相机视角的移动过程会很丝滑，但是无人机的运动会显得有一些卡顿
           
           分别实现了这两种模式以后，我想把他们结合到一起，既使无人机的运动减少抖动，然后也让相机减少抖动
           试了之后发现 无人机 向上运动是会强烈抖动.......不知道为什么?????????????????
       */
        // 在 LateUpdate中，Firm follow mode是 为了避免相机抖动(首先避免无人机抖动，因为相机是跟随无人机的，那么也就达到了避免相机的抖动)
        void LateUpdate()
        {
            #region ThirdPerson Firm

            if (freeLook && cameraMode == CameraMode.thirdPerson)
            {
                target.TurnInput(0);
            }
            if (target && followMode == FollowMode.firm && cameraMode == CameraMode.thirdPerson) 
            {
                 //Debug.Log("inter:"+ target.rigidBody.interpolation);//
                if (target.rigidBody.interpolation != RigidbodyInterpolation.Interpolate && !GetComponent<Camera>().targetTexture) 
                {
                    /*
                      把Rigidbody组件中 interpolate选项设为 Interpolate（在Interpolate中一共有三个选项）
                     物理运动以离散的时间步运行，而显卡以可变的帧率渲染，这可能导致物体抖动。对于主角或相机跟随的物体，建议使用插值。
                     Interpolate差值可以避免抖动现象
                    */
                    target.rigidBody.interpolation = RigidbodyInterpolation.Interpolate;
                }
                //liftForce使相机的视角抬升或下降，此时angle和height也会增大或减小
                //在 第三人称视角下，当height为0时 是视角和无人机在同一高度（即平视无人机）;liftForce是抬升或下降 相机视角的力（第三人称视角）
                height += (angle > -60 && angle < 60) ? liftForce * 0.03f : 0f;//height是要不断累加的
                //Debug.Log("liftForce:"+ liftForce);
                //Debug.Log("height:"+ height);//
                //Debug.Log("angle1:"+ angle);//
                
                //angle 是以x为轴的旋转角度（也就是相机视角的上下旋转的角度）；angle的大小在60和-60之间,为什么要加 liftForce，因为liftForce（向上抬升角度或向下减小角度）使angle 角度变大
                //angle 没有freeLook的条件判断,是因为无论是不是freeLook模式， 相机的上下视角的旋转是一样的
                angle = Mathf.Clamp(angle + liftForce, -60, 60);
                //Debug.Log("angle2:"+ angle);
                //当不是freeLook模式时，targetRot 是 以y 为轴的旋转角度（也就是相机视角的左右旋转,此时相机的旋转角度等于 无人机以Y轴的旋转角度）
                targetRot = (freeLook) ? targetRot + (turnForce * xSensitivity) : target.transform.eulerAngles.y;
                //Debug.Log(" turnForce:"+  turnForce);
                //Debug.Log(" targetRot:"+  targetRot);
                
                //Vector3.foward为Vector3(0,0,1)；Quaternion.Euler(0, targetRot, 0) * Vector3.forward：将Vector3(0,0,1)绕Y轴顺时针旋转targetRot
                //它绕Y轴旋转targetRot度数（顺时针），但它的Y值会一直为0;用四元数进行旋转操作时，是顺时针旋转为正方向；
                transform.position = (target.transform.position - Quaternion.Euler(0, targetRot, 0) * Vector3.forward * distance) + new Vector3(0, height, 0);
                //Debug.Log(" distance"+  distance);//
                transform.rotation = Quaternion.Euler(angle, targetRot, 0);
                foreach (Rigidbody rigidBody in jitterRigidBodies) {
                    if (rigidBody.interpolation != RigidbodyInterpolation.Interpolate && !GetComponent<Camera>().targetTexture) {
                        rigidBody.interpolation = RigidbodyInterpolation.Interpolate;
                    }
                }
            }
            #endregion
            
            #region FirstPerson Firm
            if (target && fpsPosition && cameraMode == CameraMode.firstPerson)
            {
                target.rigidBody.interpolation = RigidbodyInterpolation.Interpolate;
                transform.position = fpsPosition.position;//fpsPosition 在 start()中初始化了
                
                //titleForce是鼠标滚轮控制的，滚轮控制的镜头方向的调整是绕x轴(即镜头的上下调整)
                transform.rotation = Quaternion.Euler(target.transform.rotation.eulerAngles.x + tiltForce,
                    fpsPosition.rotation.eulerAngles.y, fpsPosition.rotation.eulerAngles.z);
                //(fpsPosition.rotation.eulerAngles.x和target.transform.rotation.eulerAngles.x是不一样的)；
                //transform.rotation = (!gyroscopeEnabled) ? Quaternion.Euler(fpsPosition.rotation.eulerAngles.x+tiltForce, fpsPosition.rotation.eulerAngles.y, fpsPosition.rotation.eulerAngles.z) : Quaternion.Euler(tiltForce, fpsPosition.rotation.eulerAngles.y, 0);
                //fps的旋转角度，在上下角度上时根据 鼠标滑轮去调整， 其他方向的旋转和 无人机的角度是一致的
                fpsPosition.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, target.transform.rotation.eulerAngles.y, target.transform.rotation.eulerAngles.z);
                foreach (Rigidbody rigidBody in jitterRigidBodies)
                {
                    rigidBody.interpolation = RigidbodyInterpolation.Interpolate;
                } 
            }
            #endregion
            
            #region Third person smooth
            #endregion
        }

       
        void FixedUpdate()
        {
             #region ThirdPerson Smooth
            //Debug.Log("inter:"+ target.rigidBody.interpolation);
             if (freeLook && cameraMode == CameraMode.thirdPerson) { target.TurnInput(0); }
             if (target && followMode == FollowMode.smooth && cameraMode == CameraMode.thirdPerson)
             {
                  //在smooth模式下不使用interpolation插值
                  if (target.rigidBody.interpolation != RigidbodyInterpolation.None && !GetComponent<Camera>().targetTexture)
                  {
                      target.rigidBody.interpolation = RigidbodyInterpolation.None;
                  }
                  // if (target.rigidBody.interpolation != RigidbodyInterpolation.Interpolate && !GetComponent<Camera>().targetTexture)
                 // {
                 //     target.rigidBody.interpolation = RigidbodyInterpolation.Interpolate;
                 // }
                 
                 height += (angle > -60 && angle < 60) ? liftForce * 0.03f : 0f;
                 angle = Mathf.Clamp(angle + liftForce, -60, 60);
                 targetRot = (freeLook) ? targetRot + (turnForce * xSensitivity) : target.transform.eulerAngles.y;
                 
                 /*
                  * public static float SmoothDampAngle (float current, float target, ref float currentVelocity, float smoothTime);
                  * 随时间推移将以度为单位给定的角度逐渐改变为所需目标角度。
                  * current	当前位置。
                    target	尝试达到的目标。
                    currentVelocity	当前速度，此值由函数在每次调用时进行修改。
                    smoothTime	达到目标所需的近似时间。值越小，达到目标的速度越快
                 */
                 float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRot, ref angleV, (followSmoothing * Time.fixedDeltaTime) * 60f);
                 Vector3 desiredPos = (target.transform.position - Quaternion.Euler(0, smoothAngle, 0) * Vector3.forward * distance) + new Vector3(0, height, 0);
                 
                 /*
                  * public static Vector3 SmoothDamp (Vector3 current, Vector3 target, ref Vector3 currentVelocity, float smoothTime, float maxSpeed= Mathf.Infinity, float deltaTime= Time.deltaTime);
                    随时间推移将一个向量逐渐改变为所需目标。向量通过某个类似于弹簧-阻尼的函数（它从不超过目标）进行平滑。 最常见的用法是用于平滑跟随摄像机。
                    current	当前位置。
                    target	尝试达到的目标。
                    currentVelocity	当前速度，此值由函数在每次调用时进行修改。
                    smoothTime	达到目标所需的近似时间。值越小，达到目标的速度越快。
                    maxSpeed	可以选择允许限制最大速度。
                    deltaTime	自上次调用此函数以来的时间。默认情况下为 Time.deltaTime。
                  */
                 transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref velocity, (followSmoothing * Time.fixedDeltaTime) * 60f);
                 
                 /*
                  * public static Quaternion Lerp (Quaternion a, Quaternion b, float t);
                  * 在 a 和 b 之间插入 t，然后对结果进行标准化处理( 在 a 与 b 之间按 t 进行线性插值)。参数 t 被限制在 [0, 1] 范围内。
                  */
                 transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(angle, targetRot, 0), (followSmoothing * Time.fixedDeltaTime) * 60f);
                 foreach (Rigidbody rigidBody in jitterRigidBodies)
                 {
                     if (rigidBody.interpolation != RigidbodyInterpolation.None && !GetComponent<Camera>().targetTexture)
                     {
                         rigidBody.interpolation = RigidbodyInterpolation.None;
                     }
                 }
             }
             #endregion
        }

        #region Custom Functions
        public void ChangeCameraMode() { cameraMode = (cameraMode == CameraMode.firstPerson) ? CameraMode.thirdPerson : CameraMode.firstPerson; }
        public void ChangeFollowMode() { followMode = (followMode == FollowMode.smooth) ? FollowMode.firm : FollowMode.smooth; }
        //public void ChangeGyroscope() { gyroscopeEnabled = !gyroscopeEnabled; }
        //此处计算liftForce,由于是input为参数，有相关的输入，那么会直接触发这个函数
        public void LiftInput(float input)
        {
            liftForce = (!invertYAxis) ? input * ySensitivity : -input * ySensitivity;
            
        }
        public void TurnInput(float input) { turnForce = input * xSensitivity; }

        /*
              fpsMinAngle 设为-20, fpsMaxAngle 设为 70
             鼠标滑轮向后滑动（镜头向下旋转),input是负的，总角度(titleForce)为正
             鼠标滑轮向前滑动(镜头向上旋转),input 是正的,总角度(titleForce)为负
        */
        public void TiltInput(float input)
        {   
            tiltForce = Mathf.Clamp(tiltForce + (-input * tiltSensitivity), fpsMinAngle, fpsMaxAngle);
        }
        public void CanFreeLook(bool state) { freeLook = state; }
        public void XSensitivity(float value) { xSensitivity = value; }
        public void YSensitivity(float value) { ySensitivity = value; }
        public void InvertYAxis(bool state) { invertYAxis = state; }
        #endregion
    }
}