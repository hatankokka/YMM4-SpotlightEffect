using System;
using System.Numerics;
using Vortice.Direct2D1;
using Vortice.Direct2D1.Effects;
using Vortice.Mathematics;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;

namespace SpotlightEffect
{
    /// <summary>
    /// 矩形スポットライト Processor（px指定版）。
    ///
    /// 元映像そのものはぼかさず、白い矩形マスクだけをGaussianBlurする。
    /// そのマスクをAlphaMaskとして元映像に適用し、暗化映像の上に重ねる。
    /// </summary>
    internal sealed class SpotlightVideoEffectProcessor : IVideoEffectProcessor
    {
        readonly SpotlightVideoEffect item;
        readonly ID2D1DeviceContext deviceContext;

        readonly ColorMatrix darkenEffect;
        readonly ID2D1Image darkenOutput;

        readonly Flood maskFloodEffect;
        readonly ID2D1Image maskFloodOutput;

        readonly Crop maskCropEffect;
        readonly ID2D1Image maskCropOutput;

        readonly GaussianBlur maskBlurEffect;
        readonly ID2D1Image maskBlurOutput;

        readonly AlphaMask alphaMaskEffect;
        readonly ID2D1Image maskedSourceOutput;

        readonly Composite compositeEffect;

        bool isFirst = true;
        double prevDarkness = double.NaN;
        double prevEdgeBlur = double.NaN;
        double prevCenterX = double.NaN;
        double prevCenterY = double.NaN;
        double prevSpotWidth = double.NaN;
        double prevSpotHeight = double.NaN;

        public ID2D1Image Output { get; }

        public SpotlightVideoEffectProcessor(IGraphicsDevicesAndContext devices, SpotlightVideoEffect item)
        {
            this.item = item;
            deviceContext = devices.DeviceContext;

            darkenEffect = new ColorMatrix(deviceContext);
            darkenOutput = darkenEffect.Output;

            maskFloodEffect = new Flood(deviceContext)
            {
                Color = new Vector4(1f, 1f, 1f, 1f),
            };
            maskFloodOutput = maskFloodEffect.Output;

            maskCropEffect = new Crop(deviceContext)
            {
                BorderMode = BorderMode.Soft,
            };
            maskCropEffect.SetInput(0, maskFloodOutput, true);
            maskCropOutput = maskCropEffect.Output;

            maskBlurEffect = new GaussianBlur(deviceContext)
            {
                BorderMode = BorderMode.Soft,
                StandardDeviation = 0f,
            };
            maskBlurEffect.SetInput(0, maskCropOutput, true);
            maskBlurOutput = maskBlurEffect.Output;

            alphaMaskEffect = new AlphaMask(deviceContext);
            alphaMaskEffect.SetInput(1, maskBlurOutput, true);
            maskedSourceOutput = alphaMaskEffect.Output;

            compositeEffect = new Composite(deviceContext)
            {
                Mode = CompositeMode.SourceOver,
            };
            compositeEffect.SetInput(0, darkenOutput, true);
            compositeEffect.SetInput(1, maskedSourceOutput, true);

            Output = compositeEffect.Output;
        }

        public void SetInput(ID2D1Image? input)
        {
            darkenEffect.SetInput(0, input, true);
            alphaMaskEffect.SetInput(0, input, true);
        }

        public void ClearInput()
        {
            darkenEffect.SetInput(0, null, true);
            alphaMaskEffect.SetInput(0, null, true);
        }

        public DrawDescription Update(EffectDescription effectDescription)
        {
            var frame = effectDescription.ItemPosition.Frame;
            var length = effectDescription.ItemDuration.Frame;
            var fps = effectDescription.FPS;

            double centerX = item.CenterX.GetValue(frame, length, fps);
            double centerY = item.CenterY.GetValue(frame, length, fps);
            double spotWidth = item.SpotWidth.GetValue(frame, length, fps);
            double spotHeight = item.SpotHeight.GetValue(frame, length, fps);
            double darkness = item.Darkness.GetValue(frame, length, fps);
            double edgeBlur = item.EdgeBlur.GetValue(frame, length, fps);

            spotWidth = Math.Max(spotWidth, 1.0);
            spotHeight = Math.Max(spotHeight, 1.0);
            darkness = Math.Clamp(darkness, 0.0, 100.0);
            edgeBlur = Math.Clamp(edgeBlur, 0.0, 1000.0);

            UpdateDarkness(darkness);
            UpdateMaskRectangle(centerX, centerY, spotWidth, spotHeight);
            UpdateMaskBlur(edgeBlur);

            isFirst = false;
            return effectDescription.DrawDescription;
        }

        void UpdateDarkness(double darkness)
        {
            if (!isFirst && Math.Abs(prevDarkness - darkness) < 0.0001)
                return;

            float brightness = (float)Math.Clamp(1.0 - darkness / 100.0, 0.0, 1.0);

            darkenEffect.Matrix = new Matrix5x4
            {
                M11 = brightness, M12 = 0,          M13 = 0,          M14 = 0,
                M21 = 0,          M22 = brightness, M23 = 0,          M24 = 0,
                M31 = 0,          M32 = 0,          M33 = brightness, M34 = 0,
                M41 = 0,          M42 = 0,          M43 = 0,          M44 = 1,
                M51 = 0,          M52 = 0,          M53 = 0,          M54 = 0,
            };

            prevDarkness = darkness;
        }

        void UpdateMaskRectangle(double centerX, double centerY, double spotWidth, double spotHeight)
        {
            bool unchanged =
                !isFirst &&
                Math.Abs(prevCenterX - centerX) < 0.0001 &&
                Math.Abs(prevCenterY - centerY) < 0.0001 &&
                Math.Abs(prevSpotWidth - spotWidth) < 0.0001 &&
                Math.Abs(prevSpotHeight - spotHeight) < 0.0001;

            if (unchanged)
                return;

            float halfWidth = Math.Max((float)spotWidth / 2f, 1f);
            float halfHeight = Math.Max((float)spotHeight / 2f, 1f);

            float left = (float)centerX - halfWidth;
            float top = (float)centerY - halfHeight;
            float right = (float)centerX + halfWidth;
            float bottom = (float)centerY + halfHeight;

            // D2D Crop effectのRectangleは(left, top, right, bottom)。
            maskCropEffect.Rectangle = new Vector4(left, top, right, bottom);

            prevCenterX = centerX;
            prevCenterY = centerY;
            prevSpotWidth = spotWidth;
            prevSpotHeight = spotHeight;
        }

        void UpdateMaskBlur(double edgeBlur)
        {
            if (!isFirst && Math.Abs(prevEdgeBlur - edgeBlur) < 0.0001)
                return;

            maskBlurEffect.StandardDeviation = Math.Clamp((float)edgeBlur, 0f, 300f);
            prevEdgeBlur = edgeBlur;
        }

        public void Dispose()
        {
            try { compositeEffect.SetInput(0, null, true); } catch { }
            try { compositeEffect.SetInput(1, null, true); } catch { }
            try { alphaMaskEffect.SetInput(0, null, true); } catch { }
            try { alphaMaskEffect.SetInput(1, null, true); } catch { }
            try { maskBlurEffect.SetInput(0, null, true); } catch { }
            try { maskCropEffect.SetInput(0, null, true); } catch { }
            try { darkenEffect.SetInput(0, null, true); } catch { }

            Output.Dispose();
            compositeEffect.Dispose();

            maskedSourceOutput.Dispose();
            alphaMaskEffect.Dispose();

            maskBlurOutput.Dispose();
            maskBlurEffect.Dispose();

            maskCropOutput.Dispose();
            maskCropEffect.Dispose();

            maskFloodOutput.Dispose();
            maskFloodEffect.Dispose();

            darkenOutput.Dispose();
            darkenEffect.Dispose();
        }
    }
}
