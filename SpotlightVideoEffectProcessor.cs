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
    /// スポットライトエフェクト Processor（矩形専用・安定版）
    ///
    /// 方針:
    /// - Polygon / Freehand は扱わない。
    /// - 自前Bitmap / CommandList / PushLayer / PathGeometry は使わない。
    /// - Direct2D組み込みEffectだけで構成する。
    /// - 画像本体はぼかさず、矩形マスクだけをGaussianBlurする。
    ///
    /// チェーン:
    ///   元映像 ──┬→ ColorMatrix(暗化) ─────────────────────────────┐
    ///            │                                                 ├→ Composite(SourceOver) → Output
    ///            └→ AlphaMask(元映像, Flood→Crop→GaussianBlur) ───┘
    ///
    /// これにより、スポット内の文字・地図はぼけず、境界だけがぼける。
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
        float prevCanvasWidth = -1f;
        float prevCanvasHeight = -1f;

        public ID2D1Image Output { get; }

        public SpotlightVideoEffectProcessor(IGraphicsDevicesAndContext devices, SpotlightVideoEffect item)
        {
            this.item = item;
            deviceContext = devices.DeviceContext;

            // 背面: 全体を暗くした映像。
            darkenEffect = new ColorMatrix(deviceContext);
            darkenOutput = darkenEffect.Output;

            // マスク元: 白い全面画像。
            maskFloodEffect = new Flood(deviceContext)
            {
                Color = new Vector4(1f, 1f, 1f, 1f),
            };
            maskFloodOutput = maskFloodEffect.Output;

            // 白い全面画像から、スポット矩形だけを切り出す。
            maskCropEffect = new Crop(deviceContext)
            {
                BorderMode = BorderMode.Soft,
            };
            maskCropEffect.SetInput(0, maskFloodOutput, true);
            maskCropOutput = maskCropEffect.Output;

            // 画像本体ではなく、白マスクだけをぼかす。
            maskBlurEffect = new GaussianBlur(deviceContext)
            {
                BorderMode = BorderMode.Soft,
                StandardDeviation = 0f,
            };
            maskBlurEffect.SetInput(0, maskCropOutput, true);
            maskBlurOutput = maskBlurEffect.Output;

            // 元映像を、ぼかし済みマスクで抜く。
            alphaMaskEffect = new AlphaMask(deviceContext);
            alphaMaskEffect.SetInput(1, maskBlurOutput, true);
            maskedSourceOutput = alphaMaskEffect.Output;

            // 背面の暗化映像の上に、マスクで抜いた元映像を重ねる。
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

            centerX = Math.Clamp(centerX, 0.0, 100.0);
            centerY = Math.Clamp(centerY, 0.0, 100.0);
            spotWidth = Math.Clamp(spotWidth, 1.0, 200.0);
            spotHeight = Math.Clamp(spotHeight, 1.0, 200.0);
            darkness = Math.Clamp(darkness, 0.0, 100.0);
            edgeBlur = Math.Clamp(edgeBlur, 0.0, 60.0);

            GetCanvasSize(out float canvasWidth, out float canvasHeight);

            UpdateDarkness(darkness);
            UpdateMaskRectangle(centerX, centerY, spotWidth, spotHeight, canvasWidth, canvasHeight);
            UpdateMaskBlur(edgeBlur, canvasWidth, canvasHeight);

            isFirst = false;
            return effectDescription.DrawDescription;
        }

        void GetCanvasSize(out float canvasWidth, out float canvasHeight)
        {
            var size = deviceContext.Size;
            canvasWidth = size.Width;
            canvasHeight = size.Height;

            // YMM4ではUpdate時点でDeviceContext.Sizeが未確定/極小になることがある。
            // その場合は過去に安定した1920x1080を使う。
            if (canvasWidth <= 1f || canvasHeight <= 1f)
            {
                canvasWidth = 1920f;
                canvasHeight = 1080f;
            }
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

        void UpdateMaskRectangle(double centerX, double centerY, double spotWidth, double spotHeight, float canvasWidth, float canvasHeight)
        {
            bool unchanged =
                !isFirst &&
                Math.Abs(prevCenterX - centerX) < 0.0001 &&
                Math.Abs(prevCenterY - centerY) < 0.0001 &&
                Math.Abs(prevSpotWidth - spotWidth) < 0.0001 &&
                Math.Abs(prevSpotHeight - spotHeight) < 0.0001 &&
                Math.Abs(prevCanvasWidth - canvasWidth) < 0.01f &&
                Math.Abs(prevCanvasHeight - canvasHeight) < 0.01f;

            if (unchanged)
                return;

            float cx = canvasWidth * (float)(centerX / 100.0);
            float cy = canvasHeight * (float)(centerY / 100.0);
            float halfWidth = Math.Max(canvasWidth * (float)(spotWidth / 200.0), 1f);
            float halfHeight = Math.Max(canvasHeight * (float)(spotHeight / 200.0), 1f);

            float left = cx - halfWidth;
            float top = cy - halfHeight;
            float width = halfWidth * 2f;
            float height = halfHeight * 2f;

            // Crop.Rectangle は Vector4(left, top, width, height)。
            maskCropEffect.Rectangle = new Vector4(left, top, width, height);

            prevCenterX = centerX;
            prevCenterY = centerY;
            prevSpotWidth = spotWidth;
            prevSpotHeight = spotHeight;
            prevCanvasWidth = canvasWidth;
            prevCanvasHeight = canvasHeight;
        }

        void UpdateMaskBlur(double edgeBlur, float canvasWidth, float canvasHeight)
        {
            if (!isFirst && Math.Abs(prevEdgeBlur - edgeBlur) < 0.0001)
                return;

            // UI値は0-60%。そのままpx扱いにすると弱すぎる/強すぎる環境が出るため、
            // 画面短辺に対する比率へ変換する。
            // 例: 1080pで EdgeBlur=10 → 約27px。
            float sigma = Math.Min(canvasWidth, canvasHeight) * (float)(edgeBlur / 100.0) * 0.25f;
            sigma = Math.Clamp(sigma, 0f, 120f);

            maskBlurEffect.StandardDeviation = sigma;
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
