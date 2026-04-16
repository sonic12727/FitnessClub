namespace FitnessClub.Core.Utils
{
    public static class ClubTimeHelper
    {
        public static TimeZoneInfo ClubTimeZone
        {
            get
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
                }
                catch
                {
                    return TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");
                }
            }
        }

        public static DateTime GetLocalNow()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ClubTimeZone);
        }

        public static DateTime GetLocalToday()
        {
            return GetLocalNow().Date;
        }

        public static (DateTime startUtc, DateTime endUtc) GetUtcBoundsForLocalDay(DateTime localDate)
        {
            var startLocal = DateTime.SpecifyKind(localDate.Date, DateTimeKind.Unspecified);
            var endLocal = startLocal.AddDays(1);

            var startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, ClubTimeZone);
            var endUtc = TimeZoneInfo.ConvertTimeToUtc(endLocal, ClubTimeZone);

            return (startUtc, endUtc);
        }

        public static (DateTime startUtc, DateTime endUtc) GetUtcBoundsForLocalRange(DateTime from, DateTime to)
        {
            var startLocal = DateTime.SpecifyKind(from.Date, DateTimeKind.Unspecified);
            var endLocal = DateTime.SpecifyKind(to.Date.AddDays(1), DateTimeKind.Unspecified);

            var startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, ClubTimeZone);
            var endUtc = TimeZoneInfo.ConvertTimeToUtc(endLocal, ClubTimeZone);

            return (startUtc, endUtc);
        }
    }
}
