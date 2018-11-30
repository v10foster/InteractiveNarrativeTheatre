using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System;

[Serializable]
public class TheatreAction
{
    string actionType;
    string actor;
    string action;
    string location;
    string time;
    string[] props;
}

[Serializable]
public class TheatreFrame
{
    string[] characters;
    public string[] props_in_scene;
    string[] location_in_scene;
    string[] time;
    TheatreAction[] actions;
}
