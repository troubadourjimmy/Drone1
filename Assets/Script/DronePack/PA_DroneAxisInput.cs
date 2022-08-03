using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PA_DronePack
{
    public class PA_DroneAxisInput : MonoBehaviour
    {
        #region Variables
        public enum InputType {
            Desktop,
            Gamepad
        }
        public InputType inputType = InputType.Desktop;
        InputType? _inputType = null;//可空类型

        public string forwardBackward;//对应的input manager中: vertical
        public string _forwardBackward;

        public string strafeLeftRight;//horizontal
        public string _strafeLeftRight;

        public string riseLower;//lift
        public string _riseLower;

        public string turn;//mouse x
        public string _turn;

        public string toggleMotor;
        public string _toggleMotor;//z
        //
        public string toggleCameraMode;//c
        public string _toggleCameraMode;

        public string toggleCameraGyro;//g
        public string _toggleCameraGyro;

        public string toggleFollowMode;//f
        public string _toggleFollowMode;

        public string toggleHeadless;
        public string _toggleHeadless;

        public string cameraRiseLower;//mouse Y
        public string _cameraRiseLower;

        public string cameraTurn;//mouse x
        public string _cameraTurn;

        public string cameraTilt;//mouse scrollwheel 鼠标滚轮
        public string _cameraTilt;

        public string cameraFreeLook;//left alt
        public string _cameraFreeLook;
        #endregion

        #region Hidden Variables
        public PA_DroneController dcoScript;
        public PA_DroneCamera dcScript;
        bool toggleMotorIsKey = false;
        bool toggleCameraModeIsKey = false;
        bool toggleFollowModeIsKey = false;
        bool cameraFreeLookIsKey = false;
        
        
        string[] keys = new string[] {
            "f1",
            "f2",
            "f3",
            "f4",
            "f5",
            "f6",
            "f7",
            "f8",
            "f9",
            "f10",
            "f11",
            "f12",
            "f13",
            "f14",
            "f15",
            "0",
            "1",
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
            "8",
            "9",
            "a",
            "b",
            "c",
            "d",
            "e",
            "f",
            "g",
            "h",
            "i",
            "j",
            "k",
            "l",
            "m",
            "n",
            "o",
            "p",
            "q",
            "r",
            "s",
            "t",
            "u",
            "v",
            "w",
            "x",
            "y",
            "z",
            "backspace",
            "delete",
            "tab",
            "clear",
            "return",
            "pause",
            "escape",
            "space",
            "up",
            "down",
            "right",
            "left",
            "insert",
            "home",
            "end",
            "pageup",
            "pagedown",
            "numlock",
            "capslock",
            "scroll ock",
            "rightshift",
            "leftshift",
            "rightctrl",
            "leftctrl",
            "rightalt",
            "leftalt"
        };
        #endregion

        void Awake()
        {
            #region Cache Components + Input
            dcoScript = GetComponent<PA_DroneController>();
            dcScript = FindObjectOfType<PA_DroneCamera>();
            UpdateInput();
            #endregion
        }

        void Update()
        {
            //Input监听器
            #region Input Axis Listeners  
            if (_inputType != inputType) {
                UpdateInput();
                _inputType = inputType;
            }
            
            if (forwardBackward != "") {
                dcoScript.DriveInput(Input.GetAxisRaw(forwardBackward));
            }

            if (strafeLeftRight != "") {
                dcoScript.StrafeInput(Input.GetAxisRaw(strafeLeftRight));
            }

            if (riseLower != "") {
                dcoScript.LiftInput(Input.GetAxisRaw(riseLower));
            }

            if (turn != "") {
                dcoScript.TurnInput(Input.GetAxisRaw(turn));
            }

            //dcScript是PA_DroneCamera, 摄像机的视角的升降(第三人称视角)
            if (cameraRiseLower != "" && dcScript) {
                dcScript.LiftInput(Input.GetAxisRaw(cameraRiseLower));
            }
            //摄像机视角的旋转(第三人称视角)
            if (cameraTurn != "" && dcScript) {
                dcScript.TurnInput(Input.GetAxisRaw(cameraTurn));
            }

            if (cameraTilt != "" && dcScript)
            {
                dcScript.TiltInput(Input.GetAxisRaw(cameraTilt));
            }
            
            //Debug.Log("toggleMotor:"+toggleMotor);
            #endregion
            
            //其他KeyCode的监听器
             #region Button / KeyCode Listeners
            //Unity Input接收两种参数，一种是Keycode的枚举值，另一种则是每个按键的字符串，以下是每个按键对应的字符串表
            if (toggleMotor != "") {
                //Unity Input接收两种参数，此部分是第一种KeyCode的枚举值
                if (toggleMotorIsKey) {
                    //toggleMotor 变量 是'z'
                    //Enum.Parse()将 字符串 toggleMotor转换为枚举类 KeyCode
                    if (Input.GetKeyDown((KeyCode)Enum.Parse(typeof(KeyCode), toggleMotor))) {
                        dcoScript.ToggleMotor();//ToggleMotor()作用是切换开关
                    }
                } 
                //Unity Input接收两种参数，此部分是第二种: 每个按键对应的字符串表
                else {
                    if (Input.GetButtonDown(toggleMotor)) {
                        dcoScript.ToggleMotor();
                    }
                }
            }
            
            
            //摄像机部分的模式切换
            if (toggleCameraMode != "" && dcScript) {
                if (toggleCameraModeIsKey) {
                    if (Input.GetKeyDown((KeyCode)Enum.Parse(typeof(KeyCode), toggleCameraMode))) {
                        dcScript.ChangeCameraMode();
                    }
                } else {
                    if (Input.GetButtonDown(toggleCameraMode)) {
                        dcScript.ChangeCameraMode();
                    }
                }
            }
           
            //FollowMode的模式切换
            if (toggleFollowMode != "" && dcScript) {
                if (toggleFollowModeIsKey) {
                    if (Input.GetKeyDown((KeyCode)Enum.Parse(typeof(KeyCode), toggleFollowMode))) {
                        dcScript.ChangeFollowMode();
                    }
                } else {
                    if (Input.GetButtonDown(toggleFollowMode)) {
                        dcScript.ChangeFollowMode();
                    }
                }
            }
            
            //FreeLook模式的切换
            if (cameraFreeLook != "" && dcScript) {
                if (cameraFreeLookIsKey) {
                    if (Input.GetKeyDown((KeyCode)Enum.Parse(typeof(KeyCode), cameraFreeLook))) {
                        dcScript.CanFreeLook(true);
                    }
                    if (Input.GetKeyUp((KeyCode)Enum.Parse(typeof(KeyCode), cameraFreeLook))) {
                        dcScript.CanFreeLook(false);
                    }
                } else {
                    if (Input.GetButtonDown(cameraFreeLook)) {
                        dcScript.CanFreeLook(true);
                    }
                    if (Input.GetButtonUp(cameraFreeLook)) {
                        dcScript.CanFreeLook(false);
                    }
                }
            }
            #endregion
        }

        #region Custom Functions
        void ParseKeys()
        {
            //arrays.Contains()该方法是判断字符串中是否有子字符串。如果有则返回true，如果没有则返回false。
            //String.ToLower将字符串转化为小写
            toggleMotorIsKey = keys.Contains(toggleMotor.ToLower());
            toggleCameraModeIsKey = keys.Contains(toggleCameraMode.ToLower());
            toggleFollowModeIsKey = keys.Contains(toggleFollowMode.ToLower());
            cameraFreeLookIsKey = keys.Contains(cameraFreeLook.ToLower());
        }

        public void UpdateInput()
        {
            if (inputType == InputType.Desktop) {
                forwardBackward = "Vertical";
                strafeLeftRight = "Horizontal";
                riseLower = "Lift";
                turn = "Mouse X";
                cameraRiseLower = "Mouse Y";
                cameraTurn = "Mouse X";
                cameraTilt = "Mouse ScrollWheel";//相机倾斜
                toggleMotor = "Z";
                toggleCameraMode = "C";
                //toggleCameraGyro = "G";
                toggleFollowMode = "F";
                cameraFreeLook = "LeftAlt";
                //toggleHeadless = "H";
            }
            if (inputType == InputType.Gamepad) {
                forwardBackward = "GP SecondaryJoystick Y";
                strafeLeftRight = "GP SecondaryJoystick X";
                riseLower = "GP PrimaryJoystick Y";
                turn = "GP PrimaryJoystick X";
                cameraRiseLower = "GP DPad Y";
                cameraTurn = "GP PrimaryJoystick X";
                cameraTilt = "GP DPad Y";
                toggleMotor = "GP Button 0";
                toggleCameraMode = "GP Button 1";
                toggleCameraGyro = "GP Button 2";
                toggleFollowMode = "GP Button 3";
                cameraFreeLook = "GP Button 5";
                toggleHeadless = "GP Button 6";
            }
          
            ParseKeys();
        }
        #endregion

    }
}
