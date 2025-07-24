
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using System;

#if ENABLE_MA
using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;
#endif

namespace Triturbo.FaceTrackingAddon
{
    public static class ParameterHelper
    {
        public static int GetAvatarParameterMemoryUsed(VRCAvatarDescriptor avatarDescriptor)
        {
            int parameterCount = avatarDescriptor.GetExpressionParameterCount();
            int totalCost = 0;

            //VRC
            for (int i = 0; i < parameterCount; i++)
            {
                VRCExpressionParameters.Parameter parameter = avatarDescriptor.GetExpressionParameter(i);

                if (parameter.networkSynced == true)
                {
                    VRCExpressionParameters.ValueType valueType = parameter.valueType;
                    totalCost += VRCExpressionParameters.TypeCost(valueType);
                }

            }
#if ENABLE_MA

            try
            {
                totalCost = ParameterInfo.ForUI.GetParametersForObject(avatarDescriptor.gameObject).Sum(p => p.BitUsage);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

#endif
            return totalCost;
        }
        public static int GetMAParameterMemoryUsed(GameObject maObject)
        {
            if (maObject == null) return 0;
#if ENABLE_MA
            //ModularAvatarParameters map = maObject?.GetComponent<ModularAvatarParameters>();
            //if (map == null)
            //    return 0;

            return ParameterInfo.ForUI.GetParametersForObject(maObject).Sum(p => p.BitUsage);;
#else
            return 0;
#endif
        }
#if ENABLE_MA
        //public static int GetMAParameterMemoryUsed(ModularAvatarParameters map)
        //{

        //    return GetMAParameterMemoryUsed(map.parameters);
        //}
        //public static int GetMAParameterMemoryUsed(List<ParameterConfig> maParams)
        //{
        //    int totalBits = 0;

        //    HashSet<ParameterConfig> uniqueSet = new HashSet<ParameterConfig>(maParams);
        //    foreach (var p in uniqueSet)
        //    {
        //        if (p.localOnly)
        //            continue;
        //        switch (p.syncType)
        //        {
        //            case ParameterSyncType.Bool:
        //                totalBits += 1;
        //                break;

        //            case ParameterSyncType.Float:
        //                totalBits += 8;
        //                break;
        //            case ParameterSyncType.Int:
        //                totalBits += 8;
        //                break;

        //            case ParameterSyncType.NotSynced:
        //                break;
        //            default:
        //                break;
        //        }

        //    }
        //    return totalBits;
        //}
#endif

    }
}

