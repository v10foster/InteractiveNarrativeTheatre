using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System;

public class InteractiveTheatreMain : VHMain
{
    public bool m_displayVhmsgLog = false;
    public FreeMouseLook m_camera;
    public Texture2D m_whiteTexture;
    public BMLEventHandler m_BMLEventHandler;
    public AudioCapturer m_AudioCapturer;
    public RecordingDevice m_RecordingDevice;
    public VideoRecorder m_VideoRecorder;

    SmartbodyManager m_sbm;

    Vector3 m_StartingCameraPosition;  // used for camera reset
    Quaternion m_StartingCameraRotation;

    bool m_showMenu = false;

    bool m_showController = false;
    bool m_walkToMode = false;
    Vector3 m_walkToPoint;
    bool m_reachMode = false;
    float m_timeSlider = 1.0f;

    bool m_gazingAtMouse = false;

    enum ControllerMenus             { NOMENU,       SBVOICE,    SBMOTION,    SBCONTROLLER,    ASR,   VIDEO,   VR,   CONFIG,   LENGTH };
    string [] m_controllerMenuText = { "debug menu", "sb-voice", "sb-motion", "sb-controller", "asr", "video", "vr", "config", };
    ControllerMenus m_controllerMenuSelected = ControllerMenus.NOMENU;

    string [] testUtteranceButtonText = { "1", "Tts", "V" };
    string [] testUtteranceName = { "brad_fullname", "speech_TTS", "z_viseme_test2" };
    string [] testUtteranceText = { "", "This is a sentence using text to speech.  Dont I sound delightful?", "" };  // the TTS text
    int testUtteranceSelected = 0;
    string [] testTtsVoices = { "Festival_voice_rab_diphone", "Festival_voice_kal_diphone", "Festival_voice_ked_diphone", "Microsoft|Anna", "Microsoft|Mary", "Microsoft|Mike", "Microsoft|David|Desktop", "Microsoft|Zira|Desktop", "Cerevoice_katherine", "Cerevoice_star" };
    int testTtsSelected = 0;
    string [] testAnimButtonText = { "1", "2", "3" };
    string [] testAnimName = { "ChrBrad@Idle01_ChopBoth01", "ChrBrad@Idle01_IndicateLeftLf01", "ChrBrad@Idle01_IndicateRightRt01" };
    //int[] asrRequiredFrequency = { /*ms speech*/22050, /*pocket sphinx*/ 16000 };
    int testAnimSelected = 0;
    bool m_useBigram = false;
    bool m_useContinuousAudio = false;
    float m_prevMicRecordingLevel;

    float m_gazeOffsetValueVertical   = 0;
    float m_gazeOffsetValueHorizontal = 0;

    float m_nodNumber = 2;
    float m_nodTime   = 1;
    int recognizerSelected = 0;
    int recordingDeviceSelected = 0;

    public string m_vhmsgServer = "cedros";

    bool m_remoteSpeechMethodPolling = true;  // true - polling method, false - vhmsg

    float m_debugMenuButtonH;


    public override void Awake()
    {
        base.Awake();
    }


    public override void Start()
    {
        Application.targetFrameRate = 60;
        base.Start();

        DisplaySubtitles = true;
        DisplayUserDialog = true;

        m_StartingCameraPosition = m_camera.transform.position;
        m_StartingCameraRotation = m_camera.transform.rotation;

        m_Console = DebugConsole.Get();

        m_sbm = SmartbodyManager.Get();


        SubscribeVHMsg();


        m_showController = true;

        if (VHUtils.IsAndroid() || VHUtils.IsIOS())
        {
            if (!m_Console.DrawConsole) m_Console.ToggleConsole();
        }

        if (VHUtils.IsAndroid() || VHUtils.IsIOS())
            m_debugMenuButtonH = 70;
        else
            m_debugMenuButtonH = 20;
    }


    void SubscribeVHMsg()
    {
        VHMsgBase vhmsg = VHMsgBase.Get();
        if (vhmsg)
        {
            vhmsg.SubscribeMessage("vrAllCall");
            vhmsg.SubscribeMessage("vrKillComponent");
            vhmsg.SubscribeMessage("vrExpress");
            vhmsg.SubscribeMessage("vrSpeak");
            vhmsg.SubscribeMessage("vrSpoke");
            vhmsg.SubscribeMessage("CommAPI");
            vhmsg.SubscribeMessage("acquireSpeech");
            vhmsg.SubscribeMessage("PlaySound");
            vhmsg.SubscribeMessage("StopSound");
            vhmsg.SubscribeMessage("renderer");
            vhmsg.SubscribeMessage("RemoteBmlRequest");
            vhmsg.SubscribeMessage("RemoteBmlReply");

            vhmsg.AddMessageEventHandler(new VHMsgBase.MessageEventHandler(VHMsg_MessageEvent));

            vhmsg.SendVHMsg("vrComponent renderer");
        }
    }


    public void Update()
    {
        if (m_sbm)
        {
            Camera cameraComponent = m_camera.GetComponent<Camera>();
            m_sbm.m_camPos = m_camera.transform.position;
            m_sbm.m_camRot = m_camera.transform.rotation;
            m_sbm.m_camFovY = cameraComponent.fieldOfView;
            m_sbm.m_camAspect = cameraComponent.aspect;
            m_sbm.m_camZNear = cameraComponent.nearClipPlane;
            m_sbm.m_camZFar = cameraComponent.farClipPlane;
        }

        //if (Input.GetKeyDown(KeyCode.Alpha1))
        //{
        //    MecanimCharacter mecAnimCharacter = GameObject.Find("BradM").GetComponent<MecanimCharacter>();
        //    mecAnimCharacter.PlayAudio("brad_fullname");
        //}

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (m_showMenu)
            {
                m_showMenu = false;
                Time.timeScale = m_timeSlider;
            }
            else
            {
                m_showMenu = true;
                Time.timeScale = 0;
            }
        }

        m_camera.enabled = !m_Console.DrawConsole;

        if (!m_Console.DrawConsole) // they aren't typing in a box
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
            }


            if (Input.GetKeyDown(KeyCode.C))
            {
                m_showController = !m_showController;
            }

            if (Input.GetKeyDown(KeyCode.K))
            {
                ToggleAxisLines();
            }

            if (Input.GetKeyDown(KeyCode.X))
            {
                // reset camera position
                m_camera.transform.position = m_StartingCameraPosition;
                m_camera.transform.rotation = m_StartingCameraRotation;
            }

            if (Input.GetKeyDown(KeyCode.Z))
            {
                GameObject.FindObjectOfType<DebugInfo>().NextMode();
            }
        }


        if (m_walkToMode)
        {
            // walk to mouse position

            bool doRaycast;
            Vector3 position = Vector3.zero;
            if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            {
                doRaycast = Input.touches.Length > 0;
                if (doRaycast)
                    position = Input.touches[0].position;
            }
            else
            {
                doRaycast = Input.GetMouseButtonDown(0);
                position = Input.mousePosition;
            }

            if (doRaycast)
            {
                Ray ray = m_camera.GetComponent<Camera>().ScreenPointToRay(position);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    Debug.Log("Walk to: " + -hit.point.x + " " + hit.point.z);
                    SmartbodyManager.Get().SBWalkTo("*", string.Format("{0} {1}", -hit.point.x, hit.point.z), false);
                    m_walkToPoint = hit.point;
                }
            }
        }


        if (m_reachMode)
        {
            bool doReach;
            Vector3 position = Vector3.zero;
            if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            {
                doReach = Input.touches.Length > 0;
                if (doReach)
                    position = Input.touches[0].position;
            }
            else
            {
                doReach = Input.GetMouseButtonDown(0);
                position = Input.mousePosition;
            }

            if (doReach)
            {
                bool found = false;
                Ray ray = m_camera.GetComponent<Camera>().ScreenPointToRay(position);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.collider.gameObject.GetComponent<SmartbodyPawn>() != null)
                    {
                        string cubeName = hit.collider.gameObject.name;
                        string rightLeft = UnityEngine.Random.Range(0, 2) > 0 ? "right" : "left";

                        m_sbm.PythonCommand(string.Format(@"bml.execBML('{0}', '<gaze target=""{1}"" sbm:joint-range=""neck eyes"" />')", "GraveDigger", cubeName));
                        m_sbm.PythonCommand(string.Format(@"bml.execBML('{0}', '<sbm:reach sbm:handle=""rdoctor"" sbm:action=""touch"" sbm:reach-type=""{1}"" target=""{2}"" />')", "GraveDigger", rightLeft, cubeName));
                        //m_sbm.PythonCommand(string.Format(@"bml.execBML('{0}', '<sbm:reach sbm:action=""touch"" sbm:reach-type=""{1}"" target=""{2}"" />')", "Brad", rightLeft, cubeName));

                        found = true;
                    }
                }

                if (!found)
                {
                    m_sbm.PythonCommand(string.Format(@"bml.execBML('{0}', '<sbm:reach sbm:handle=""rdoctor"" sbm:action=""touch"" sbm:reach-finish=""true"" />')", "GraveDigger"));
                    m_sbm.PythonCommand(string.Format(@"bml.execBML('{0}', '<gaze target=""{1}"" sbm:joint-range=""neck eyes"" />')", "GraveDigger", "Camera"));
                }
            }
        }


        if (m_gazingAtMouse)
        {
            if ((Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer) ||
               !m_Console.DrawConsole) // they aren't typing in a box
            {
                bool doGaze;
                Vector3 position = Vector3.zero;
                if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
                {
                    doGaze = Input.touches.Length > 0;
                    if (doGaze)
                        position = Input.touches[0].position;
                }
                else
                {
                    doGaze = true;
                    position = Input.mousePosition;
                }

                if (doGaze)
                {
                    Ray ray = m_camera.GetComponent<Camera>().ScreenPointToRay(position);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit))
                    {
                        GameObject mousePawn = GameObject.Find("MousePawn");
                        mousePawn.transform.position = hit.point;
                    }
                    else
                    {
                        GameObject mousePawn = GameObject.Find("MousePawn");
                        mousePawn.transform.position = m_camera.GetComponent<Camera>().transform.position;
                    }
                }
            }
        }


        if (m_RecordingDevice != null)
        {
            // update recording level
            float recordingLevel = m_RecordingDevice.GetRecordingVolumeLevel();
            recordingLevel = Math.Min(recordingLevel * 2.0f, 1.0f);
            if (recordingLevel > m_prevMicRecordingLevel)
            {
                m_prevMicRecordingLevel = recordingLevel;
            }
            else
            {
                m_prevMicRecordingLevel -= (Time.deltaTime * 2.0f);   // decay at some arbitrary level, this seems to work well
                m_prevMicRecordingLevel = Math.Max(m_prevMicRecordingLevel, 0);
            }
        }


        if (!m_showMenu)
        {
            // lock the screen cursor if they are looking around or using their mic
#if UNITY_4_7 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3_OR_NEWER
            Cursor.lockState = m_camera.CameraRotationOn ? CursorLockMode.Locked : CursorLockMode.None;
#else
            Screen.lockCursor = m_camera.CameraRotationOn;
#endif
        }
    }


    public override void OnGUI()
    {
        base.OnGUI();

        if (m_showMenu)
        {
            Rect r = new Rect(0.25f, 0.2f, 0.5f, 0.6f);
            GUILayout.BeginArea(VHIMGUI.ScaleToRes(ref r));
            GUILayout.BeginVertical();

            if (GUILayout.Button("Main Menu"))
            {
                m_showMenu = false;
                Time.timeScale = m_timeSlider;

                m_sbm.RemoveAllSBObjects();

                VHUtils.SceneManagerLoadScene("MainMenu");
            }

            GUILayout.Space(40);

            if (GUILayout.Button("Exit"))
            {
                VHUtils.ApplicationQuit();
            }

            GUILayout.Space(40);

            if (GUILayout.Button("Return to Game"))
            {
                m_showMenu = false;
                Time.timeScale = m_timeSlider;
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        if (m_showController)
        {
            float buttonX = 0;
            float buttonY = 0;
            float buttonW = 150;
            float spaceHeight = 30;

            if (m_VideoRecorder != null)
            {
#if !UNITY_WEBGL
                m_VideoRecorder.m_RenderTarget.gameObject.SetActive(m_controllerMenuSelected == ControllerMenus.VIDEO);
#endif
            }

            GUILayout.BeginArea(new Rect (buttonX, buttonY, buttonW, Screen.height));
            GUILayout.BeginVertical();


            GUILayout.BeginHorizontal();

            if (GUILayout.Button("<"))
            {
                m_controllerMenuSelected = (ControllerMenus)VHMath.DecrementWithRollover((int)m_controllerMenuSelected, m_controllerMenuText.Length);
            }

            if (GUILayout.Button(m_controllerMenuText[(int)m_controllerMenuSelected]))
            {
                m_controllerMenuSelected = (ControllerMenus)VHMath.IncrementWithRollover((int)m_controllerMenuSelected, m_controllerMenuText.Length);
            }

            if (GUILayout.Button(">"))
            {
                m_controllerMenuSelected = (ControllerMenus)VHMath.IncrementWithRollover((int)m_controllerMenuSelected, m_controllerMenuText.Length);
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(spaceHeight);

            if (m_controllerMenuSelected == ControllerMenus.NOMENU)
            {
            }
            else if (m_controllerMenuSelected == ControllerMenus.SBVOICE)
            {
                OnGUISbVoice();
            }
            else if (m_controllerMenuSelected == ControllerMenus.SBMOTION)
            {
                OnGUISbMotion();
            }
            else if (m_controllerMenuSelected == ControllerMenus.SBCONTROLLER)
            {
                OnGUISbController();
            }
            else if (m_controllerMenuSelected == ControllerMenus.ASR)
            {
                OnGUIASR();
            }
            else if (m_controllerMenuSelected == ControllerMenus.VIDEO)
            {
                OnGUIVideo();
            }
            else if (m_controllerMenuSelected == ControllerMenus.VR)
            {
                OnGUIVR();
            }
            else if (m_controllerMenuSelected == ControllerMenus.CONFIG)
            {
                OnGUIConfig(buttonW, spaceHeight);
            }


            if (!m_showMenu)
            {
                Time.timeScale = m_timeSlider;
            }


#if false
            string character1 = "Brad";
            string character2 = "BradM";

            if (GUILayout.Button("BML1"))
            {
#if false
                vrSpeak Brad ALL sbm_test_bml_7
                <?xml version="1.0" encoding="UTF-8"?>
                <act>
                    <bml>
                        <animation name="ChrBrad@Idle01_ChopBoth01" start="1.0" />
                    </bml>
                </act>


                vrSpeak Brad ALL sbm_test_bml_9
                <?xml version="1.0" encoding="UTF-8"?>
                <act>
                    <bml>
                        <head type="NOD" repeats="2" start="0" end="2" />
                    </bml>
                </act>

                vrSpeak Brad ALL sbm_test_bml_10
                <?xml version="1.0" encoding="UTF-8"?>
                <act>
                    <participant id="Brad" role="actor"/>
                    <bml>
                        <speech id="sp1" ref="helloworld" type="application/ssml+xml">Hello world</speech>
                    </bml>
                </act>
#endif


                string bmlIdString = "renderer_bml_" + m_bmlId++;
                string xml =    @"<?xml version=""1.0"" encoding=""UTF-8""?>" +
                                @"<act>" +
                                @"<bml>" +
                                @"<animation name=""ChrBrad@Idle01_ChopBoth01"" start=""1.0"" />" +
                                @"</bml>" +
                                @"</act>";

                string [] anims = new string [] {
                    "ChrBrad@Idle01_ArmStretch01",
                    "ChrBrad@Idle01_ChopLf01",
                    "ChrBrad@Idle01_Contemplate01",
                    "ChrBrad@Idle01_ExampleLf01",
                    "ChrBrad@Idle01_IndicateRightRt01",
                    "ChrBrad@Idle01_MeLf01",
                    "ChrBrad@Idle01_NegativeBt01",
                    "ChrBrad@Idle01_NegativeRt01",
                    "ChrBrad@Idle01_OfferBoth01",
                    "ChrBrad@Idle01_PleaBt02",
                    "ChrBrad@Idle01_ScratchChest01",
                    "ChrBrad@Idle01_ScratchHeadLf01",
                    "ChrBrad@Idle01_ScratchTempleLf01",
                    "ChrBrad@Idle01_ShoulderStretch01",
                    "ChrBrad@Idle01_TouchHands01",
                    "ChrBrad@Idle01_WeightShift01",
                    "ChrBrad@Idle01_WeightShift02",
                    "ChrBrad@Idle01_YouLf01" };

                xml = xml.Replace("ChrBrad@Idle01_ChopBoth01", anims[bml1Index]);
                bml1Index = (++bml1Index) % anims.Length;


                VHMsgBase vhmsg = VHMsgBase.Get();
                vhmsg.SendVHMsg(string.Format("vrSpeak {0} ALL {1} {2}", character1, bmlIdString, xml));
                vhmsg.SendVHMsg(string.Format("vrSpeak {0} ALL {1} {2}", character2, bmlIdString, xml));
            }

            if (GUILayout.Button("BML2"))
            {
                string bmlIdString = "renderer_bml_" + m_bmlId++;
                string xml =    @"<?xml version=""1.0"" encoding=""UTF-8""?>" +
                                @"<act>" +
                                @"<bml>" +
                                @"<head type=""NOD"" repeats=""2"" start=""0"" end=""2"" />" +
                                @"</bml>" +
                                @"</act>";

                VHMsgBase vhmsg = VHMsgBase.Get();

                vhmsg.SendVHMsg(string.Format("vrSpeak {0} ALL {1} {2}", character1, bmlIdString, xml));
                vhmsg.SendVHMsg(string.Format("vrSpeak {0} ALL {1} {2}", character2, bmlIdString, xml));
            }

            if (GUILayout.Button("BML3"))
            {
                string bmlIdString = "renderer_bml_" + m_bmlId++;
                string xml =    @"<?xml version=""1.0"" encoding=""UTF-8""?>" +
                                @"<act>" +
                                @"<participant id=""Brad"" role=""actor""/>" +
                                @"<bml>" +
                                @"<speech id=""sp1"" ref=""brad_fullname"" type=""application/ssml+xml"">My full name is brad mathew smith</speech>" +
                                @"</bml>" +
                                @"</act>";

                string [] sounds = new string [] { "brad_fullname", "brad_age", "brad_alwayscertain", "brad_alwayssure", "brad_answeredthat" };

                xml = xml.Replace("brad_fullname", sounds[bml3Index]);
                bml3Index = (++bml3Index) % 5;

                VHMsgBase vhmsg = VHMsgBase.Get();

                vhmsg.SendVHMsg(string.Format("vrSpeak {0} ALL {1} {2}", character1, bmlIdString, xml));
                vhmsg.SendVHMsg(string.Format("vrSpeak {0} ALL {1} {2}", character2, bmlIdString, xml));
            }

            if (GUILayout.Button("BML4"))
            {
                string bmlIdString = "renderer_bml_" + m_bmlId++;
                string xml =    @"<?xml version=""1.0"" encoding=""UTF-8""?>" +
                                @"<act>" +
                                @"<participant id=""Brad"" role=""actor"" />" +
                                @"<bml>" +
                                @"<speech id=""sp1"" ref=""brad_fullname"" type=""application/ssml+xml"">" +
                                @"<mark name=""T0"" />My" +
                                @"<mark name=""T1"" /><mark name=""T2"" />full" +
                                @"<mark name=""T3"" /><mark name=""T4"" />name" +
                                @"<mark name=""T5"" /><mark name=""T6"" />is" +
                                @"<mark name=""T7"" /><mark name=""T8"" />Brad" +
                                @"<mark name=""T9"" /><mark name=""T10"" />Matthew" +
                                @"<mark name=""T11"" /><mark name=""T12"" />Smith." +
                                @"<mark name=""T13"" />" +
                                @"</speech>" +
                                @"<event message=""vrAgentSpeech partial 1366842060442-9-1 T1 My "" stroke=""sp1:T1"" />" +
                                @"<event message=""vrAgentSpeech partial 1366842060442-9-1 T3 My full "" stroke=""sp1:T3"" />" +
                                @"<event message=""vrAgentSpeech partial 1366842060442-9-1 T5 My full name "" stroke=""sp1:T5"" />" +
                                @"<event message=""vrAgentSpeech partial 1366842060442-9-1 T7 My full name is "" stroke=""sp1:T7"" />" +
                                @"<event message=""vrAgentSpeech partial 1366842060442-9-1 T9 My full name is Brad "" stroke=""sp1:T9"" />" +
                                @"<event message=""vrAgentSpeech partial 1366842060442-9-1 T11 My full name is Brad Matthew "" stroke=""sp1:T11"" />" +
                                @"<event message=""vrAgentSpeech partial 1366842060442-9-1 T13 My full name is Brad Matthew Smith. "" stroke=""sp1:T13"" />" +
                                @"<gaze participant=""Brad"" id =""gaze"" target=""Camera"" direction=""UPLEFT"" angle=""0"" sbm:joint-range=""EYES HEAD"" xmlns:sbm=""http://ict.usc.edu"" />" +
                                @"<event message=""vrSpoke Brad all 1366842060442-9-1 My full name is Brad Matthew Smith"" stroke=""sp1:relax"" xmlns:xml=""http://www.w3.org/XML/1998/namespace"" xmlns:sbm=""http://ict.usc.edu"" />" +
                                @"<!--Inclusivity-->" +
                                @"<head type=""SHAKE"" amount=""0.4"" repeats=""1.0"" velocity=""1"" start=""sp1:T4"" priority=""4"" duration=""1"" />" +
                                @"<!--Noun clause nod-->" +
                                @"<head type=""NOD"" amount=""0.20"" repeats=""1.0"" start=""sp1:T9"" priority=""5"" duration=""1"" />" +
                                @"<animation name=""ChrBrad@Idle01_Contemplate01"" start=""sp1:T8"" />" +
                                @"</bml>" +
                                @"</act>";

                VHMsgBase vhmsg = VHMsgBase.Get();
                vhmsg.SendVHMsg(string.Format("vrSpeak {0} ALL {1} {2}", character1, bmlIdString, xml));
                vhmsg.SendVHMsg(string.Format("vrSpeak {0} ALL {1} {2}", character2, bmlIdString, xml));
            }
#endif

#if !UNITY_WEBGL && !UNITY_WSA && !UNITY_2018_2_OR_NEWER
            if (VHUtils.SceneManagerActiveSceneName() == "mecanimWeb")
            {
                m_vhmsgServer = GUILayout.TextField(m_vhmsgServer, 256);

                if (Network.connections.Length > 0)
                {
                    if (GUILayout.Button("Disconnect"))
                    {
                        Debug.Log("Calling Network.Disconnect()");
                        Network.Disconnect();
                    }
                }
                else
                {
                    if (GUILayout.Button("Connect"))
                    {
                        Debug.Log("Calling Network.Connect()");
                        NetworkConnectionError error = Network.Connect(m_vhmsgServer, 25000);
                        Debug.Log(error.ToString());


                        VHMsgBase m_vhmsg = GameObject.Find("VHMsgEmulator").GetComponent<VHMsgEmulator>();

                        m_vhmsg.SubscribeMessage("vrAllCall");
                        m_vhmsg.SubscribeMessage("vrKillComponent");
                        //m_vhmsg.SubscribeMessage("vrExpress");
                        m_vhmsg.SubscribeMessage("vrSpeak");
                        //m_vhmsg.SubscribeMessage("vrSpoke");
                        //m_vhmsg.SubscribeMessage("CommAPI");

                        m_vhmsg.SubscribeMessage("PlaySound");
                        m_vhmsg.SubscribeMessage("StopSound");
                        //m_vhmsg.SubscribeMessage("ToggleObjectVisibility");
                        //m_vhmsg.SubscribeMessage("wsp");

                        // sbm related vhmsgs
                        //m_vhmsg.SubscribeMessage("sbm");
                        //m_vhmsg.SubscribeMessage("vrAgentBML");
                        //m_vhmsg.SubscribeMessage("vrSpeak");
                        //m_vhmsg.SubscribeMessage("RemoteSpeechReply");
                        //m_vhmsg.SubscribeMessage("StopSound");
                        //m_vhmsg.SubscribeMessage("object-data");

                        m_vhmsg.AddMessageEventHandler(new VHMsgBase.MessageEventHandler(VHMsg_MessageEvent));
                        m_vhmsg.SendVHMsg("vrComponent renderer");
                    }
                }
            }
#endif

            GUILayout.EndVertical();
            GUILayout.EndArea();


            // this is outside of the gui area
            if (m_controllerMenuSelected == ControllerMenus.ASR)
            {
                if (m_RecordingDevice.IsRecording)
                {
                    // background
                    Rect micFillBar = new Rect(0.94f, 0.25f, 0.03f, 0.70f);
                    GUI.color = Color.black;
                    VHIMGUI.DrawTexture(micFillBar, m_whiteTexture);
                    GUI.color = Color.white;

                    // current recording level
                    Rect micLevel = micFillBar;
                    GUI.color = Color.red;
                    micLevel.height = m_prevMicRecordingLevel * micLevel.height;
                    micLevel.y = micFillBar.y + (micFillBar.height - micLevel.height);
                    VHIMGUI.DrawTexture(micLevel, m_whiteTexture);
                    GUI.color = Color.white;

                    // silence threshold
                    Rect silenceLevel = micFillBar;
                    GUI.color = Color.white;
                    silenceLevel.height = 0.004f;
                    silenceLevel.width += 0.015f;
                    silenceLevel.x -= (0.015f / 2);
                    silenceLevel.y = silenceLevel.y + ((1 - m_AudioCapturer.m_AudioStreamer.m_SilenceThreshhold) * micFillBar.height) - (silenceLevel.height / 2);
                    VHIMGUI.DrawTexture(silenceLevel, m_whiteTexture);
                    GUI.color = Color.white;
                }
            }
        }


        if (m_walkToMode)
        {
            Vector3 screenPoint = m_camera.GetComponent<Camera>().WorldToScreenPoint(m_walkToPoint);

            GUI.color = new Color(1, 0, 0, 1);
            float boxH = 10;
            float boxW = 10;
            Rect r = new Rect(screenPoint.x - (boxW / 2), (m_camera.GetComponent<Camera>().pixelHeight - screenPoint.y) - (boxH / 2), boxW, boxH);
            GUI.DrawTexture(r, m_whiteTexture);
            GUI.color = Color.white;
        }
    }


    void OnGUISbVoice()
    {
        GUILayout.BeginHorizontal();

        if (GUILayout.Button(testUtteranceButtonText[testUtteranceSelected], GUILayout.Height(m_debugMenuButtonH)))
        {
            testUtteranceSelected = VHMath.IncrementWithRollover(testUtteranceSelected, testUtteranceButtonText.Length);
        }
        if (GUILayout.Button("Audio", GUILayout.Height(m_debugMenuButtonH)))  { m_sbm.SBPlayAudio("Brad", testUtteranceName[testUtteranceSelected], testUtteranceText[testUtteranceSelected]); MobilePlayAudio(testUtteranceName[testUtteranceSelected]); }
        if (GUILayout.Button("XML", GUILayout.Height(m_debugMenuButtonH)))
        {
            AudioSpeechFile speech = GameObject.Find("/Utterances/" + testUtteranceName[testUtteranceSelected]).GetComponent<AudioSpeechFile>();
            string message = string.Format(@"bml.execXML('{0}', '{1}')", "Brad", speech.ConvertedXml);
            SmartbodyManager.Get().PythonCommand(message);
            MobilePlayAudio(testUtteranceName[testUtteranceSelected]);
        }

        GUILayout.EndHorizontal();

        if (GUILayout.Button(testTtsVoices[testTtsSelected], GUILayout.Height(m_debugMenuButtonH)))
        {
            testTtsSelected = VHMath.IncrementWithRollover(testTtsSelected, testTtsVoices.Length);

            m_sbm.PythonCommand(string.Format(@"scene.command('set character {0} voicebackup remote {1}')", "Brad", testTtsVoices[testTtsSelected]));
        }

        string mapping = m_useBigram ? "Bigram Method" : "Facefx Curves";
        if (GUILayout.Button(mapping, GUILayout.Height(m_debugMenuButtonH)))
        {
            ToggleLipSyncMethod();
        }

        GUILayout.BeginHorizontal();

        GUILayout.Label("RemoteSpeech");

        string remoteSpeechMethod = m_remoteSpeechMethodPolling ? "Polling" : "VHMsg";
        if (GUILayout.Button(remoteSpeechMethod, GUILayout.Height(m_debugMenuButtonH)))
        {
            m_remoteSpeechMethodPolling = !m_remoteSpeechMethodPolling;

            foreach (var character in m_sbm.GetSBMCharacterNames())
            {
                m_sbm.PythonCommand(string.Format(@"scene.getCharacter('{0}').setBoolAttribute('bmlRequestUsesPolling', {1})", character, m_remoteSpeechMethodPolling ? "True" : "False"));
            }
        }

        GUILayout.EndHorizontal();

        if (GUILayout.Button("Play Cutscene", GUILayout.Height(m_debugMenuButtonH)))
        {
            GameObject.Find("Cutscene01").GetComponent<Cutscene>().Play();
        }
    }


    void OnGUISbMotion()
    {
        if (GUILayout.Button("TestAnims", GUILayout.Height(m_debugMenuButtonH)))
        {
            GameObject.Find("ChrBradMotionsSKM_motion_Preload").GetComponent<SmartbodyMotionSet>().PlayAllMotions("Brad");
            GameObject.Find("ChrBradMotionsSKM_motion_Preload").GetComponent<SmartbodyMotionSet>().PlayAllMotions("Tiger");
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button(testAnimButtonText[testAnimSelected], GUILayout.Height(m_debugMenuButtonH)))
        {
            testAnimSelected = VHMath.IncrementWithRollover(testAnimSelected, testAnimButtonText.Length);
        }

        if (GUILayout.Button("Anim", GUILayout.Height(m_debugMenuButtonH)))
        {
            string c = "Brad";

            m_sbm.SBPlayAnim(c, testAnimName[testAnimSelected]);
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Gest1", GUILayout.Height(m_debugMenuButtonH)))
        {
            string c = "Brad";
            string message = string.Format(@"bml.execBML('{0}', '<gesture name=""{1}""/>')", c, testAnimName[testAnimSelected]);
            m_sbm.PythonCommand(message);
        }

        if (GUILayout.Button("Gest2", GUILayout.Height(m_debugMenuButtonH)))
        {
            string c = "Brad";
            string message = string.Format(@"bml.execBML('{0}', '<gesture lexeme=""{1}"" type=""{2}"" hand=""{3}""/>')", c, "METAPHORIC", "OBLIGATION", "BOTH_HANDS");
            m_sbm.PythonCommand(message);
        }
        GUILayout.EndHorizontal();
    }


    void OnGUISbController()
    {
        m_walkToMode = GUILayout.Toggle(m_walkToMode, "WalkToMode");

        if (GUILayout.Button("Stop Walking"))
        {
            string message = string.Format(@"bml.execBML('{0}', '<locomotion enable=""{1}"" />')", "*", "false");
            SmartbodyManager.Get().PythonCommand(message);
        }

        bool reachMode = GUILayout.Toggle(m_reachMode, "ReachMode");
        if (reachMode != m_reachMode)
        {
            m_reachMode = reachMode;
            GameObject reachObjects = GameObject.Find("ReachObjects");

            Transform[] allChildren = reachObjects.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in allChildren)
            {
                if (t == reachObjects.transform)
                    continue;

                t.gameObject.SetActive(m_reachMode);
            }
        }

        if (GUILayout.Button("Gaze Camera", GUILayout.Height(m_debugMenuButtonH)))
        {
            m_gazingAtMouse = false;
            m_sbm.SBGaze("Brad", "Camera");
        }

        if (GUILayout.Button("Gaze Mouse", GUILayout.Height(m_debugMenuButtonH)))
        {
            m_gazingAtMouse = true;
            m_sbm.SBGaze("Brad", "MousePawn");
        }

        if (GUILayout.Button("Gaze Tiger", GUILayout.Height(m_debugMenuButtonH)))
        {
            m_gazingAtMouse = true;
            m_sbm.SBGaze("Brad", "Tiger");
        }

        if (GUILayout.Button("Gaze Each other", GUILayout.Height(m_debugMenuButtonH)))
        {
            m_gazingAtMouse = true;
            m_sbm.SBGaze("Brad", "Tiger", 100.0f);
            m_sbm.SBGaze("Tiger", "Brad", 500.0f);
        }

        if (GUILayout.Button("Gaze Off", GUILayout.Height(m_debugMenuButtonH)))
        {
            m_gazingAtMouse = false;
            m_sbm.PythonCommand(string.Format(@"scene.command('char {0} gazefade out 1')", "Brad"));
            m_sbm.PythonCommand(string.Format(@"scene.command('char {0} gazefade out 1')", "Tiger"));
        }

        if (GUILayout.Button("Load Featured Model", GUILayout.Height(m_debugMenuButtonH)))
        {
            var comp = GetComponent<LoadGooglePolyAsset>();
            if (comp)
            {
                comp.LoadAsset();
            }
        }
        if (GUILayout.Button("Load Sword Model", GUILayout.Height(m_debugMenuButtonH)))
        {
            var comp = GetComponent<LoadGooglePolyAsset>();
            if (comp)
            {
                comp.LoadAsset("sword");
            }
        }

        GUILayout.Space(20);

        GUILayout.Label(string.Format("Gaze offset - {0} {1}", (int)m_gazeOffsetValueVertical, (int)m_gazeOffsetValueHorizontal));
        GUILayout.BeginHorizontal();
        //GUILayout.Space(buttonW / 2);
        //float gazeOffsetVTemp = GUILayout.VerticalSlider(m_gazeOffsetValueVertical, 90, -90);
        GUILayout.EndHorizontal();
        float gazeOffsetHTemp = GUILayout.HorizontalSlider(m_gazeOffsetValueHorizontal, -90, 90);

        if (//gazeOffsetVTemp != m_gazeOffsetValueVertical ||
            gazeOffsetHTemp != m_gazeOffsetValueHorizontal)
        {
            //m_gazeOffsetValueVertical   = gazeOffsetVTemp;
            m_gazeOffsetValueHorizontal = gazeOffsetHTemp;

            string message = string.Format(@"bml.execBML('{0}', '<gaze target=""{1}"" direction=""{2}"" angle=""{3}"" />')", "Brad", "Camera", m_gazeOffsetValueHorizontal > 0 ? "LEFT" : "RIGHT", Math.Abs(m_gazeOffsetValueHorizontal));
            m_sbm.PythonCommand(message);
        }

        GUILayout.Space(20);

        GUILayout.BeginHorizontal();
        GUILayout.Label(string.Format("n:{0:F1}", m_nodNumber), GUILayout.Width(40));
        m_nodNumber = GUILayout.HorizontalSlider(m_nodNumber, 0, 5);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label(string.Format("t:{0:F1}", m_nodTime), GUILayout.Width(40));
        m_nodTime = GUILayout.HorizontalSlider(m_nodTime, 0, 5);
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Nod", GUILayout.Height(m_debugMenuButtonH)))
        {
            string message = string.Format(@"bml.execBML('{0}', '<head type=""{1}"" repeats=""{2}"" start=""0"" end=""{3}"" />')", "Brad", "NOD", m_nodNumber, m_nodTime);
            m_sbm.PythonCommand(message);
        }

        if (GUILayout.Button("Shake", GUILayout.Height(m_debugMenuButtonH)))
        {
            string message = string.Format(@"bml.execBML('{0}', '<head type=""{1}"" repeats=""{2}"" start=""0"" end=""{3}"" />')", "Brad", "SHAKE", m_nodNumber, m_nodTime);
            m_sbm.PythonCommand(message);
        }
    }


    void OnGUIASR()
    {
        if (GUILayout.Button("Current Mic: " + m_RecordingDevice.GetDeviceName(), GUILayout.Height(m_debugMenuButtonH)))
        {
            recordingDeviceSelected++;
            recordingDeviceSelected %= m_RecordingDevice.GetNumRecordingDevices();
            m_RecordingDevice.SetRecordingDevice(recordingDeviceSelected);
        }

        if (GUILayout.Button(m_AudioCapturer.m_DefaultRecognizer.name, GUILayout.Height(m_debugMenuButtonH)))
        {
            recognizerSelected++;
            recognizerSelected %= m_AudioCapturer.m_SpeechRecognizers.Length;
            m_AudioCapturer.SetDefaultRecognizer(m_AudioCapturer.m_SpeechRecognizers[recognizerSelected]);
        }

        if (GUILayout.Button("Recognize", GUILayout.Height(m_debugMenuButtonH)))
        {
            m_AudioCapturer.CaptureAudioTextFromClip(GameObject.Find(testUtteranceName[testUtteranceSelected]).GetComponent<AudioSpeechFile>().m_AudioClip);
        }

        if (GUILayout.Button("Recognize & Play", GUILayout.Height(m_debugMenuButtonH)))
        {
            m_AudioCapturer.CaptureAudioTextFromClip(GameObject.Find(testUtteranceName[testUtteranceSelected]).GetComponent<AudioSpeechFile>().m_AudioClip, m_sbm.GetCharacterVoice("Brad"));
        }

        bool prevVal = m_useContinuousAudio;
        m_useContinuousAudio = GUILayout.Toggle(m_useContinuousAudio, "Continuous ASR");
        if (prevVal != m_useContinuousAudio)
        {
            m_RecordingDevice.SetContinuousStreaming(m_useContinuousAudio);
        }

        GUI.enabled = !m_useContinuousAudio;
        if (GUILayout.Button(m_RecordingDevice.IsRecording ? "Stop Recording" : "Push to Record"))
        {
            if (m_RecordingDevice.IsRecording)
            {
                m_RecordingDevice.StopRecording();
            }
            else
            {
                m_RecordingDevice.StartRecording();
            }
        }
        GUI.enabled = true;

        GUILayout.Label("Is Recording: " + (m_RecordingDevice.IsRecording ? "Yes" : "No"));
        GUILayout.Label("Audio Detected: " + (m_AudioCapturer.m_AudioStreamer.IsStreamAudioDetected ? "Yes" : "No"));

        m_AudioCapturer.m_AudioStreamer.m_SilenceThreshhold = GUILayout.HorizontalSlider(m_AudioCapturer.m_AudioStreamer.m_SilenceThreshhold, 0, 1);
        GUILayout.Label(string.Format("SilenceThresh: {0:f2}", m_AudioCapturer.m_AudioStreamer.m_SilenceThreshhold));
    }


    void OnGUIVideo()
    {
        GUILayout.Label("Camera Name: " + m_VideoRecorder.CurrentCameraName);

        if (GUILayout.Button("Play", GUILayout.Height(m_debugMenuButtonH)))
        {
            m_VideoRecorder.StartRecording();
        }

        if (GUILayout.Button("Pause", GUILayout.Height(m_debugMenuButtonH)))
        {
            m_VideoRecorder.PauseRecording();
        }

        if (GUILayout.Button("Stop", GUILayout.Height(m_debugMenuButtonH)))
        {
            m_VideoRecorder.StopRecording();
        }

        if (GUILayout.Button("Switch Cameras", GUILayout.Height(m_debugMenuButtonH)))
        {
            m_VideoRecorder.SwitchCameras();
            m_VideoRecorder.StartRecording();
        }
    }

    void OnGUIVR()
    {
        // some links for reference:
        // http://forum.unity3d.com/threads/native-vive-support.397828/
        // http://forum.unity3d.com/threads/unity-htc-vive-native-integration.400367/
        // https://steamcommunity.com/app/358720/discussions/0/490124466456883617/


#if UNITY_2017_2_OR_NEWER
        GUILayout.Label(string.Format("VRSettings.enabled: {0}", UnityEngine.XR.XRSettings.enabled));

        if (GUILayout.Button("Toggle VR"))
        {
            if (UnityEngine.XR.XRSettings.enabled)
            {
                UnityEngine.XR.XRSettings.enabled = false;
            }
            else
            {
                UnityEngine.XR.XRSettings.enabled = true;
            }
        }

        GUILayout.Label(string.Format("VRSettings.loadedDeviceName: {0}", UnityEngine.XR.XRSettings.loadedDeviceName));
        GUILayout.Label(string.Format("VRSettings.showDeviceView: {0}", UnityEngine.XR.XRSettings.showDeviceView));
        GUILayout.Label(string.Format("VRSettings.renderScale: {0}", UnityEngine.XR.XRSettings.eyeTextureResolutionScale));
        GUILayout.Label(string.Format("VRDevice.isPresent: {0}", UnityEngine.XR.XRDevice.isPresent));
        GUILayout.Label(string.Format("VRDevice.model: {0}", UnityEngine.XR.XRDevice.model));
#endif
    }

    void OnGUIConfig(float buttonW, float spaceHeight)
    {
        GUILayout.Label("Quality:");

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("<", GUILayout.Width(buttonW * 0.16f)))
        {
            QualitySettings.SetQualityLevel(VHMath.Clamp(QualitySettings.GetQualityLevel() - 1, 0, QualitySettings.names.Length - 1));
        }

        GUILayout.Button(string.Format("{0}", QualitySettings.names[QualitySettings.GetQualityLevel()]), GUILayout.Width(buttonW * 0.6f));

        if (GUILayout.Button(">", GUILayout.Width(buttonW * 0.16f)))
        {
            QualitySettings.SetQualityLevel(VHMath.Clamp(QualitySettings.GetQualityLevel() + 1, 0, QualitySettings.names.Length - 1));
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(spaceHeight);

        if (GUILayout.Button("Toggle Stats"))
        {
            GameObject.FindObjectOfType<DebugInfo>().NextMode();
        }

        if (GUILayout.Button("Toggle Console"))
        {
            m_Console.ToggleConsole();
        }

        if (GUILayout.Button("Toggle OnScreenLog"))
        {
            GameObject debugOnScreenLog = VHUtils.FindChild(GameObject.Find("vhAssets"), "DebugOnScreenLog");
            debugOnScreenLog.SetActive(!debugOnScreenLog.activeSelf);
            DebugOnScreenLog log = debugOnScreenLog.GetComponent<DebugOnScreenLog>();
            log.ShowLog(!log.IsShowing);

            Debug.LogFormat("Debug OnScreenLog toggled {0}", log.IsShowing ? "On" : "Off");
        }

        GUILayout.Space(spaceHeight);

        if (m_sbm) m_sbm.m_displayLogMessages = GUILayout.Toggle(m_sbm.m_displayLogMessages, "SBMLog");
        m_displayVhmsgLog = GUILayout.Toggle(m_displayVhmsgLog, "VHMsgLog");
        m_timeSlider = GUILayout.HorizontalSlider(m_timeSlider, 0.01f, 3);
        GUILayout.Label(string.Format("Time: {0}", m_timeSlider));

        GUILayout.Space(spaceHeight);
    }


    public override void OnApplicationQuit()
    {
        base.OnApplicationQuit();
    }


    void OnDestroy()
    {
        VHMsgBase vhmsg = VHMsgBase.Get();
        if (vhmsg)
            vhmsg.RemoveMessageEventHandler(new VHMsgBase.MessageEventHandler(VHMsg_MessageEvent));
    }


    void VHMsg_MessageEvent(object sender, VHMsgBase.Message message)
    {
        if (m_displayVhmsgLog)
        {
            Debug.Log("VHMsg recvd: " + message.s);
        }

        string [] splitargs = message.s.Split( " ".ToCharArray() );

        if (splitargs.Length > 0)
        {
            if (splitargs[0] == "vrAllCall")
            {
                VHMsgBase vhmsg = VHMsgBase.Get();
                vhmsg.SendVHMsg("vrComponent renderer");
            }
            else if (splitargs[0] == "vrKillComponent")
            {
                if (splitargs.Length > 1)
                {
                    if (splitargs[1] == "renderer" || splitargs[1] == "all")
                    {
                        VHUtils.ApplicationQuit();
                    }
                }
            }
            else if (splitargs[0] == "RemoteBmlRequest")
            {
                //RemoteBmlRequest <characterName> <requestId> <utterranceName>
                //VHMsgBase vhmsg = VHMsgBase.Get();
                string charName = splitargs[1];
                string requestId = splitargs[2];
                string utteranceName = splitargs[3];
                m_sbm.SendBmlReply(charName, requestId, utteranceName);
            }
            else if (splitargs[0] == "PlaySound")
            {
                string path = splitargs[1].Trim('"');   // PlaySound has double quotes around the sound file.  remove them before continuing.
                path = Path.GetFullPath(path);
                path = path.Replace("\\", "/");

                string utteranceId = Path.GetFileNameWithoutExtension(path);
                AudioSpeechFile[] speechFiles = FindObjectsOfType<AudioSpeechFile>();
                bool found = false;
                for (int i = 0; i < speechFiles.Length; i++)
                {
                    if (speechFiles[i].m_AudioClip.name == utteranceId)
                    {
                        found = true;
                        AudioSource charVoiceSource = m_sbm.GetCharacterVoice(splitargs[2]);
                        charVoiceSource.clip = speechFiles[i].m_AudioClip;
                        charVoiceSource.Play();
                        break;
                    }
                }

                if (!found)
                {
                    if (path.StartsWith("//"))  // network path
                        path = "file://" + path;
                    else  // assume absolute path
                        path = "file:///" + path;

                    WWW www = new WWW(path);
                    VHUtils.PlayWWWSound(this, www, m_sbm.GetCharacterVoice(splitargs[2]), false);
                }
            }
            else if (splitargs[0] == "StopSound")
            {
                m_sbm.GetCharacterVoice("Brad").Stop();
            }
            else if (splitargs[0] == "renderer")
            {
                if (splitargs.Length > 2)
                {
                    if (splitargs[1] == "function")
                    {
                        // "renderer function log testing testing"
                        // "renderer function console show_tips 1"

                        string function = splitargs[2].ToLower();
                        string[] rendererSplitArgs = new string[splitargs.Length - 3];
                        Array.Copy(splitargs, 3, rendererSplitArgs, 0, splitargs.Length - 3);

                        gameObject.SendMessage(function, rendererSplitArgs);
                    }
                }
            }
            else if (splitargs[0] == "vrSpeak" || splitargs[0] == "vrAgentBML")
            {
#if false
                vrSpeak Brad ALL sbm_test_bml_7
                <?xml version="1.0" encoding="UTF-8"?>
                <act>
                    <bml>
                        <animation name="ChrBrad@Idle01_ChopBoth01" start="1.0" />
                    </bml>
                </act>
#endif
                if (splitargs.Length > 4)
                {
                    string character = splitargs[1];
                    //string all = splitargs[2];
                    //string bmlId = splitargs[3];
                    string xml = String.Join(" ", splitargs, 4, splitargs.Length - 4);

                    if (m_BMLEventHandler != null && character == "BradM")
                    {
                        m_BMLEventHandler.LoadXMLString(character, xml);
                    }
                    //BMLParser.TestBMLParser(character, xml);
                }
            }
        }
    }


    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Callback from DebugConsole")]
    protected void log( string [] args )
    {
        if (args.Length > 0)
        {
            string argsString = String.Join(" ", args);
            Debug.Log(argsString);
        }
    }


    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Callback from DebugConsole")]
    protected void console( string [] args )
    {
        if (args.Length > 0)
        {
            string argsString = String.Join(" ", args);
            HandleConsoleMessage(argsString, m_Console);
        }
    }


    protected override void HandleConsoleMessage(string commandEntered, DebugConsole console)
    {
        base.HandleConsoleMessage(commandEntered, console);

        if (commandEntered.IndexOf("vhmsg") != -1)
        {
            string opCode = string.Empty;
            string args = string.Empty;
            if (console.ParseVHMSG(commandEntered, ref opCode, ref args))
            {
                VHMsgBase vhmsg = VHMsgBase.Get();
                vhmsg.SendVHMsg(opCode, args);
            }
            else
            {
                console.AddText(commandEntered + " requires an opcode string and can have an optional argument string");
            }
        }
        else if (commandEntered.IndexOf("set_resolution") != -1)
        {
            Vector2 vec2Data = Vector2.zero;
            if (console.ParseVector2(commandEntered, ref vec2Data))
            {
                SetResolution((int)vec2Data.x, (int)vec2Data.y, Screen.fullScreen);
            }
        }
    }


    void SetResolution(int width, int height, bool fullScreen)
    {
        Screen.SetResolution(width, height, fullScreen);
    }


    void MobilePlayAudio(string audioFile)
    {
        // Play the audio directly because VHMsg isn't enabled on mobile.  So, we can't receive the PlaySound message

        if (Application.platform == RuntimePlatform.Android ||
            Application.platform == RuntimePlatform.IPhonePlayer)
        {
            string utteranceId = Path.GetFileNameWithoutExtension(audioFile);
            AudioSpeechFile[] speechFiles = FindObjectsOfType<AudioSpeechFile>();
            for (int i = 0; i < speechFiles.Length; i++)
            {
                if (speechFiles[i].m_AudioClip.name == utteranceId)
                {
                    AudioSource charVoiceSource = m_sbm.GetCharacterVoice("Brad");
                    charVoiceSource.clip = speechFiles[i].m_AudioClip;
                    charVoiceSource.Play();
                    break;
                }
            }
        }
    }


    void ToggleLipSyncMethod()
    {
        m_useBigram = !m_useBigram;

        if (m_useBigram)
        {
            m_sbm.PythonCommand(string.Format(@"scene.getCharacter('{0}').setStringAttribute('lipSyncSetName', 'default')", "Brad"));
            m_sbm.PythonCommand(string.Format(@"scene.getCharacter('{0}').setBoolAttribute('usePhoneBigram', True)", "Brad"));

            m_sbm.PythonCommand(string.Format(@"scene.getCharacter('{0}').setUseVisemeCurves(False)", "Brad"));

            VHMsgBase.Get().SendVHMsg(string.Format("TTSRelay setmapping sbm"));
        }
        else
        {
            m_sbm.PythonCommand(string.Format(@"scene.getCharacter('{0}').setStringAttribute('lipSyncSetName', 'default')", "Brad"));
            m_sbm.PythonCommand(string.Format(@"scene.getCharacter('{0}').setBoolAttribute('usePhoneBigram', False)", "Brad"));

            m_sbm.PythonCommand(string.Format(@"scene.getCharacter('{0}').setUseVisemeCurves(True)", "Brad"));

            VHMsgBase.Get().SendVHMsg(string.Format("TTSRelay setmapping facefx"));
        }
    }


    protected IEnumerator StartReachSequence()
    {
        Debug.Log(string.Format("StartReachSequence()"));

        string cubeName = "GrabSphere1";

        VHMsgBase.Get().SendVHMsg(string.Format(@"sb bml.execBML('{0}', '<gaze target=""{1}"" sbm:joint-range=""neck eyes"" />')", "Brad", cubeName));

        yield return new WaitForSeconds(0.2f);

        VHMsgBase.Get().SendVHMsg(string.Format(@"sb bml.execBML('{0}', '<sbm:reach sbm:handle=""rdoctor"" sbm:action=""touch"" sbm:reach-type=""right"" target=""{1}"" />')", "Brad", cubeName));

        yield return new WaitForSeconds(0.8f);

        //VHMsgBase.Get().SendVHMsg(string.Format(@"unity reach {0} attach {1} r_wrist", cubeName, "Brad"));
        //yield return new WaitForSeconds(0.5f);
        //VHMsgBase.Get().SendVHMsg(string.Format(@"sb bml.execBML('{0}', '<sbm:reach sbm:handle=""rdoctor"" target=""Issue{1}"" sbm:reach-type=""right"" sbm:fade-in=""1.0"" />')", "Brad", other_player));

        yield return new WaitForSeconds(1.0f);

        //VHMsgBase.Get().SendVHMsg(string.Format(@"unity reach {0} remove {1}", cubeName, "Brad"));
        VHMsgBase.Get().SendVHMsg(string.Format(@"sb bml.execBML('{0}', '<sbm:reach sbm:handle=""rdoctor"" sbm:action=""touch"" sbm:reach-finish=""true"" />')", "Brad"));
        VHMsgBase.Get().SendVHMsg(string.Format(@"sb bml.execBML('{0}', '<gaze target=""{1}"" sbm:joint-range=""neck eyes"" />')", "Brad", "Camera"));
    }


    public void ToggleAxisLines()
    {
        GameObject axisLines = GameObject.Find("AxisLines");
        if (axisLines)
        {
            if (axisLines.transform.childCount > 0)
            {
                Transform[] allChildren = axisLines.GetComponentsInChildren<Transform>(true);

                if (axisLines.transform.GetChild(0).gameObject.activeSelf)
                {
                    foreach (Transform t in allChildren)
                    {
                        if (t == axisLines.transform)
                            continue;

                        t.gameObject.SetActive(false);
                    }
                }
                else
                {
                    foreach (Transform t in allChildren)
                    {
                        if (t == axisLines.transform)
                            continue;

                        t.gameObject.SetActive(true);
                    }
                }
            }
        }
    }
}
