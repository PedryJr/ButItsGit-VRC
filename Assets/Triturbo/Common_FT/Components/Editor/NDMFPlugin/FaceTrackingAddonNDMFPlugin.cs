#if ENABLE_NDMF

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Animations;
using UnityEditor;
using System.Linq;
using Triturbo.FaceTrackingAddon.NDMF;
using nadena.dev.ndmf;
using VRC.SDK3.Avatars.Components;
using System;
using Triturbo.FaceTrackingAddon.LayersControl;

using Triturbo.FaceTrackingAddon.LayersControl.Runtime;
using Triturbo.FaceTrackingAddon.EyeTrackingSettings.Runtime;
using Triturbo.FaceTrackingAddon.EyeTrackingSettings;


using Triturbo.FaceTrackingAddon.EmoteLayersModifier;

using Triturbo.FaceTrackingAddon.EmoteLayersModifier.Runtime;
using System.IO;
using VRC.SDKBase.Editor;
using VRC.Core;


[assembly: ExportsPlugin(typeof(FaceTrackingAddonNDMFPlugin))]
namespace Triturbo.FaceTrackingAddon.NDMF
{
    public class FaceTrackingAddonNDMFPlugin : Plugin<FaceTrackingAddonNDMFPlugin>
    {
        protected override void Configure()
        {
            InPhase(BuildPhase.Resolving).Run("Face Tracking - Emote Layer Modify", ctx => {

            });

            InPhase(BuildPhase.Generating).Run("Face Tracking Addon", ctx =>
            {

                foreach (var component in ctx.AvatarRootObject.GetComponentsInChildren<LayersControlComponent>())
                {
                    component.Process(ctx.AvatarDescriptor);
                }

                foreach (var component in ctx.AvatarRootObject.GetComponentsInChildren<EmoteLayersModifierComponent>())
                {
                    component.Process(ctx.AvatarDescriptor);
                }

                var eyeSettings = ctx.AvatarRootObject.GetComponentInChildren<EyeTrackingSettingsComponent>();
                if (eyeSettings != null && eyeSettings.GetBody() != null)
                {
                    Animator animator = ctx.AvatarRootObject.GetComponent<Animator>();
                    RuntimeAnimatorController controller = null;

                    if (animator != null)
                    {
                        controller = animator.runtimeAnimatorController;
                        animator.runtimeAnimatorController = null;
                    }
                    eyeSettings.m_ExpressionBlendShapes.SplitExpressionWeight(eyeSettings.Body);

#if ENABLE_MA
                    eyeSettings.ApplyParameters();
#endif
                    if (animator != null)
                    {
                        animator.runtimeAnimatorController = controller;
                    }
                }



            });
        }
    }
}

#endif