using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Triturbo.FaceTrackingAddon.LayersControl.Runtime;
using VRC.SDK3.Avatars.Components;
using UnityEditor.Animations;
using System.Linq;
using System;
using UnityEditor;

namespace Triturbo.FaceTrackingAddon.LayersControl
{
    public static class LayerControlUtil
    {
        public static void Process(this LayersControlComponent component, VRCAvatarDescriptor avatardesciptor)
        {
            RuntimeAnimatorController controller = avatardesciptor.baseAnimationLayers[component.LayerIndex].animatorController;
            if (controller == null)
            {
                return;
            }
            AnimatorController animatorController = controller as AnimatorController;
            int[] targets = GetTargetLayers(component, avatardesciptor);


            if (component.m_RemoveBlendShapesInBanList)
                foreach (int targetIndex in targets)
                {
                    RemoveBlendshapes(animatorController.layers[targetIndex].stateMachine, component.m_BlendShapeBanList);
                }
            CreateLayer(component, animatorController, targets);
        }

        public static int[] GetTargetLayers(this LayersControlComponent component, VRCAvatarDescriptor avatardesciptor)
        {
            List<int> matchingLayerIndices = new List<int>(); // List to store matching layer indices

            AnimatorController animator = avatardesciptor.baseAnimationLayers[component.LayerIndex].animatorController as AnimatorController;
            if (animator == null)
            {
                return Array.Empty<int>();
            }
            for (int i = 0; i < animator.layers.Length; i++)
            {
                if (component.IsExclude(i))
                {
                    continue;
                }
                AnimatorStateMachine stateMachine = animator.layers[i].stateMachine;


                foreach (ChildAnimatorState state in stateMachine.states)
                {
                    // Check if the state's motion is an AnimationClip and if it is in the list of animations
                    if (state.state.motion is AnimationClip animationClip && component.m_Animations.Contains(animationClip))
                    {
                        matchingLayerIndices.Add(i);
                        break;
                    }
                }
            }
            return matchingLayerIndices.ToArray();
        }

        public static bool WriteDefault(AnimatorController animator)
        {
            for (int i = 0; i < animator.layers.Length; i++)
            {

                AnimatorStateMachine stateMachine = animator.layers[i].stateMachine;


                foreach (ChildAnimatorState state in stateMachine.states)
                {
                    if (!state.state.writeDefaultValues)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static void CreateLayer(this LayersControlComponent component, AnimatorController animatorController, int[] targets)
        {
            if(targets==null || targets.Length == 0)
            {
                return;
            }

            bool writeDefault = WriteDefault(animatorController);

            AnimatorControllerLayer layer = new AnimatorControllerLayer
            {
                name = "Disable Layer Control",
                stateMachine = new AnimatorStateMachine()
            };



            var clip = CreateAnimationClip();
            var idleState = layer.stateMachine.AddState("idle", new Vector3(0, 200, 0));


            idleState.motion = clip;
            idleState.writeDefaultValues = writeDefault;

            var disableState = layer.stateMachine.AddState("Disable", new Vector3(0, 300, 0));
            disableState.motion = clip;
            disableState.writeDefaultValues = writeDefault;


            foreach (int targetIndex in targets)
            {

                float defaultWeight = animatorController.layers[targetIndex].defaultWeight;
                if (defaultWeight == component.m_GoalWeight)
                {
                    continue;
                }


                var layerControlIdle = idleState.AddStateMachineBehaviour<VRC.SDK3.Avatars.Components.VRCAnimatorLayerControl>();

                var layerControlDisable = disableState.AddStateMachineBehaviour<VRC.SDK3.Avatars.Components.VRCAnimatorLayerControl>();


                layerControlIdle.layer = targetIndex;
                layerControlIdle.playable = VRC.SDKBase.VRC_AnimatorLayerControl.BlendableLayer.FX;
                layerControlIdle.blendDuration = component.m_BlendDuration;
                layerControlIdle.goalWeight = defaultWeight;


                layerControlDisable.layer = targetIndex;
                layerControlDisable.playable = VRC.SDKBase.VRC_AnimatorLayerControl.BlendableLayer.FX;
                layerControlDisable.blendDuration = component.m_BlendDuration;
                layerControlDisable.goalWeight = component.m_GoalWeight;
            }



            var transitionToDisable = idleState.AddTransition(disableState);
            transitionToDisable.AddCondition(AnimatorConditionMode.If, 1, component.m_Parameter);
            transitionToDisable.duration = 0;
            transitionToDisable.hasExitTime = false;


            var transitionToIdle = disableState.AddTransition(idleState);
            transitionToIdle.AddCondition(AnimatorConditionMode.IfNot, 1, component.m_Parameter);
            transitionToIdle.duration = 0;
            transitionToIdle.hasExitTime = false;


            if (!animatorController.parameters.Any(p => p.name == component.m_Parameter))
            {
                animatorController.AddParameter(component.m_Parameter, AnimatorControllerParameterType.Bool);
            }

            animatorController.AddLayer(layer);

            //AssetDatabase.AddObjectToAsset(b, ctx.AssetContainer);
            //AssetDatabase.AddObjectToAsset(newLayer.stateMachine, ctx.AssetContainer);

        }

        public static AnimationClip CreateAnimationClip()
        {
            // Create a new Animation Clip
            AnimationClip clip = new AnimationClip();
            clip.name = "Buffer_LayerDisable";

            // Define the Animation Curve for isActive property
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0.0f, 0.0f); // isActive at time 0s
            curve.AddKey(1.0f, 0.0f); // isActive at time 1s

            // Apply the Animation Curve to the isActive property
            clip.SetCurve("_Buffer", typeof(GameObject), "m_IsActive", curve);

            return clip;
        }


        public static void RemoveBlendshapes(AnimatorStateMachine stateMachine, IEnumerable<string> blacklistShapes)
        {

            foreach (var state in stateMachine.states)
            {

                var motion = state.state.motion;

                if (motion is AnimationClip)
                {
                    var clip = RemoveBlendshapes((AnimationClip)motion, blacklistShapes);
                    if (clip != null) state.state.motion = clip;

                }
            }

            foreach (var subStateMachine in stateMachine.stateMachines)
            {
                RemoveBlendshapes(subStateMachine.stateMachine, blacklistShapes);
            }
        }
        public static AnimationClip RemoveBlendshapes(AnimationClip clip, IEnumerable<string> blacklistShapes)
        {

            
            AnimationClip newClip = UnityEngine.Object.Instantiate(clip);
            newClip.name = clip.name + "(Triturbo LayerControl Clone)";

            var bindings = AnimationUtility.GetCurveBindings(newClip);

            bool edited = false;
            foreach (var binding in bindings)
            {
                if (binding.type == typeof(SkinnedMeshRenderer) && blacklistShapes.Contains(binding.propertyName.Substring("blendShape.".Length)))
                {
                    AnimationUtility.SetEditorCurve(newClip, binding, null);
                    edited = true;
                }
            }

            return edited ? newClip : null;
        }
    }
}



