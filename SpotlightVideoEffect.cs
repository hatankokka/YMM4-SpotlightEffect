using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Exo;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin.Effects;

namespace SpotlightEffect
{
    public enum SpotShape
    {
        [Display(Name = "四角形")]
        Rectangle = 0,
    }

    [VideoEffect("スポットライト", ["合成"], [], IsAviUtlSupported = false)]
    internal class SpotlightVideoEffect : VideoEffectBase
    {
        public override string Label => "スポットライト";

        [Display(Name = "形状")]
        [EnumComboBox]
        public SpotShape Shape { get; set; } = SpotShape.Rectangle;

        [Display(Name = "中心X")]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation CenterX { get; } = new Animation(50, 0, 100);

        [Display(Name = "中心Y")]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation CenterY { get; } = new Animation(50, 0, 100);

        [Display(Name = "幅")]
        [AnimationSlider("F1", "%", 1, 200)]
        public Animation SpotWidth { get; } = new Animation(40, 1, 200);

        [Display(Name = "高さ")]
        [AnimationSlider("F1", "%", 1, 200)]
        public Animation SpotHeight { get; } = new Animation(30, 1, 200);

        [Display(Name = "暗くする強度")]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation Darkness { get; } = new Animation(70, 0, 100);

        [Display(Name = "境界ぼかし")]
        [AnimationSlider("F1", "%", 0, 60)]
        public Animation EdgeBlur { get; } = new Animation(15, 0, 60);

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
