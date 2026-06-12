using System;
using KaraWeb.Shared.Helpers;
using NUnit.Framework;

namespace KaraWeb.Tests.Helpers
{
    [TestFixture]
    public sealed class TimesHelperTest
    {
        [TestCase(120, 8, 1200, 5200)]
        [TestCase(120, 120, null, 1000 * 60)]
        [TestCase(120, 120, -1000 * 60, 0)]
        [TestCase(0, 120, null, null)]
        [TestCase(950 * 4, 0, 7122, 7122)]
        [TestCase(950 * 4, 103, 7122, 8748)]
        public void GetTimeFromBeatTest(decimal bpm, int beat, int? gapMs, int? expectedTimeMs)
        {
            TimeSpan? gap = gapMs.HasValue ? TimeSpan.FromMilliseconds(gapMs.Value) : null;
            TimeSpan? expectedTime = expectedTimeMs.HasValue ? TimeSpan.FromMilliseconds(expectedTimeMs.Value) : null;
            Assert.That(TimesHelper.GetTimeFromBeat(bpm, beat, gap), Is.EqualTo(expectedTime).Within(TimeSpan.FromMilliseconds(1)),
                "The retrieved time isn't as expected");
        }

        [TestCase(120, 5200, 1200, 8)]
        [TestCase(120, 1000 * 60, null, 120)]
        [TestCase(120, 1000 * 60 * 2, -1000 * 60, 0)]
        [TestCase(0, 120, null, null)]
        [TestCase(950 * 4, 8749, 7122, 103)]
        [TestCase(950 * 4, 7122, 7122, 0)]
        [TestCase(950 * 4, 1122, 7122, null)]
        public void GetBeatFromTimeTest(decimal bpm, int timeMs, int? gapMs, int? expectedBeat)
        {
            var time = TimeSpan.FromMilliseconds(timeMs);
            TimeSpan? gap = gapMs.HasValue ? TimeSpan.FromMilliseconds(gapMs.Value) : null;
            Assert.That(TimesHelper.GetBeatFromTime(bpm, time, gap), Is.EqualTo(expectedBeat),
                "The retrieved beat isn't as expected");
        }
    }
}