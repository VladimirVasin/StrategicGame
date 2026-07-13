using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.InputSystem;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyInputActionsContractTests
    {
        private const string AssetPath = "Assets/InputSystem_Actions.inputactions";
        private const string AssetGuid = "2bcd2660ca9b64942af0de543d8d7100";

        private static readonly string[] CanonicalUiActions =
        {
            "Navigate|c95b2375-e6d9-4b88-9c4c-c5e76515df4b|PassThrough|Vector2|||False",
            "Submit|7607c7b6-cd76-4816-beef-bd0341cfe950|Button|Button|||False",
            "Cancel|15cef263-9014-4fd5-94d9-4e4a6234a6ef|Button|Button|||False",
            "Point|32b35790-4ed0-4e9a-aa41-69ac6d629449|PassThrough|Vector2|||True",
            "Click|3c7022bf-7922-4f7c-a998-c437916075ad|PassThrough|Button|||True",
            "RightClick|44b200b1-1557-4083-816c-b22cbdf77ddf|PassThrough|Button|||False",
            "MiddleClick|dad70c86-b58c-4b17-88ad-f5e53adf419e|PassThrough|Button|||False",
            "ScrollWheel|0489e84a-4833-4c40-bfae-cea84b696689|PassThrough|Vector2|||False",
            "TrackedDevicePosition|24908448-c609-4bc3-a128-ea258674378a|PassThrough|Vector3|||False",
            "TrackedDeviceOrientation|9caa3d8a-6b2f-4e8e-8bad-6ede561bd9be|PassThrough|Quaternion|||False"
        };

        private static readonly string[] CanonicalUiBindings =
        {
            "Gamepad|809f371f-c5e2-4e7a-83a1-d867598f40dd|2DVector||Navigate|True|False||",
            "up|14a5d6e8-4aaf-4119-a9ef-34b8c2c548bf|<Gamepad>/leftStick/up|;Gamepad|Navigate|False|True||",
            "up|9144cbe6-05e1-4687-a6d7-24f99d23dd81|<Gamepad>/rightStick/up|;Gamepad|Navigate|False|True||",
            "down|2db08d65-c5fb-421b-983f-c71163608d67|<Gamepad>/leftStick/down|;Gamepad|Navigate|False|True||",
            "down|58748904-2ea9-4a80-8579-b500e6a76df8|<Gamepad>/rightStick/down|;Gamepad|Navigate|False|True||",
            "left|8ba04515-75aa-45de-966d-393d9bbd1c14|<Gamepad>/leftStick/left|;Gamepad|Navigate|False|True||",
            "left|712e721c-bdfb-4b23-a86c-a0d9fcfea921|<Gamepad>/rightStick/left|;Gamepad|Navigate|False|True||",
            "right|fcd248ae-a788-4676-a12e-f4d81205600b|<Gamepad>/leftStick/right|;Gamepad|Navigate|False|True||",
            "right|1f04d9bc-c50b-41a1-bfcc-afb75475ec20|<Gamepad>/rightStick/right|;Gamepad|Navigate|False|True||",
            "|fb8277d4-c5cd-4663-9dc7-ee3f0b506d90|<Gamepad>/dpad|;Gamepad|Navigate|False|False||",
            "Joystick|e25d9774-381c-4a61-b47c-7b6b299ad9f9|2DVector||Navigate|True|False||",
            "up|3db53b26-6601-41be-9887-63ac74e79d19|<Joystick>/stick/up|Joystick|Navigate|False|True||",
            "down|0cb3e13e-3d90-4178-8ae6-d9c5501d653f|<Joystick>/stick/down|Joystick|Navigate|False|True||",
            "left|0392d399-f6dd-4c82-8062-c1e9c0d34835|<Joystick>/stick/left|Joystick|Navigate|False|True||",
            "right|942a66d9-d42f-43d6-8d70-ecb4ba5363bc|<Joystick>/stick/right|Joystick|Navigate|False|True||",
            "Keyboard|ff527021-f211-4c02-933e-5976594c46ed|2DVector||Navigate|True|False||",
            "up|563fbfdd-0f09-408d-aa75-8642c4f08ef0|<Keyboard>/w|Keyboard&Mouse|Navigate|False|True||",
            "up|eb480147-c587-4a33-85ed-eb0ab9942c43|<Keyboard>/upArrow|Keyboard&Mouse|Navigate|False|True||",
            "down|2bf42165-60bc-42ca-8072-8c13ab40239b|<Keyboard>/s|Keyboard&Mouse|Navigate|False|True||",
            "down|85d264ad-e0a0-4565-b7ff-1a37edde51ac|<Keyboard>/downArrow|Keyboard&Mouse|Navigate|False|True||",
            "left|74214943-c580-44e4-98eb-ad7eebe17902|<Keyboard>/a|Keyboard&Mouse|Navigate|False|True||",
            "left|cea9b045-a000-445b-95b8-0c171af70a3b|<Keyboard>/leftArrow|Keyboard&Mouse|Navigate|False|True||",
            "right|8607c725-d935-4808-84b1-8354e29bab63|<Keyboard>/d|Keyboard&Mouse|Navigate|False|True||",
            "right|4cda81dc-9edd-4e03-9d7c-a71a14345d0b|<Keyboard>/rightArrow|Keyboard&Mouse|Navigate|False|True||",
            "|9e92bb26-7e3b-4ec4-b06b-3c8f8e498ddc|*/{Submit}|Keyboard&Mouse;Gamepad;Touch;Joystick;XR|Submit|False|False||",
            "|82627dcc-3b13-4ba9-841d-e4b746d6553e|*/{Cancel}|Keyboard&Mouse;Gamepad;Touch;Joystick;XR|Cancel|False|False||",
            "|c52c8e0b-8179-41d3-b8a1-d149033bbe86|<Mouse>/position|Keyboard&Mouse|Point|False|False||",
            "|e1394cbc-336e-44ce-9ea8-6007ed6193f7|<Pen>/position|Keyboard&Mouse|Point|False|False||",
            "|5693e57a-238a-46ed-b5ae-e64e6e574302|<Touchscreen>/touch*/position|Touch|Point|False|False||",
            "|4faf7dc9-b979-4210-aa8c-e808e1ef89f5|<Mouse>/leftButton|;Keyboard&Mouse|Click|False|False||",
            "|8d66d5ba-88d7-48e6-b1cd-198bbfef7ace|<Pen>/tip|;Keyboard&Mouse|Click|False|False||",
            "|47c2a644-3ebc-4dae-a106-589b7ca75b59|<Touchscreen>/touch*/press|Touch|Click|False|False||",
            "|bb9e6b34-44bf-4381-ac63-5aa15d19f677|<XRController>/trigger|XR|Click|False|False||",
            "|38c99815-14ea-4617-8627-164d27641299|<Mouse>/scroll|;Keyboard&Mouse|ScrollWheel|False|False||",
            "|4c191405-5738-4d4b-a523-c6a301dbf754|<Mouse>/rightButton|Keyboard&Mouse|RightClick|False|False||",
            "|24066f69-da47-44f3-a07e-0015fb02eb2e|<Mouse>/middleButton|Keyboard&Mouse|MiddleClick|False|False||",
            "|7236c0d9-6ca3-47cf-a6ee-a97f5b59ea77|<XRController>/devicePosition|XR|TrackedDevicePosition|False|False||",
            "|23e01e3a-f935-4948-8d8b-9bcac77714fb|<XRController>/deviceRotation|XR|TrackedDeviceOrientation|False|False||"
        };

        [Test]
        public void StrategyMapsExposeTheKeyboardMouseContract()
        {
            InputActionAsset asset = LoadAsset();
            CollectionAssert.AreEqual(
                new[] { "Global", "Camera", "Gameplay", "Build", "Debug", "UI" },
                asset.actionMaps.Select(map => map.name).ToArray());
            Assert.That(asset.FindActionMap("Player", false), Is.Null);

            AssertAction(asset, "Global", "Cancel", InputActionType.Button, "Button", "<Keyboard>/escape");
            AssertAction(asset, "Global", "Save", InputActionType.Button, "Button", "<Keyboard>/f5");
            AssertAction(asset, "Global", "Load", InputActionType.Button, "Button", "<Keyboard>/f8");
            AssertAction(asset, "Global", "Speed1", InputActionType.Button, "Button", "<Keyboard>/f1");
            AssertAction(asset, "Global", "Speed2", InputActionType.Button, "Button", "<Keyboard>/f2");
            AssertAction(asset, "Global", "Speed3", InputActionType.Button, "Button", "<Keyboard>/f3");

            AssertAction(asset, "Camera", "Pan", InputActionType.Value, "Vector2",
                "2DVector", "<Keyboard>/w", "<Keyboard>/s", "<Keyboard>/a", "<Keyboard>/d",
                "2DVector", "<Keyboard>/upArrow", "<Keyboard>/downArrow",
                "<Keyboard>/leftArrow", "<Keyboard>/rightArrow");
            AssertAction(asset, "Camera", "FocusCamp", InputActionType.Button, "Button", "<Keyboard>/space");
            AssertAction(asset, "Camera", "ZoomKeys", InputActionType.Value, "Axis",
                "1DAxis", "<Keyboard>/q", "<Keyboard>/minus", "<Keyboard>/numpadMinus",
                "<Keyboard>/e", "<Keyboard>/equals", "<Keyboard>/numpadPlus");
            AssertAction(asset, "Camera", "PointerPosition", InputActionType.PassThrough, "Vector2", "<Mouse>/position");
            AssertAction(asset, "Camera", "PointerDelta", InputActionType.PassThrough, "Vector2", "<Mouse>/delta");
            AssertAction(asset, "Camera", "Scroll", InputActionType.PassThrough, "Vector2", "<Mouse>/scroll");
            AssertAction(asset, "Camera", "MiddleDrag", InputActionType.Button, "Button", "<Mouse>/middleButton");
            AssertAction(asset, "Camera", "RightDrag", InputActionType.Button, "Button", "<Mouse>/rightButton");

            AssertAction(asset, "Gameplay", "PrimaryClick", InputActionType.Button, "Button", "<Mouse>/leftButton");
            AssertAction(asset, "Gameplay", "DeleteSelection", InputActionType.Button, "Button", "<Keyboard>/delete");
            AssertAction(asset, "Build", "Toggle", InputActionType.Button, "Button", "<Keyboard>/b");
            AssertAction(asset, "Build", "Place", InputActionType.Button, "Button", "<Mouse>/leftButton");
            AssertAction(asset, "Build", "CancelPointer", InputActionType.Button, "Button", "<Mouse>/rightButton");
            for (int slot = 1; slot <= 9; slot++)
            {
                AssertAction(asset, "Build", "Slot" + slot, InputActionType.Button, "Button",
                    "<Keyboard>/" + slot, "<Keyboard>/numpad" + slot);
            }

            AssertAction(asset, "Debug", "Toggle", InputActionType.Button, "Button", "<Keyboard>/f9");
        }

        [Test]
        public void UiMapAndControlSchemesRetainTheirCanonicalContract()
        {
            InputActionAsset asset = LoadAsset();
            InputActionMap ui = asset.FindActionMap("UI", true);
            Assert.That(ui.id.ToString("D"), Is.EqualTo("272f6d14-89ba-496f-b7ff-215263d3219f"));
            CollectionAssert.AreEqual(CanonicalUiActions, ui.actions.Select(ActionSignature).ToArray());
            CollectionAssert.AreEqual(CanonicalUiBindings, ui.bindings.Select(BindingSignature).ToArray());
            CollectionAssert.AreEqual(
                new[]
                {
                    "Keyboard&Mouse|Keyboard&Mouse|<Keyboard>:False:False,<Mouse>:False:False",
                    "Gamepad|Gamepad|<Gamepad>:False:False",
                    "Touch|Touch|<Touchscreen>:False:False",
                    "Joystick|Joystick|<Joystick>:False:False",
                    "XR|XR|<XRController>:False:False"
                },
                asset.controlSchemes.Select(ControlSchemeSignature).ToArray());
            Assert.That(AssetDatabase.AssetPathToGUID(AssetPath), Is.EqualTo(AssetGuid));
        }

        private static InputActionAsset LoadAsset()
        {
            InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetPath);
            Assert.That(asset, Is.Not.Null, "Strategy input actions asset is missing or failed to import");
            return asset;
        }

        private static void AssertAction(
            InputActionAsset asset,
            string mapName,
            string actionName,
            InputActionType type,
            string expectedControlType,
            params string[] bindingPaths)
        {
            InputActionMap map = asset.FindActionMap(mapName, true);
            InputAction action = map.FindAction(actionName, true);
            Assert.That(action.type, Is.EqualTo(type), mapName + "/" + actionName + " type");
            Assert.That(action.expectedControlType, Is.EqualTo(expectedControlType),
                mapName + "/" + actionName + " control type");
            CollectionAssert.AreEqual(bindingPaths, action.bindings.Select(binding => binding.path).ToArray(),
                mapName + "/" + actionName + " bindings");
        }

        private static string ActionSignature(InputAction action)
        {
            return string.Join("|",
                action.name,
                action.id.ToString("D"),
                action.type.ToString(),
                action.expectedControlType ?? string.Empty,
                action.processors ?? string.Empty,
                action.interactions ?? string.Empty,
                action.wantsInitialStateCheck.ToString());
        }

        private static string BindingSignature(InputBinding binding)
        {
            return string.Join("|",
                binding.name ?? string.Empty,
                binding.id.ToString("D"),
                binding.path ?? string.Empty,
                binding.groups ?? string.Empty,
                binding.action ?? string.Empty,
                binding.isComposite.ToString(),
                binding.isPartOfComposite.ToString(),
                binding.processors ?? string.Empty,
                binding.interactions ?? string.Empty);
        }

        private static string ControlSchemeSignature(InputControlScheme scheme)
        {
            List<string> devices = new();
            for (int i = 0; i < scheme.deviceRequirements.Count; i++)
            {
                InputControlScheme.DeviceRequirement requirement = scheme.deviceRequirements[i];
                devices.Add(requirement.controlPath
                    + ":" + requirement.isOptional
                    + ":" + requirement.isOR);
            }

            return scheme.name + "|" + scheme.bindingGroup + "|" + string.Join(",", devices);
        }
    }
}
