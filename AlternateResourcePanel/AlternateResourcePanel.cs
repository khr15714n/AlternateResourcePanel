﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using KSP;
using UnityEngine;

namespace KSPAlternateResourcePanel
{
    [KSPAddon(KSPAddon.Startup.Flight,false)]
    public partial class KSPAlternateResourcePanel : MonoBehaviour
    {
        //StageGroup Button
        //Autostage option
        //Settings??
        //Remember toggle state/settings

        //GlobalSettings
        private Boolean HoverOn = false;    //Are we hovering on something to draw the screen
        private Boolean ToggleOn = false;   //Has the draw been toggled on
        private Boolean Drawing = false;    //Am I drawing the window
        private Boolean DrawingSettings = false;    //Am I drawing the Settings Bit
        private static String _ClassName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name;

        /// <summary>
        /// Class Initializer
        /// </summary>
        public KSPAlternateResourcePanel()
        {

        }

        
        /// <summary>
        /// Awake Event - when the DLL is loaded 
        /// </summary>
        public void Awake()
        {
            DebugLogFormatted("Awakening the {0}", _ClassName);

            System.Random rnd = new System.Random();
            _WindowMainID = rnd.Next(1000, 2000000);
            _WindowSettingsID = rnd.Next(1000, 2000000);

            //Load Textures
            LoadTextures();

            //Load Settings here?
            LoadSettings();

            //Add it to the queue
            DebugLogFormatted("Adding to DrawQueue");
            RenderingManager.AddToPostDrawQueue(0, DrawGUI);

            //Get the time per sec from the settings file
            SetupRepeatingFunction("BehaviourUpdate", 0.05f);
            //SetupRepeatingFunction_BehaviourUpdate(10);
        }
                
        /// <summary>
        /// Destroy Event - when the DLL is unloaded 
        /// </summary>
        public void OnDestroy()
        {
            DebugLogFormatted("Destroying the {0}", _ClassName);
        }

        
        /// <summary>
        /// Update Function - Happens on every frame - this is where behavioural stuff is typically done 
        /// </summary>
        public void Update()
        {
            //TODO:Disable this one
            //if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && Input.GetKeyDown(KeyCode.F8))
            //    DebugActionTriggered(HighLogic.LoadedScene);

            //Activate Stage via Space Bar in MapView
            if (blnStagingSpaceInMapView && MapView.MapIsEnabled && Drawing && Input.GetKey(KeyCode.Space))
                Staging.ActivateNextStage();

        }

        private static int _WindowMainID = 0;
        private static Rect _WindowMainRect = new Rect(Screen.width - 298, 19, 299, 20);
        private static int _WindowMainHeight = 30;

        private static int _WindowSettingsID = 0;
        private static Rect _WindowSettingsRect = new Rect(Screen.width - 298, 200, 299, 200);
        private static int _WindowSettingsHeight = 144;

        private static Boolean ShowSettings = false;
        private static Boolean blnResetWindow = false;
        /// <summary>
        /// This is what we do every frame when the object is being drawn 
        /// We dont get here unless we are in the postdraw queue
        /// </summary>
        public void DrawGUI()
        {

            //Check for loaded Textures, etc
            if (!DrawStuffConfigured) {
                SetupDrawStuff();
            }

            //Draw the button
            if (GUI.Button(rectButton, "Alternate",styleButtonMain))
            {
                ToggleOn = !ToggleOn;
                SaveConfig();
            }

            //Test for moue over any component
            HoverOn = IsMouseOver();

            //Are there any resources left?
            if ((HoverOn || ToggleOn) && (lstResources.Count > 0))
            {
                if (blnResetWindow)
                {
                    DebugLogFormatted("A");
                    _WindowMainRect = new Rect(Screen.width - 298, 19, 299, 20);
                    blnResetWindow = false;
                    SaveConfig();
                }

                //set up the main window and loock it in the screen
                Rect MainWindowPos = new Rect(_WindowMainRect) { height = _WindowMainHeight };
                MainWindowPos = ClampToScreen(MainWindowPos);
                _WindowMainRect = GUILayout.Window(_WindowMainID, MainWindowPos, FillWindow, "", stylePanel);

                //Flag that we are drawing on the screen
                Drawing = true;

                if (ShowSettings)
                {
                    Rect SettingsWindowPos = new Rect(_WindowMainRect) {y = _WindowMainRect.y+_WindowMainRect.height-2, height=_WindowSettingsHeight};

                    //Nowfit it in the screen and move it around as the main window moves around 
                    if (Screen.height < SettingsWindowPos.y + SettingsWindowPos.height)
                    {
                        if (0 < SettingsWindowPos.x - SettingsWindowPos.width)
                        {
                            SettingsWindowPos.x = _WindowMainRect.x - _WindowMainRect.width + 2;
                            SettingsWindowPos.y = Mathf.Clamp(_WindowMainRect.y, 0, Screen.height - SettingsWindowPos.height + 1);
                        }
                        else //if (Screen.width < SettingsWindowPos.x + SettingsWindowPos.width)
                        {
                            SettingsWindowPos.x = _WindowMainRect.x + _WindowMainRect.width - 2;
                            SettingsWindowPos.y = Mathf.Clamp(_WindowMainRect.y, 0, Screen.height - SettingsWindowPos.height + 1);
                        }
                    }

                    _WindowSettingsRect=GUILayout.Window(_WindowSettingsID, SettingsWindowPos, FillSettingsWindow, "", stylePanel);

                    DrawingSettings = true;
                }

            }
            else
            {
                Drawing = false;
                DrawingSettings = false;
                ShowSettings = false;
            }

            //_WindowDebugRect = GUILayout.Window(_WindowDebugID, _WindowDebugRect, FillDebugWindow, "Debug");

            DrawToolTip();
        }
        //private static int _WindowDebugID = 12345;
        //private static Rect _WindowDebugRect = new Rect(100,130,400,200);


        Rect ClampToScreen(Rect r)
        {
            r.x = Mathf.Clamp(r.x, -1, Screen.width - r.width+1);
            r.y = Mathf.Clamp(r.y, -1, Screen.height - r.height+1);
            return r;
        }

        int intLineHeight = 20;
        static Rect rectButton = new Rect(Screen.width - 109, 0, 80, 30);

        private void FillWindow(int WindowHandle)
        {
            GUILayout.BeginVertical();

            float fltBarRemainRatio;
            float fltBarRemainStageRatio;

            //What will the height of the panel be
            _WindowMainHeight = ((lstResources.Count + 1) * intLineHeight) + 12;

            Dictionary<String, Texture2D> dictFirst = texIconsKSPARP;
            Dictionary<String, Texture2D> dictSecond = texIconsPlayer;

            Rect rectBar;
            //Now draw the main panel
            for (int i = 0; i < lstResources.Count; i++)
            {
                if (i > 0) GUILayout.Space(4);
                GUILayout.BeginHorizontal();
                //add title
                GUIContent contLabel;
                    
                if (dictFirst.ContainsKey(lstResources[i].Resource.name.ToLower()))
                {
                    contLabel = new GUIContent(dictFirst[lstResources[i].Resource.name.ToLower()]);
                }
                else if (dictSecond.ContainsKey(lstResources[i].Resource.name.ToLower()))
                {
                    contLabel = new GUIContent(dictSecond[lstResources[i].Resource.name.ToLower()]);
                }
                else
                {
                    contLabel = new GUIContent(System.Text.RegularExpressions.Regex.Replace(lstResources[i].Resource.name, "[^A-Z]", ""));
                    if (lstResources[i].Resource.name.Length < 5) 
                        contLabel.text = lstResources[i].Resource.name;
                    else if (contLabel.text.Length<2)
                        contLabel.text = lstResources[i].Resource.name.Substring(0,3) + "...";
                }

                contLabel.tooltip = lstResources[i].Resource.name;
                GUILayout.Label(contLabel, styleBarName);

                GUILayout.Space(4);


                //set ration for remaining resource value
                fltBarRemainRatio = (float)lstResources[i].Amount / (float)lstResources[i].MaxAmount;

                //For resources with no stage specifics
                if (lstResources[i].Resource.resourceFlowMode == ResourceFlowMode.ALL_VESSEL)
                {
                    //full width bar
                    rectBar=DrawBar(i, styleBarGreen_Back, 245);
                    if ((rectBar.width * fltBarRemainRatio) > 1)
                        DrawBarScaled(rectBar, i, styleBarGreen, styleBarGreen_Thin, fltBarRemainRatio);

                    //add amounts
                    DrawUsage(rectBar, i, lstResources[i].Amount, lstResources[i].MaxAmount);
                    //add rate
                    if (blnShowInstants) DrawRate(rectBar, i, lstResources[i].Rate);
                }
                else
                {
                    //need full Vessel and current stage bars
                    rectBar = DrawBar( i, styleBarGreen_Back,120);
                    if ((rectBar.width * fltBarRemainRatio) > 1)
                        DrawBarScaled(rectBar, i, styleBarGreen, styleBarGreen_Thin, fltBarRemainRatio);

                    //add amounts
                    DrawUsage(rectBar, i, lstResources[i].Amount, lstResources[i].MaxAmount);
                    //add rate
                    if (blnShowInstants) DrawRate(rectBar, i, lstResources[i].Rate);

                    GUILayout.Space(1);
                    ////get last stage of this resource and set it
                    arpResource resTemp = lstResourcesLastStage.FirstOrDefault(x => x.Resource.id == lstResources[i].Resource.id);
                    if (resTemp != null)
                    {
                        fltBarRemainStageRatio = (float)resTemp.Amount / (float)resTemp.MaxAmount;
                        rectBar=DrawBar( i, styleBarBlue_Back,120);
                        if ((rectBar.width * fltBarRemainStageRatio) > 1)
                            DrawBarScaled(rectBar, i, styleBarBlue, styleBarBlue_Thin, fltBarRemainStageRatio);

                        //add amounts
                        DrawUsage(rectBar, i, resTemp.Amount, resTemp.MaxAmount);
                        //add rate
                        if (blnShowInstants) DrawRate(rectBar, i, resTemp.Rate);
                    }
                }


                GUILayout.EndHorizontal();
                    
            }

            GUILayout.BeginHorizontal();
            ////STAGING STUFF
            if (blnStaging)
            {
                GUILayout.Label("Stage:", styleStageTextHead, GUILayout.Width(60));
                GUIStyle styleStageNum = new GUIStyle(styleStageTextHead);
                GUIContent contStageNum = new GUIContent(Staging.CurrentStage.ToString());
                //styleStageNum.normal.textColor=new Color(173,43,43);
                //GUIContent contStageNum = new GUIContent(Staging.CurrentStage.ToString(),"NO Active Engines");
                //if (THERE ARE ACTIVE ENGINES IN STAGE)
                //{
                //contStageNum.tooltip="Active Engines";
                styleStageNum.normal.textColor = new Color(117, 206, 60);
                //}

                GUILayout.Label(contStageNum, styleStageNum ,GUILayout.Width(40));

                if (blnStagingInMapView || !MapView.MapIsEnabled)
                {
                    if (GUILayout.Button("Activate Stage", styleStageButton,GUILayout.Width(100)))
                        Staging.ActivateNextStage();
                }

            }

            //rectChevron.y = rectPanel.y + rectPanel.height - rectChevron.height - 2;
            GUILayout.FlexibleSpace();
            GUIContent btnMinMax = new GUIContent(btnChevronDown, "Show Settings...");
            if (ShowSettings) { btnMinMax.image = btnChevronUp; btnMinMax.tooltip = "Hide Settings"; }
            if (GUILayout.Button( btnMinMax, styleButtonSettings))
            {
                ShowSettings = !ShowSettings;
                SaveConfig();
            }

            GUILayout.EndHorizontal();
            
            
            GUILayout.EndVertical();

            SetTooltipText();
            if (!blnLockLocation)
                GUI.DragWindow();
        }

        private Rect DrawBar(int Row, GUIStyle Style, int Width = 0, int Height = 0)
        {
            List<GUILayoutOption> Options = new List<GUILayoutOption>() ;
            if (Width == 0) Options.Add(GUILayout.ExpandWidth(true));
            else Options.Add(GUILayout.Width(Width));
            if (Height != 0) Options.Add(GUILayout.Height(Height));
            GUILayout.Label("", Style,Options.ToArray());

            return GUILayoutUtility.GetLastRect();
        }

        private void DrawBar(Rect rectStart, int Row, GUIStyle Style)
        {
            GUI.Label(rectStart, "", Style);
        }

        private void DrawBarScaled(Rect rectStart, int Row, GUIStyle Style, GUIStyle StyleNarrow, float Scale)
        {
            Rect rectTemp = new Rect(rectStart);
            rectTemp.width=(float)Math.Ceiling(rectTemp.width = rectTemp.width * Scale);
            if (rectTemp.width <= 2) Style = StyleNarrow;
            GUI.Label(rectTemp, "", Style);
        }

        private void DrawUsage(Rect rectStart, int Row, double Amount, double MaxAmount)
        {
            Rect rectTemp = new Rect(rectStart);

            if (blnShowInstants && (rectStart.width<180)) rectTemp.width = (rectTemp.width * 2 / 3);
            rectTemp.x -= 20;
            rectTemp.width += 40;

            GUI.Label(rectTemp, string.Format("{0} / {1}", DisplayValue(Amount), DisplayValue(MaxAmount)), styleBarText);
        }

        private void DrawRate(Rect rectStart, int Row, double Rate)
        {
            Rect rectTemp = new Rect(rectStart) { width = rectStart.width - 2 };
            GUI.Label(rectTemp, string.Format("({0})", DisplayValue(Rate)), styleBarRateText);
        }

        private string DisplayValue(Double Amount)
        {
            String strFormat = "{0:0}";
            if (Amount<100) 
                strFormat = "{0:0.00}";
            return string.Format(strFormat, Amount);
        }

        private void FillSettingsWindow(int windowHandle)
        {
            //styleSettingsArea.padding = new RectOffset(intTest, intTest2, intTest3, intTest4);
            //styleToggle.padding = new RectOffset(intTest5, intTest6, intTest7, intTest8);

            //General
            GUILayout.BeginHorizontal(styleSettingsArea, GUILayout.Width(282));
            GUILayout.BeginVertical(GUILayout.Width(60));
            GUILayout.Label("General:", styleStageTextHead);
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            blnShowInstants = GUILayout.Toggle(blnShowInstants, "Show Rate Change Values", styleToggle);
            blnLockLocation = GUILayout.Toggle(blnLockLocation, "Lock Window Position.", styleToggle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Position", styleButton))
                SaveConfig();
            if (GUILayout.Button("Reset Position", styleButton))
                blnResetWindow = true;
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            //Staging
            GUILayout.BeginHorizontal(styleSettingsArea, GUILayout.Width(282), GUILayout.Height(64));
            GUILayout.BeginVertical(GUILayout.Width(60));
            GUILayout.Label("Staging:", styleStageTextHead);
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            blnStaging = GUILayout.Toggle(blnStaging, "Staging Enabled", styleToggle);
            if (blnStaging)
            {
                blnStagingInMapView = GUILayout.Toggle(blnStagingInMapView, "Allow Staging in Mapview", styleToggle);
                if (blnStagingInMapView)
                    blnStagingSpaceInMapView = GUILayout.Toggle(blnStagingSpaceInMapView, "Allow Space Bar in Mapview", styleToggle);
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

        }


        private Boolean IsMouseOver() 
        {
            //are we painting?
            Boolean blnRet = Event.current.type == EventType.Repaint;

            //And the mouse is over the button
            blnRet = blnRet && rectButton.Contains(Event.current.mousePosition);

            //mouse in main window
            blnRet = blnRet || (Drawing && _WindowMainRect.Contains(Event.current.mousePosition));

            ////or, the form was on the screen and the mouse is over that rectangle
            //blnRet = blnRet || (Drawing && rectPanel.Contains(Event.current.mousePosition));

            ////or, the settings form was on the screen and the mouse is over that rectangle
            blnRet = blnRet || (DrawingSettings && _WindowSettingsRect.Contains(Event.current.mousePosition));

            return blnRet;

        }
                        
        #region "Tooltip Work"
        //Tooltip variables
        //Store the tooltip text from throughout the code
        String strToolTipText = "";
        String strLastTooltipText = "";
        //is it displayed and where
        Boolean blnToolTipDisplayed = false;
        Rect rectToolTipPosition;
        int intTooltipVertOffset = 12;
        int intTooltipMaxWidth = 250;
        //timer so it only displays for a preriod of time
        float fltTooltipTime = 0f;
        float fltMaxToolTipTime = 15f;


        private void DrawToolTip()
        {
            if (strToolTipText != "" && (fltTooltipTime < fltMaxToolTipTime))
            {
                GUIContent contTooltip = new GUIContent(strToolTipText);
                if (!blnToolTipDisplayed || (strToolTipText != strLastTooltipText))
                {
                    //reset display time if text changed
                    fltTooltipTime = 0f;
                    //Calc the size of the Tooltip
                    rectToolTipPosition = new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y + intTooltipVertOffset, 0, 0);
                    float minwidth, maxwidth;
                    styleTooltipStyle.CalcMinMaxWidth(contTooltip, out minwidth, out maxwidth); // figure out how wide one line would be
                    rectToolTipPosition.width = Math.Min(intTooltipMaxWidth - styleTooltipStyle.padding.horizontal, maxwidth); //then work out the height with a max width
                    rectToolTipPosition.height = styleTooltipStyle.CalcHeight(contTooltip, rectToolTipPosition.width); // heers the result
                    //Make sure its not off the right of the screen
                    if (rectToolTipPosition.x + rectToolTipPosition.width > Screen.width) rectToolTipPosition.x = Screen.width - rectToolTipPosition.width;
                }
                //Draw the Tooltip
                GUI.Label(rectToolTipPosition, contTooltip, styleTooltipStyle);
                //On top of everything
                GUI.depth = 0;

                //update how long the tip has been on the screen and reset the flags
                fltTooltipTime += Time.deltaTime;
                blnToolTipDisplayed = true;
            }
            else
            {
                //clear the flags
                blnToolTipDisplayed = false;
            }
            if (strToolTipText != strLastTooltipText) fltTooltipTime = 0f;
            strLastTooltipText = strToolTipText;
        }

        public void SetTooltipText()
        {
            if (Event.current.type == EventType.Repaint)
            {
                strToolTipText = GUI.tooltip;
            }
        }
        #endregion

        
        /// <summary>
        /// Some Structured logging to the debug file
        /// </summary>
        /// <param name="Message"></param>
        public static void DebugLogFormatted(String Message, params object[] strParams)
        {
            Message = String.Format(Message, strParams);
            String strMessageLine = String.Format("{0},{2},{1}", DateTime.Now, Message, _ClassName);
            Debug.Log(strMessageLine);

        }
    }


    //[KSPAddon(KSPAddon.Startup.MainMenu, false)]
    //public class Debug_AutoLoadPersistentSaveOnStartup : MonoBehaviour
    //{
    //    public static bool first = true;
    //    public void Start()
    //    {
    //        if (first)
    //        {
    //            first = false;
    //            HighLogic.SaveFolder = "default";
    //            var game = GamePersistence.LoadGame("persistent", HighLogic.SaveFolder, true, false);
    //            if (game != null && game.flightState != null && game.compatible)
    //            {
    //                FlightDriver.StartAndFocusVessel(game, 6);
    //            }
    //            //CheatOptions.InfiniteFuel = true;
    //        }
    //    }
    //}

}