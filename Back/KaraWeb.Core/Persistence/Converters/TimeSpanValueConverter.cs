using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace KaraWeb.Core.Persistence.Converters
{
    internal sealed class TimeSpanValueConverter : ValueConverter<TimeSpan?, double>
    {
        public TimeSpanValueConverter() : base(
            t => t.HasValue ? t.Value.TotalMilliseconds : double.NaN,
            d => double.IsNaN(d) ? null : TimeSpan.FromMilliseconds(d))
        {
        }
    }
}