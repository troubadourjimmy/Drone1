using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PA_DronePack
{
    public class PA_DroneController : MonoBehaviour
    {
        #region Movement Values
        [Tooltip("sets the drone's max forward speed")]//设置无人机的最大向前运动速度
        public float forwardSpeed = 7f;
        [Tooltip("sets the drone's max backward speed")]////设置无人机的最大向后运动速度
        public float backwardSpeed = 5f;
        [Tooltip("sets the drone's max left strafe speed")]//设置无人机的最大向左运动速度
        public float rightSpeed = 5f;
        [Tooltip("sets the drone's max right strafe speed")]////设置无人机的最大向又运动速度
        public float leftSpeed = 5f;
        [Tooltip("sets the drone's max rise speed")]//设置无人机的最大上升速度
        public float riseSpeed = 5f;
        [Tooltip("sets the drone's max lower speed")]//设置无人机的最大下降速度
        public float lowerSpeed = 5f;
        [Tooltip("how fast the drone speeds up")]//设置无人机的加速度（加速时）
        public float acceleration = 0.5f;
        [Tooltip("how fast the drone slows down")]//设置无人机的加速度(减速时)
        public float deceleration = 0.2f;
        [Tooltip("how eaisly the drone is affected by outside forces")]//设置无人机的稳定性(受外力的影响程度)
        public float stability = 0.1f;
        [Tooltip("how fast the drone rotates")]///设置无人机的旋转速度(角速度)
        public float turnSensitivty = 2f;

        [Tooltip("states whether or not the drone active on start")]//无人机在启动时是否处于活动状态
        public bool motorOn = true;
        #endregion

        #region Appearance
        [Tooltip("assign drone's propellers to this array")]//将无人机的螺旋桨分配给该数组
        public List<GameObject> propellers;
        public GameObject drone;
        [Tooltip("set propellers max spin speed")]//设置螺旋桨最大旋转速度
        public float propSpinSpeed = 50;
        [Range(0, 1f)]
        [Tooltip("how fast the propellers slow down")]//螺旋桨减速的速度有多快
        public float propStopSpeed = 1f;
        [Tooltip("the transform/location used to tilt the drone forward")]//用于使无人机向前倾斜的transform（无人机向前运动时会产生倾斜）
        public Transform frontTilt;
        [Tooltip("the transform/location used to tilt the drone backward")]//用于使无人机向后倾斜的transform
        public Transform backTilt;
        [Tooltip("the transform/location used to tilt the drone right")]//用于使无人机向右倾斜的transform
        public Transform rightTilt;
        [Tooltip("the transform/location used to tilt the drone left")]//用于使无人机向左倾斜的transform
        public Transform leftTilt;
        #endregion

        #region Collision Settings
         
        #endregion

        #region Sound Effects
        #endregion

        #region Read Only Variables
        [Tooltip("displays the collision force of the last impact")]//显示最后一次撞击的碰撞力
        public float collisionMagnitude;
        [Tooltip("displays the current force lifting up the drone")]//显示当前举起无人机的力
        public float liftForce;
        [Tooltip("displays the current force driving the drone")]//显示当前驱动无人机前后运动的力
        public float driveForce;
        [Tooltip("displays the current force strafing the drone")]//显示当前驱动无人机左右运动的力
        public float strafeForce;
        [Tooltip("displays the current force turning the drone")]//显示当前转动无人机的力(鼠标转动无人机的方向)
        public float turnForce;
        [Tooltip("displays the drone's distance from the ground")]//显示无人机与地面的距离
        public float groundDistance = Mathf.Infinity;
        [Tooltip("displays the drones distance from being upright")]//显示无人机当前角度与它本身直立时的距离
        public float uprightAngleDistance;
        [Tooltip("displays the current propeller speed")]//显示当前的螺旋桨速度
        public float calPropSpeed = 0;
        [Tooltip("displays drone's starting position")]//显示无人机的起始位置
        public Vector3 startPosition;
        [Tooltip("displays the drone's rotational position")]//显示无人机的旋转位置
        public Quaternion startRotation;
        #endregion

        #region Private Functions
        private float driveInput = 0;//使无人机向前和向后运动的动力输入
        private float strafeInput = 0;//使无人机向左和向右运动的动力输入
        private float liftInput = 0;//我认为是向上运动的动力输入
        public Rigidbody rigidBody;//无人机刚体属性
        private RaycastHit hit;//用于从射线投射获取信息的结构
        private Collider coll;//无人机碰撞属性
        private bool oGrav;//是否启用刚体的重力属性
        private float oDrag;//刚体的阻力属性
        private float oADrag;//刚体的角阻力属性
        #endregion

        void Awake()
        {
            #region Cache Components & Start Values
            coll = GetComponent<Collider>();        // 无人机的collider
            rigidBody = GetComponent<Rigidbody>();  // 无人机的 rigidbody
            startPosition = transform.position;     // 无人机的初始位置
            startRotation = transform.rotation;     // 无人机的初始角度
            oGrav = rigidBody.useGravity;           // 无人机的刚体重力属性
            oDrag = rigidBody.drag;                 // 无人机的刚体阻力
            oADrag = rigidBody.angularDrag;         // 无人机的刚体角阻力
            drone = GameObject.Find("_Drone [Quad]");

            #endregion
        }

        private void Start()
        {
            #region errorcheck
            #endregion
        }

        void Update()
        {
            //浮点计算、螺旋桨 
            #region Float Calculations, Propellers
            //uprightAngleDistance是无人机与直立的距离;transform.up是 drone自身坐标系的y轴在世界坐标系中的位置
            uprightAngleDistance = (1f - transform.up.y) * 0.5f; //是无人机从正面向上的距离
            uprightAngleDistance = (uprightAngleDistance < 0.001) ? 0f : uprightAngleDistance; // 如果value<0.001,value =0; 否则value维持原值不变
            
            /* Physics.Raycast(): 向场景中的所有碰撞体投射一条射线，该射线起点为 /origin/，朝向 /direction/，长度为 /maxDistance/。
               如果射线与任何碰撞体相交，返回 true，否则返回 false.
               如果返回 true，则 hitInfo 将包含有关最近的碰撞体的命中位置的更多信息。
            */
            if (Physics.Raycast(transform.position, Vector3.down, out hit, Mathf.Infinity)) 
            { groundDistance = hit.distance; }  //测量无人机与地面/固体物体的距离(报告当前对象与报告的碰撞体之间的距离)
            
            /*
             计算螺旋桨应旋转的速度；
             也就是当motor开着的时候，螺旋桨转速为最大转速；
             当motor一旦停止，螺旋桨开始慢慢减速；注意propStopSpeed的取值范围是[0,1]
             propSpinSpeed螺旋桨最大转速;
             calPropSpeed，当前的螺旋桨速度;
             propStopSpeed，螺旋桨减速时的加速度;
            */
            calPropSpeed = (motorOn) ? propSpinSpeed : calPropSpeed * (1f - (propStopSpeed / 2)); //计算螺旋桨应旋转的速度
            foreach (GameObject propeller in propellers) { propeller.transform.Rotate(0, 0, calPropSpeed); } //旋转螺旋桨
            
            #endregion
        }

        /*
         * FixedUpdate()函数用于固定更新。在游戏运行的过程中，每一帧的处理时间是不固定的，当我们需要固定间隔时间执行某些代码时，
           就会用到FixedUpdate()函数;update跟当前平台的帧数有关，而FixedUpdate是真实时间,
           所以处理物理逻辑的时候要把代码放在FixedUpdate而不是Update;
           在处理Rigidbody的时候最好用FixedUpdate
        */
        void FixedUpdate()
        {
            #region Physics Calculations, Rigidbody Movement, SoundFX
            if (motorOn)  // 如果无人机的发动机是启动的状态...
            {
                rigidBody.useGravity = false;  // 在飞行过程中不使用刚体的重力
                rigidBody.drag = 0;            // 在飞行过程中降低刚体的阻力
                rigidBody.angularDrag = 0;     // 在飞行过程中降低刚体的角阻力

                                                
                if (groundDistance > 0.2f)//在无人机离地面不是很近的时候...
                {   /*
                        是当无人机向前后左右移动的时候，产生倾斜效果                           
                    */
                    if (driveInput > 0) { rigidBody.AddForceAtPosition(Vector3.down * (Mathf.Abs(driveInput) * 0.2f), frontTilt.position, ForceMode.Acceleration); }   //在指定的“倾斜点”上添加向下/倾斜力
                    if (driveInput < 0) { rigidBody.AddForceAtPosition(Vector3.down * (Mathf.Abs(driveInput) * 0.2f), backTilt.position, ForceMode.Acceleration); }    // ...
                    if (strafeInput > 0) { rigidBody.AddForceAtPosition(Vector3.down * (Mathf.Abs(strafeInput) * 0.2f), rightTilt.position, ForceMode.Acceleration); }  // ...
                    if (strafeInput < 0) { rigidBody.AddForceAtPosition(Vector3.down * (Mathf.Abs(strafeInput) * 0.2f), leftTilt.position, ForceMode.Acceleration); }   // ...
                }
               
                /*要先转化为本地坐标系下的localVelocity，再转化回世界坐标系下的速度
                因为在操作无人机的进行前后左右运动的时候，你不是在世界坐标系内进行前后左右的移动，
                你是以无人机自己为参考，进行前后左右移动；
                当我们用鼠标改变无人机的视角时，我们的前后左右运动一直是以无人机自己为基准的。
               */
                Vector3 localVelocity = transform.InverseTransformDirection(rigidBody.velocity);//将无人机的世界速度转换为本地速度并存储
                localVelocity.z = (driveInput != 0) ? Mathf.Lerp(localVelocity.z, driveInput, acceleration * 0.3f) : Mathf.Lerp(localVelocity.z, driveInput, deceleration * 0.2f);//根据 drive input输入 增加或减小无人机向前或向后运动的速度
                //Debug.Log(driveInput);
                driveForce = (Mathf.Abs(localVelocity.z) > 0.01f) ? localVelocity.z : 0f;//如果localVelocity.z>0.01，则driveForce等于 localVelocity.z
                //driveForce = localVelocity.z;
                localVelocity.x = (strafeInput != 0) ? Mathf.Lerp(localVelocity.x, strafeInput, acceleration * 0.3f) : Mathf.Lerp(localVelocity.x, strafeInput, deceleration * 0.2f);//根据 strafe input输入 增加或减小无人机向左或向右运动的速度
                strafeForce = (Mathf.Abs(localVelocity.x) > 0.01f) ? localVelocity.x : 0f;//如果大于 localVelocity.x>0.01，则strafeForce等于 localVelocity.x
                rigidBody.velocity = transform.TransformDirection(localVelocity);//将无人机的本地速度转换回世界速度并应用它
            
                
                /*
                 *  liftForce: 无人机的上升速度
                    此处不需要把坐标系从世界坐标系转换为本地坐标系，因为无人机向上的速度和向下得速度在本地和世界坐标系中是一样的
                 */
                liftForce = (liftInput != 0) ? Mathf.Lerp(liftForce, liftInput, acceleration * 0.2f) : Mathf.Lerp(liftForce, liftInput, deceleration * 0.3f); ////根据 lift input输入 增加或减小无人机向上或向下运动的速度
                liftForce = (Mathf.Abs(liftForce) > 0.01f) ? liftForce : 0f;
                //Debug.Log("liftForce:"+liftForce);
                //无人机各个方向的速度的最终设定
                rigidBody.velocity = new Vector3(rigidBody.velocity.x, liftForce, rigidBody.velocity.z);// apply global velocity's Y axis to global velocity
                //rigidBody.velocity = new Vector3(strafeForce, liftForce, driveForce);
                
                /*
                  无人机的角速度
                */
                rigidBody.angularVelocity *= 1f - Mathf.Clamp(InputMagnitude(), 0.2f, 1.0f) * stability;//根据运动输入和稳定性值抑制无人机的角速度 
                //rigidBody.angularVelocity *= 1f - Mathf.Clamp(InputMagnitude(), 0.2f, 3.0f) * stability;
                //rigidBody.angularVelocity *= (1f-stability);//根据运动输入和稳定性值抑制无人机的角速度
                //Debug.Log("InputMagnitude:"+ InputMagnitude() );
                
                //生成一个向上的新旋转方向,由物体的正上方向 向 世界坐标的正上方向旋转（全局 Y 轴，Vector3.up）; Vector3.up即为（0,1,0）
                Quaternion uprightRotation = Quaternion.FromToRotation(transform.up, Vector3.up);
                //Debug.Log(transform.up);
                //Debug.Log("uprightRotation.w" + uprightRotation.w);
                
                /*
                 * public void AddTorque(Vector3 torque, ForceMode mode = ForceMode.Force),对刚体施加一个旋转力,torque 决定旋转力的大小和旋转轴的方向，旋转方向参照左手定则
                  左手定则:大拇指是旋转轴的方向（即Vector3 torque的方向），那么四个手指握起来的方向就是力旋转方向
                  在新的旋转方向上添加扭矩（力）以保持无人机直立
                 */
                ////此处没懂， uprightRotation.x 和 uprightRotation.z 并不是这个四元数对应的旋转轴的x和z分量；四元数的x = 旋转轴的x*sin(θ/2)？？？？？？？？？？？？？？？？？？？？？？？？？？？？
                rigidBody.AddTorque(new Vector3(uprightRotation.x, 0, uprightRotation.z) * 100f, ForceMode.Acceleration);//在新的旋转方向上添加扭矩（力）以保持无人机直立
                ////rigidBody.AddTorque(new Vector3((uprightRotation.x)/Mathf.Sin(Mathf.Acos(uprightRotation.w)), 0, (uprightRotation.z)/Mathf.Sin(Mathf.Acos(uprightRotation.w))) * 100f, ForceMode.Acceleration);//上一行的修改代码
                ////rigidBody.AddTorque(new Vector3(uprightRotation.x, 0, uprightRotation.z) * 10f, ForceMode.Acceleration);
                ////Debug.Log(uprightRotation.x +","+ uprightRotation.y+ ","+uprightRotation.z);
                
                rigidBody.angularVelocity = new Vector3(rigidBody.angularVelocity.x, turnForce, rigidBody.angularVelocity.z);//然后使用 turninput 围绕它的 Y 轴旋转无人机
                //rigidBody.angularVelocity = rigidBody.angularVelocity * (1f - Mathf.Clamp(InputMagnitude(), 0.2f, 1.0f) * stability);//换了位置
                ////Debug.Log("x:"+rigidBody.angularVelocity.x+"y,turnForce:"+turnForce+ ", z:"+rigidBody.angularVelocity.z);
                //drone.transform.Rotate(0,2,0);
            }
            else //如果motor关闭
            {
                rigidBody.useGravity = oGrav;    // 重置刚体的重力
                rigidBody.drag = oDrag;          // 重置刚体的阻力
                rigidBody.angularDrag = oADrag;  // 重置刚体的角阻力
            }
            #endregion
        }
        

        #region Public Functions
        public void ToggleMotor() { motorOn = !motorOn; }//Toggle 切换
        //public void ToggleHeadless() { headless = !headless; }
        public void DriveInput(float input) { if (input > 0) { driveInput = input * forwardSpeed; } else if (input < 0) { driveInput = input * backwardSpeed; } else { driveInput = 0; } }
        public void StrafeInput(float input) { if (input > 0) { strafeInput = input * rightSpeed; } else if (input < 0) { strafeInput = input * leftSpeed; } else { strafeInput = 0; } }
        public void LiftInput(float input) { if (input > 0) { liftInput = input * riseSpeed; motorOn = true; } else if (input < 0) { liftInput = input * lowerSpeed; } else { liftInput = 0; } }
        public void TurnInput(float input) { turnForce = input * turnSensitivty; }

        public void ResetDronePosition()
        {
            rigidBody.position = startPosition; 
            rigidBody.rotation = startRotation; 
            rigidBody.velocity = Vector3.zero;
        }
        
        float InputMagnitude() { return (Mathf.Abs(driveInput) + Mathf.Abs(strafeInput) + Mathf.Abs(liftInput)) / 3; } //
        //float InputMagnitude() { return (Mathf.Abs(driveInput) + Mathf.Abs(strafeInput) + Mathf.Abs(liftInput)) / 6; }//
        #endregion
    }
}