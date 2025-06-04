using System;

public static class DateHelper
{
    public static (int, int) CalculateWeeks(int year, int month)
    {
        DateTime firstDay = new DateTime(year, month, 1);
        DateTime lastDay = firstDay.AddMonths(1).AddDays(-1);
        DateTime today = DateTime.Now;

        int workingWeeks = 1;
        int currentWeek = 0;

        DateTime currentDay = firstDay;

        while (currentDay <= lastDay)
        {
            if (currentDay.DayOfWeek == DayOfWeek.Monday && currentDay != firstDay)
            {
                workingWeeks++;
            }

            if (currentDay.Year == today.Year && currentDay.Month == today.Month && currentDay.Day == today.Day)
            {
                currentWeek = workingWeeks;
            }

            currentDay = currentDay.AddDays(1);
        }

        if (today.Month != month || today.Year != year)
        {
            currentWeek = 0;
        }

        return (workingWeeks, currentWeek);
    }
    public static int GetWorkingDaysInWeek(int currentWeek, int year, int month)
    {
        DateTime firstDay = new DateTime(year, month, 1);
        DateTime lastDay = firstDay.AddMonths(1).AddDays(-1);

        DateTime weekStart;
        if (currentWeek == 1)
        {
            // Tuần 1 bắt đầu từ ngày đầu tiên của tháng
            weekStart = firstDay;

            // Xác định ngày Chủ Nhật đầu tiên
            DateTime firstSunday = firstDay;
            while (firstSunday.DayOfWeek != DayOfWeek.Sunday)
            {
                firstSunday = firstSunday.AddDays(1);
            }

            // Tuần 1 kết thúc vào Chủ Nhật đầu tiên
            DateTime weekEnd = firstSunday > lastDay ? lastDay : firstSunday;

            return (weekEnd - weekStart).Days + 1; // Số ngày làm việc trong tuần 1
        }
        else
        {
            // Tìm thứ Hai đầu tiên của tuần đó
            weekStart = firstDay;
            int weekCount = 1;
            while (weekStart.DayOfWeek != DayOfWeek.Monday || weekCount < currentWeek)
            {
                weekStart = weekStart.AddDays(1);
                if (weekStart.DayOfWeek == DayOfWeek.Monday)
                {
                    weekCount++;
                }
            }

            // Xác định ngày kết thúc của tuần
            DateTime weekEnd = weekStart.AddDays(6);
            if (weekEnd > lastDay)
            {
                weekEnd = lastDay;
            }

            return (weekEnd - weekStart).Days + 1; // Số ngày làm việc trong tuần
        }
    }
}