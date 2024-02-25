
#if ENABLE_VR && ENABLE_XR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
#endif

namespace VoxelPlay {

    public static class VRCheck {

        public static bool isEnabled, isVrRunning;

#if ENABLE_VR && ENABLE_XR
        static List<XRDisplaySubsystemDescriptor> displaysDescs = new List<XRDisplaySubsystemDescriptor>();
        static List<XRDisplaySubsystem> displays = new List<XRDisplaySubsystem>();

        public static void Init() {
            isActive = IsActive();
            isVrRunning = IsVrRunning();
        }
        static bool IsActive() {
            displaysDescs.Clear();
            SubsystemManager.GetSubsystemDescriptors(displaysDescs);

            // If there are registered display descriptors that is a good indication that VR is most likely "enabled"
            return displaysDescs.Count > 0;
        }

        static bool IsVrRunning() {
            bool vrIsRunning = false;
            displays.Clear();
            SubsystemManager.GetInstances(displays);
            foreach (var displaySubsystem in displays) {
                if (displaySubsystem.running) {
                    vrIsRunning = true;
                    break;
                }
            }

            return vrIsRunning;
        }
#else
        public static void Init() {
            isEnabled = isVrRunning = false;
        }
#endif

    }

}