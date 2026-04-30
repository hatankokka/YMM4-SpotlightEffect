using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Exo;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin.Effects;

namespace SpotlightEffect
{
    [VideoEffect("スポットライト", ["合成"], [], IsAviUtlSupported = false)]
    internal sealed class SpotlightVideoEffect : VideoEffectBase
    {
        public override string Label => "スポットライト";

        [Display(Name = "中心X")]
        [AnimationSlider("F0", "px", -7680, 7680)]
        public Animation CenterX { get; } = new Animation(960, -7680, 7680);

        [Display(Name = "中心Y")]
        [AnimationSlider("F0", "px", -4320, 4320)]
        public Animation CenterY { get; } = new Animation(540, -4320, 4320);

        [Display(Name = "幅")]
        [AnimationSlider("F0", "px", 1, 7680)]
        public Animation SpotWidth { get; } = new Animation(640, 1, 7680);

        [Display(Name = "高さ")]
        [AnimationSlider("F0", "px", 1, 4320)]
        public Animation SpotHeight { get; } = new Animation(360, 1, 4320);

        [Display(Name = "暗くする強度")]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation Darkness { get; } = new Animation(70, 0, 100);

        [Display(Name = "境界ぼかし")]
        [AnimationSlider("F0", "px", 0, 1000)]
        public Animation EdgeBlur { get; } = new Animation(20, 0, 1000);

        public override IEnumerable<string> CreateExoVideoFilters(int keyFrameIndex, ExoOutputDescription exoOutputDescription)
        {
            return [];
        }

        public override IVideoEffectProcessor CreateVideoEffect(IGraphicsDevicesAndContext devices)
        {
            return new SpotlightVideoEffectProcessor(devices, this);
        }

        protected override IEnumerable<IAnimatable> GetAnimatables() =>
            [CenterX, CenterY, SpotWidth, SpotHeight, Darkness, EdgeBlur];
    }
}
