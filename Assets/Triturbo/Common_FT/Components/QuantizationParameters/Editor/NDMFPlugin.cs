#if ENABLE_NDMF && ENABLE_MA

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using nadena.dev.ndmf;
using Triturbo.QuantizationParameters;


[assembly: ExportsPlugin(typeof(NDMFPlugin))]

namespace Triturbo.QuantizationParameters{
    public class NDMFPlugin : Plugin<NDMFPlugin>
    {
        public override string QualifiedName => "triturbo.facetracking.quantizationPrameters";
        public override string DisplayName => "Triturbo FT - Quantization Prameters";
        protected override void Configure()
        {
            InPhase(BuildPhase.Generating).Run("FaceTracking Addon ModularSmooth", ctx => {
                QuantizationParametersUtil.Execute(ctx);
            });
        }
    }
}

#endif