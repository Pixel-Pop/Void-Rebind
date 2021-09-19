using System.Collections.Generic;
using System.IO;
using MonoMod;
using UnityEngine;
using Rewired;

#pragma warning disable CS0626
#pragma warning disable CS0649

class patch_KeyboardOptionsScreen : KeyboardOptionsScreen
{
    /* Existing private fields */
    [MonoModIgnore]
    private int currentActionId;
    [MonoModIgnore]
    private Player player;
	[MonoModIgnore]
	private Pole? currentPole;

	/* New fields */
	private Mouse mouse;

    // InputMapper.Default is the keyboard input mapper.
    private InputMapper inputMapperMouse;

	// For drawBinding 
	ControllerMap keyboardMap;
	ControllerMap mouseMap;
	List<ActionElementMap> listMouse;

    private string savePathMouse
    {
        get
        {
            if (PlatformManager.Instance != null)
            {
                return PlatformManager.Instance.getDataPath("VB_Mousebindings");
            }
            return Application.persistentDataPath + "\\VB_Mousebindings.xml";
        }
    }

    private int elementMapIdToRemove;
    private ControllerMap controllerMapOfRemove;

    /* Methods */
    public extern void orig_KeyboardOptionsScreen();
    [MonoModConstructor]
    public void KeyboardOptionsScreen()
    {
        inputMapperMouse = new InputMapper();
        orig_KeyboardOptionsScreen();
    }


    private extern void orig_Start();
    private void Start()
    {
        if (!assignableActionNames.Contains("Attack"))
        {
            assignableActionNames.Add("Attack");
        }

        orig_Start();

        mouse = player.controllers.Mouse;

        inputMapperMouse.options.ignoreMouseXAxis = true;
        inputMapperMouse.options.ignoreMouseYAxis = true;
        inputMapperMouse.InputMappedEvent += OnInputMapped;
        inputMapperMouse.ConflictFoundEvent += OnConflictFound;
    }

    public void drawBindingFullBox(ActionElementMap item)
    {
        elementMapIdToRemove = item.id;
        controllerMapOfRemove = item.controllerMap;

        AxisRange actionRange = (currentPole.HasValue ? ((currentPole.GetValueOrDefault() == Pole.Positive && currentPole.HasValue) ? AxisRange.Positive : AxisRange.Negative) : AxisRange.Full);

        InputMapper.Context mappingContextKey = new InputMapper.Context()
        {
            actionId = currentActionId,
            controllerMap = keyboardMap,
            actionRange = actionRange
        };

        InputMapper.Context mappingContextMouse = new InputMapper.Context()
        {
            actionId = currentActionId,
            controllerMap = mouseMap,
            actionRange = actionRange
        };

        if (item.controllerMap == keyboardMap)
        {
            mappingContextKey.actionElementMapToReplace = item;
        } else
        {
            mappingContextMouse.actionElementMapToReplace = item;
        }

        InputMapper.Default.Start(mappingContextKey);
        inputMapperMouse.Start(mappingContextMouse);
    }

    public void drawBindingEmptyBox()
    {
        AxisRange actionRange = (currentPole.HasValue ? ((currentPole.GetValueOrDefault() == Pole.Positive && currentPole.HasValue) ? AxisRange.Positive : AxisRange.Negative) : AxisRange.Full);

        InputMapper.Default.Start(new InputMapper.Context()
        {
            actionId = currentActionId,
            controllerMap = keyboardMap,
            actionRange = actionRange
        });
        inputMapperMouse.Start(new InputMapper.Context()
        {
            actionId = currentActionId,
            controllerMap = mouseMap,
            actionRange = actionRange
        });

        controllerMapOfRemove = null;
    }

    [PatchDrawBinding]
    public extern float orig_drawBinding(InputAction action, string actionName, Pole? pole, float y);
    public new float drawBinding(InputAction action, string actionName, Pole? pole, float y)
    {
		keyboardMap = player.controllers.maps.GetMap(ControllerType.Keyboard, 0, "Default", "Default");
		mouseMap = player.controllers.maps.GetMap(ControllerType.Mouse, 0, "Default", "Default");

        listMouse = new List<ActionElementMap>();
        player.controllers.maps.GetElementMapsWithAction(mouse, action.id, skipDisabledMaps: true, listMouse);

        return orig_drawBinding(action, actionName, pole, y);
	}

    private extern void orig_OnInputMapped(InputMapper.InputMappedEventData data);
    private void OnInputMapped(InputMapper.InputMappedEventData data)
    {
        InputMapper.Default.Stop();
        inputMapperMouse.Stop();

        if (controllerMapOfRemove != null)
        {
            if (controllerMapOfRemove != data.actionElementMap.controllerMap)
            {
                controllerMapOfRemove.DeleteElementMap(elementMapIdToRemove);
            }
        }

        orig_OnInputMapped(data);
    }

    private extern void orig_OnConflictFound(InputMapper.ConflictFoundEventData data);
    private void OnConflictFound(InputMapper.ConflictFoundEventData data)
    {
        orig_OnConflictFound(data);
    }

    private extern void orig_Load();
    private void Load()
    {
        orig_Load();
        if (File.Exists(savePathMouse))
        {
            TextReader textReader = new StreamReader(savePathMouse);
            string item = textReader.ReadToEnd();
            List<string> list = new List<string>();
            list.Add(item);
            player.controllers.maps.ClearMaps(ControllerType.Mouse, userAssignableOnly: true);
            int num = player.controllers.maps.AddMapsFromXml(ControllerType.Mouse, 0, list);
            textReader.Close();

            Save();
        }
    }

    private extern void orig_Save();
    private void Save()
    {
        orig_Save();
        string value = player.GetSaveData(userAssignableMapsOnly: true).mouseMapSaveData[0].keyboardMap.ToXmlString();
        TextWriter textWriter2 = new StreamWriter(savePathMouse);
        textWriter2.WriteLine(value);
        textWriter2.Close();
    }

    private extern void orig_ClearSave();
    private void ClearSave()
    {
		player.controllers.maps.LoadDefaultMaps(ControllerType.Mouse);
		orig_ClearSave();
        if (File.Exists(savePathMouse))
        {
            File.Delete(savePathMouse);
        }
    }

    public extern void orig_OnExit(bool isBackNavigation);
    public void OnExit(bool isBackNavigation)
    {

        if (inputMapperMouse.status == InputMapper.Status.Listening)
        {
            inputMapperMouse.Stop();
            currentActionId = -1;
        }
        orig_OnExit(isBackNavigation);
    }
}
