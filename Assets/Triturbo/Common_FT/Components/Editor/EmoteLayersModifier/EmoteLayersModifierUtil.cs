using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Triturbo.FaceTrackingAddon.EmoteLayersModifier.Runtime;


using VRC.SDK3.Avatars.Components;
using UnityEditor.Animations;
using System.Linq;
using System;
using UnityEditor;


namespace Triturbo.FaceTrackingAddon.EmoteLayersModifier
{
    public static class EmoteLayersModifierUtil 
    {
        public static void Process(this EmoteLayersModifierComponent component, VRCAvatarDescriptor avatardesciptor)
        {
            RuntimeAnimatorController controller = avatardesciptor.baseAnimationLayers[4].animatorController;
            if (controller == null)
            {
                return;
            }
            AnimatorController animatorController = controller as AnimatorController;


            // Add parameter
            if (!animatorController.parameters.Any(p => p.name == component.m_Parameter))
            {
                animatorController.AddParameter(component.m_Parameter, AnimatorControllerParameterType.Bool);
            }

            int[] targets = component.GetTargetLayers(avatardesciptor);


            bool writeDefault = WriteDefault(animatorController);

            AnimationClip defaultClip = null;

            if (!writeDefault && component.m_AutoAnimation)
            {
                var body = FindByPath(avatardesciptor.transform, component.m_MeshPath)?.GetComponent<SkinnedMeshRenderer>();


                HashSet<AnimationClip> animationClips = new HashSet<AnimationClip>();
                foreach (int index in targets)
                {
                    if ((animatorController.layers.Length <= index)) continue;

                    if (component.IsExclude(animatorController.layers[index].name)) continue;

                    var sm = animatorController.layers[index].stateMachine;
                    GetClipsFromStateMachine(sm, animationClips);

                }
                if (body != null)
                {
                    defaultClip = GenerateDefaultClip(body, component.m_MeshPath, GetAnimatedBlendshapes(animationClips, component.m_MeshPath));
                    defaultClip.name = "EmoteLayersModifierDefaultClip";
                }
            }
            else
            {
                defaultClip = component.m_DefaultAnimation;
            }




            foreach (int index in targets)
            {
                if ((animatorController.layers.Length <= index)) continue;

                if (component.IsExclude(animatorController.layers[index].name)) continue;

                var sm = animatorController.layers[index].stateMachine;

                foreach (var t in sm.anyStateTransitions)
                {
                    t.AddCondition(AnimatorConditionMode.IfNot, 0, component.m_Parameter);
                }

                if(component.m_DoOverwriteTrackingControl)
                {
                    foreach (var state in sm.states)
                    {
                        foreach (var behave in state.state.behaviours)
                        {
                            if (behave is VRCAnimatorTrackingControl trackingControl)
                            {
                                trackingControl.trackingEyes = component.m_OverwriteTrackingEyes;
                                trackingControl.trackingMouth = component.m_OverwriteTrackingMouth;
                            }
                        }
                    }

                }

                var defaultState = sm.AddState("EmoteLayersModifierDefault", sm.anyStatePosition + new Vector3(0, 200, 0));

                if(component.m_TrackingEyes != VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType.NoChange || 
                    component.m_TrackingMouth != VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType.NoChange)
                {
                    var behaviour = defaultState.AddStateMachineBehaviour<VRC.SDK3.Avatars.Components.VRCAnimatorTrackingControl>();
                    behaviour.trackingEyes = component.m_TrackingEyes;
                    behaviour.trackingMouth = component.m_TrackingMouth;
                }

                if (defaultClip != null)
                {
                    defaultState.motion = defaultClip;
                }
                defaultState.writeDefaultValues = writeDefault;

                var anyToDefault = sm.AddAnyStateTransition(defaultState);

               
                anyToDefault.AddCondition(AnimatorConditionMode.If, 0, component.m_Parameter);
                anyToDefault.canTransitionToSelf = false;
                anyToDefault.duration = component.m_Duration;
                anyToDefault.hasExitTime = false;

                var exit = defaultState.AddExitTransition();
                exit.AddCondition(AnimatorConditionMode.IfNot, 0, component.m_Parameter);
                exit.duration = component.m_Duration;
                exit.hasExitTime = false;


            }
        }

        public static Transform FindByPath(Transform root, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return root;
            }

            string[] pathSegments = path.Split('/');
            Transform current = root.transform;

            foreach (string segment in pathSegments)
            {
                current = current.Find(segment);
                if (current == null)
                {
                    return null; // Return null if any segment is not found
                }
            }

            return current;
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

        public static int[] GetTargetLayers(this EmoteLayersModifierComponent component, VRCAvatarDescriptor avatardesciptor)
        {
            List<int> matchingLayerIndices = new List<int>(); // List to store matching layer indices

            AnimatorController animator = avatardesciptor.baseAnimationLayers[4].animatorController as AnimatorController;


            return component.GetTargetLayers(animator);
        }
        public static int[] GetTargetLayers(this EmoteLayersModifierComponent component, AnimatorController animator)
        {
            if (animator == null)
            {
                return Array.Empty<int>();
            }

            
            if (component.m_LayerParameters == null)
            {
                component.m_LayerParameters = new string[] { "GestureLeft", "GestureRight" };
            }

            List<int> matchingLayerIndices = new List<int>(); // List to store matching layer indices


            for (int i = 0; i < animator.layers.Length; i++)
            {
                AnimatorStateMachine stateMachine = animator.layers[i].stateMachine;

                if (!IsAnimating(stateMachine, component.m_MeshPath)) continue;

                if (stateMachine.anyStateTransitions.Any(t => t.conditions.Any(c => component.m_LayerParameters.Contains(c.parameter))))
                {
                    matchingLayerIndices.Add(i);
                    continue;
                }


                foreach (ChildAnimatorState state in stateMachine.states)
                {
                    if (HasParametersCondition(state.state.transitions, component.m_LayerParameters))
                    {
                        matchingLayerIndices.Add(i);
                        break;
                    }
                }
            }
            return matchingLayerIndices.ToArray();
        }
        private static bool HasParametersCondition(AnimatorStateTransition[] transitions, string[] parameters)
        {
            if(parameters == null)
            {
                return false;
            }
            foreach (var transition in transitions)
            {
                if (transition.conditions.Any(c => parameters.Contains(c.parameter)))
                {
                    return true;
                }
            }

            return false;
        }
        private static bool HasGestureCondition(AnimatorStateTransition[] transitions)
        {
            foreach (var transition in transitions)
            {
                if (transition.conditions.Any(c => c.parameter == "GestureLeft" || c.parameter == "GestureRight"))
                {
                    return true;
                }
            }

            return false;
        }

        // return true if clip is animatig a SkinnedMeshRenderer with the path ;
        public static bool IsAnimating(AnimationClip clip, string path)
        {
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);

            foreach (EditorCurveBinding binding in bindings)
            {
                if (binding.type == typeof(SkinnedMeshRenderer) && binding.path == path)
                {
                    return true;
                }
            }
            return false;
        }
        public static bool IsAnimating(AnimatorStateMachine stateMachine, string path)
        {
            foreach (var state in stateMachine.states)
            {
                AnimatorState animatorState = state.state;
                if (animatorState.motion is AnimationClip)
                {
                    if (IsAnimating(animatorState.motion as AnimationClip, path))
                    {
                        return true;
                    }
                }
                else if (animatorState.motion is BlendTree)
                {
                    if (IsAnimating(animatorState.motion as BlendTree, path))
                    {
                        return true;
                    }
                }
            }

            foreach (var subStateMachine in stateMachine.stateMachines)
            {
                if (IsAnimating(subStateMachine.stateMachine, path))
                {
                    return true;
                }
            }

            return false;
        }
        public static bool IsAnimating(BlendTree blendTree, string path)
        {
            foreach (var child in blendTree.children)
            {
                if (child.motion is AnimationClip)
                {
                    if (IsAnimating(child.motion as AnimationClip, path))
                    {
                        return true;
                    }
                }
                else if (child.motion is BlendTree)
                {
                    
                    if (IsAnimating(child.motion as BlendTree, path))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static AnimationClip GenerateDefaultClip(SkinnedMeshRenderer body, string path, HashSet<string> blendshapes)
        {
            AnimationClip clip = new AnimationClip();
            //clip.legacy = true; // Make the clip legacy if needed

            foreach (string blendshape in blendshapes)
            {
                int index = body.sharedMesh.GetBlendShapeIndex(blendshape);
                if (index != -1)
                {
                    string propertyName = $"blendShape.{blendshape}";
                    AnimationCurve curve = new AnimationCurve(new Keyframe(0, body.GetBlendShapeWeight(index)));
                    clip.SetCurve(path, typeof(SkinnedMeshRenderer), propertyName, curve);
                }
                else
                {
                    Debug.LogWarning($"Blendshape '{blendshape}' not found in the SkinnedMeshRenderer.");
                }
            }

            return clip;
        }


        public static HashSet<string> GetAnimatedBlendshapes(IEnumerable<AnimationClip> clips, string meshPath)
        {
            HashSet<string> blendshapes = new HashSet<string>();

            foreach (AnimationClip clip in clips)
            {
                EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);

                foreach (EditorCurveBinding binding in bindings)
                {
                    if (binding.type == typeof(SkinnedMeshRenderer) && binding.path == meshPath)
                    {
                        if (binding.propertyName.StartsWith("blendShape."))
                        {
                            string blendshapeName = binding.propertyName.Replace("blendShape.", "");
                            blendshapes.Add(blendshapeName);
                        }
                    }
                }
            }

            return blendshapes;
        }
        public static void GetClipsFromStateMachine(AnimatorStateMachine stateMachine, HashSet<AnimationClip> clips)
        {
            foreach (var state in stateMachine.states)
            {
                AnimatorState animatorState = state.state;
                if (animatorState.motion is AnimationClip)
                {
                    clips.Add(animatorState.motion as AnimationClip);
                }
                else if (animatorState.motion is BlendTree)
                {
                    GetClipsFromBlendTree(animatorState.motion as BlendTree, clips);
                }
            }

            foreach (var subStateMachine in stateMachine.stateMachines)
            {
                GetClipsFromStateMachine(subStateMachine.stateMachine, clips);
            }
        }

        public static void GetClipsFromBlendTree(BlendTree blendTree, HashSet<AnimationClip> clips)
        {
            foreach (var child in blendTree.children)
            {
                if (child.motion is AnimationClip)
                {
                    clips.Add(child.motion as AnimationClip);
                }
                else if (child.motion is BlendTree)
                {
                    GetClipsFromBlendTree(child.motion as BlendTree, clips);
                }
            }
        }
    }
}

