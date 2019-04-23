using System;

namespace Core
{
    static class TimeSpanExtensions
	{
		public static TimeSpan Days(this int days)
		{
			return new TimeSpan(days, 0, 0, 0);
		}

		public static TimeSpan Hours(this int hours)
		{
			return new TimeSpan(0, hours, 0, 0);
		}

		public static TimeSpan Minutes(this int minutes)
		{
			return new TimeSpan(0, 0, minutes, 0);
		}

		public static TimeSpan Seconds(this int seconds)
		{
			return new TimeSpan(0, 0, 0, seconds);
		}

		public static TimeSpan Milliseconds(this int milliseconds)
		{
			return new TimeSpan(0, 0, 0, 0, milliseconds);
		}

		public static TimeSpan Milliseconds(this double milliseconds)
		{
			if(milliseconds >= TimeSpan.MaxValue.TotalMilliseconds)
				return new TimeSpan(TimeSpan.MaxValue.Ticks);
			return TimeSpan.FromMilliseconds(milliseconds);
		}

		public static TimeSpan Add(this TimeSpan ts1, TimeSpan ts2)
		{
			bool sign1 = ts1 < TimeSpan.Zero, sign2 = ts2 < TimeSpan.Zero;

			if(sign1 && sign2)
			{
				if(TimeSpan.MinValue - ts1 > ts2)
					return TimeSpan.MinValue;
			}
			else if(!sign1 && !sign2)
			{
				if(TimeSpan.MaxValue - ts1 < ts2)
					return TimeSpan.MaxValue;
			}

			return (ts1 + ts2);
		}

		public static bool IsBetween(this TimeSpan time, TimeSpan startTime, TimeSpan endTime)
		{
			if(startTime == endTime)
				return (true);
			if(endTime < startTime)
				return (time <= endTime || time >= startTime);
			return (time >= startTime && time <= endTime);
		}


		/// <summary>
		/// Convert something like 100s to 1:40.
		/// </summary>
		public static string Format(this TimeSpan time, bool milliseconds = true)
		{   // int.MaxValue.Milliseconds().ToString(@"dd\.hh\:mm\:ss\.fff")
			string value;

			if(time.TotalSeconds < (60 * 60))
				value = string.Format("{0:D2}:{1:D2}",
					time.Minutes, time.Seconds);
			else if(time.TotalSeconds < (60 * 60 * 24))
				value = string.Format("{0:D2}:{1:D2}:{2:D2}",
					time.Hours, time.Minutes, time.Seconds);
			else
				value = string.Format("{0:D}.{1:D2}:{2:D2}:{3:D2}",
					time.Days, time.Hours, time.Minutes, time.Seconds);

			if(milliseconds)
				value += string.Format(".{0:D3}", time.Milliseconds);

			return (value);
		}

	}
}
