using System;
using KaraWeb.Core.Helper;
using NUnit.Framework;

namespace KaraWeb.Tests.Helpers
{
    [TestFixture]
    public sealed class TimesHelperTest
    {
        [TestCase(120, 8, 1200, 5200)]
        [TestCase(120, 120, null, 1000 * 60)]
        [TestCase(0, 120, null, null)]
        public void GetBeatFromTime(decimal bpm, int beat, int? gapMs, int? expectedTimeMs)
        {
            TimeSpan? gap = gapMs.HasValue ? TimeSpan.FromMilliseconds(gapMs.Value) : null;
            TimeSpan? expectedTime = expectedTimeMs.HasValue ? TimeSpan.FromMilliseconds(expectedTimeMs.Value) : null;
            Assert.That(TimesHelper.GetTimeFromBeat(bpm, beat, gap), Is.EqualTo(expectedTime), "The retrieved time isn't as expected");
        }
    }
}