using OceanEduSlide.DAL;
using OceanEduSlide.Migrations;
using OceanEduSlide.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

public static class PermisstionHelper
{
    public static List<TypeUser> ListTypeUserNhanVien_CN = new List<TypeUser>() { TypeUser.EC, TypeUser.ALT, TypeUser.CM, TypeUser.TTL, TypeUser.SAB, TypeUser.ALT };
    public static List<TypeUser> ListTypeUserQuanLy = new List<TypeUser>() { TypeUser.BM, TypeUser.CV, TypeUser.ASM };
    public static List<TypeUser> ListTypeUserQuanLy_CN = new List<TypeUser>() { TypeUser.BM };
    public static List<TypeUser> ListTypeUserQuanLy_Vung = new List<TypeUser>() { TypeUser.CV, TypeUser.ASM };
    public static List<TypeUser> ListTypeUserEdit_CN = new List<TypeUser>() { TypeUser.CV, TypeUser.ASM };
    public static List<TypeUser> ListTypeUserNhanVien_CN_Vung = new List<TypeUser>() { TypeUser.EC, TypeUser.ALT, TypeUser.CM, TypeUser.TTL, TypeUser.SAB, TypeUser.ALT, TypeUser.AEC };

    #region Zone
    public static IQueryable<Zone> GetZoneManagerMonth(UnitOfWork unitOfWork, User user, IQueryable<HistoryUser> historyUsers, int year, int month)
    {
        if (!user.TypeUser.HasValue)
        {
            return Enumerable.Empty<Zone>().AsQueryable();
        }
        var zones = unitOfWork.ZoneRepository.GetQuery(a => a.Active).AsNoTracking();
        if (user.TypeUser != TypeUser.HO)
        {
            zones = zones.Where(a => (user.ZoneIds.Contains("," + a.ShortCode + ",") && ListTypeUserQuanLy_Vung.Contains(user.TypeUser.Value))
                || historyUsers.Any(hu => hu.ZoneIds != null && hu.ZoneIds.Contains("," + a.ShortCode + ",") && ListTypeUserQuanLy_Vung.Contains(hu.TypeUser)));
        }

        return zones;
    }

    public static IQueryable<Zone> GetZoneManagerPresent(UnitOfWork unitOfWork, User user)
    {
        if (!user.TypeUser.HasValue)
        {
            return Enumerable.Empty<Zone>().AsQueryable();
        }
        var zones = unitOfWork.ZoneRepository.GetQuery(a => a.Active).AsNoTracking();

        if (user.TypeUser != TypeUser.HO)
        {
            var listZoneShortCodeManagerString = user.ZoneIds ?? ",";
            if (user.ZoneId.HasValue)
            {
                if (!listZoneShortCodeManagerString.Contains("," + user.Zone.ShortCode + ","))
                {
                    listZoneShortCodeManagerString += user.Zone.ShortCode + ",";
                }
            }
            zones = zones.Where(a => a.Active && listZoneShortCodeManagerString.Contains("," + a.ShortCode + ",") && ListTypeUserQuanLy_Vung.Contains(user.TypeUser.Value));
        }

        return zones;
    }

    #endregion

    #region Office
    public static IQueryable<Office> GetOfficeManagerMonth(UnitOfWork unitOfWork, User user, IQueryable<HistoryUser> historyUsers, int year, int month, int? zoneId)
    {
        if (!user.TypeUser.HasValue)
        {
            return Enumerable.Empty<Office>().AsQueryable();
        }
        var listOffice = unitOfWork.OfficeRepository.GetQuery(a => a.Active).AsNoTracking();
        var historyOffices = unitOfWork.HistoryOfficeRepository.GetQuery(h => h.Active && h.Year == year && h.Month == month).Select(h => new
        {
            h.OfficeId,
            ZoneShortCode = h.Zone.ShortCode,
            h.ZoneId
        }).AsNoTracking();
        if (user.TypeUser != TypeUser.HO)
        {
            listOffice = listOffice.Where(a => historyOffices.Any(h => h.OfficeId == a.Id
            && (
            (((user.ZoneIds != null && user.ZoneIds.Contains("," + h.ZoneShortCode + ",")) || user.ZoneId == h.ZoneId) && ListTypeUserQuanLy_Vung.Contains(user.TypeUser.Value))
            || historyUsers.Any(hu => hu.ZoneIds != null && hu.ZoneIds.Contains("," + h.ZoneShortCode + ",") && ListTypeUserQuanLy_Vung.Contains(hu.TypeUser))
            || (user.OfficeIds != null && user.OfficeIds.Contains("," + h.OfficeId + ","))
            || user.OfficeId == h.OfficeId
            || historyUsers.Any(hu => hu.OfficeIds != null && hu.OfficeIds.Contains("," + h.OfficeId + ",") && ListTypeUserQuanLy_CN.Contains(hu.TypeUser))
            )
            ));
        }

        if (zoneId.HasValue)
        {
            listOffice = listOffice.Where(l => historyOffices.Any(h => h.OfficeId == l.Id && h.ZoneId == zoneId));
        }

        return listOffice;
    }

    public static IQueryable<Office> GetOfficeManagerPresent(UnitOfWork unitOfWork, User user)
    {
        if (!user.TypeUser.HasValue)
        {
            return Enumerable.Empty<Office>().AsQueryable();
        }
        var listOffice = unitOfWork.OfficeRepository.GetQuery(a => a.Active).AsNoTracking();

        if (user.TypeUser != TypeUser.HO)
        {
            var listZoneShortCodeManagerString = user.ZoneIds ?? ",";
            if (user.ZoneId.HasValue)
            {
                if (!listZoneShortCodeManagerString.Contains("," + user.Zone.ShortCode + ","))
                {
                    listZoneShortCodeManagerString += user.Zone.ShortCode + ",";
                }
            }

            var listOfficeIdManagerString = user.OfficeIds ?? ",";
            if (user.OfficeId.HasValue)
            {
                if (!listOfficeIdManagerString.Contains("," + user.OfficeId + ","))
                {
                    listOfficeIdManagerString += user.OfficeId + ",";
                }
            }

            listOffice = listOffice.Where(a => a.Active
            && (
            (listZoneShortCodeManagerString.Contains("," + a.Zone.ShortCode + ",") && ListTypeUserQuanLy_Vung.Contains(user.TypeUser.Value))
            || listOfficeIdManagerString.Contains("," + a.Id.ToString() + ",")
            ));
        }

        return listOffice;
    }
    #endregion

    #region User

    public static IQueryable<User> GetUserManagerPresent(UnitOfWork unitOfWork, User user)
    {
        if (!user.TypeUser.HasValue)
        {
            return Enumerable.Empty<User>().AsQueryable();
        }
        var listUser = unitOfWork.UserRepository.GetQuery(a => a.Active && a.TypeUser.HasValue && ListTypeUserNhanVien_CN_Vung.Contains(a.TypeUser.Value)).AsNoTracking();

        if (user.TypeUser != TypeUser.HO)
        {
            var listZoneShortCodeManagerString = user.ZoneIds ?? "";
            if (user.ZoneId.HasValue)
            {
                if (!listZoneShortCodeManagerString.Contains("," + user.Zone.ShortCode + ","))
                {
                    listZoneShortCodeManagerString += user.Zone.ShortCode + ",";
                }
            }

            var listOfficeIdManagerString = user.OfficeIds ?? "";
            if (user.OfficeId.HasValue)
            {
                if (!listOfficeIdManagerString.Contains("," + user.OfficeId + ","))
                {
                    listOfficeIdManagerString += user.OfficeId + ",";
                }
            }

            var listZoneCode = listZoneShortCodeManagerString.Trim(',').Split(',');
            var listOfficeId = listOfficeIdManagerString.Trim(',').Split(',');

            var listZoneIds = unitOfWork.ZoneRepository.GetQuery(a => a.Active && listZoneShortCodeManagerString.Contains(a.ShortCode)).Select(a => a.Id).ToHashSet();

            var listOfficeIds = unitOfWork.OfficeRepository.GetQuery(a => a.Active && listZoneShortCodeManagerString.Contains(a.Zone.ShortCode)).Select(a => a.Id).ToHashSet();
            if (listOfficeId.Any())
            {
                foreach (var item in listOfficeId)
                {
                    if (int.TryParse(item, out var officeId))
                    {
                        listOfficeIds.Add(officeId);
                    }
                }
            }
            listUser = listUser.Where(a => ListTypeUserNhanVien_CN_Vung.Contains(a.TypeUser.Value)
           && ((a.OfficeId.HasValue && listOfficeIds.Contains(a.OfficeId.Value)) || (a.ZoneId.HasValue && listZoneIds.Contains(a.ZoneId.Value))));

        }

        return listUser;
    }

    public static IQueryable<User> GetUserManagerMonth(UnitOfWork unitOfWork, User user, IQueryable<HistoryUser> historyUsers, int year, int month, int? zoneId, int? officeId)
    {
        if (!user.TypeUser.HasValue)
        {
            return Enumerable.Empty<User>().AsQueryable();
        }
        var listUser = unitOfWork.UserRepository.GetQuery(a => a.TypeUser.HasValue && a.TypeUser != TypeUser.User).AsNoTracking();
        var listHistoryUser = unitOfWork.HistoryUserRepository.GetQuery(h => h.Active && h.Year == year && h.Month == month && ListTypeUserNhanVien_CN_Vung.Contains(h.TypeUser)).Select(h => new
        {
            h.UserId,
            h.OfficeId,
            h.OfficeIds,
            h.ZoneIds,
            h.Office,
            h.ZoneId,
            h.Zone
        }).AsNoTracking();

        if (user.TypeUser != TypeUser.HO)
        {
            //listUser = listUser.Where(a => listHistoryUser.Any(h => h.UserId == a.Id
            //&& (
            //((((user.ZoneIds != null && user.ZoneIds.Contains("," + h.Zone.ShortCode + ",")) || user.ZoneId == h.ZoneId) && h.ZoneId != null) && ListTypeUserQuanLy_Vung.Contains(user.TypeUser.Value))
            //|| ((((user.ZoneIds != null && user.ZoneIds.Contains("," + h.Office.Zone.ShortCode + ",")) || user.ZoneId == h.Office.ZoneId) && h.OfficeId != null) && ListTypeUserQuanLy_Vung.Contains(user.TypeUser.Value))
            //|| historyUsers.Any(hu => hu.ZoneIds != null && h.ZoneId != null && hu.ZoneIds.Contains("," + h.Zone.ShortCode + ",") && ListTypeUserQuanLy_Vung.Contains(hu.TypeUser))
            //|| (user.OfficeIds != null && h.OfficeId != null && user.OfficeIds.Contains("," + h.OfficeId + ","))
            //|| (user.OfficeId == h.OfficeId &&  h.OfficeId != null)
            //|| historyUsers.Any(hu => hu.OfficeIds != null && h.OfficeId != null && hu.OfficeIds.Contains("," + h.OfficeId + ",") && ListTypeUserQuanLy_CN.Contains(hu.TypeUser))
            //)
            //));
            listUser = listUser.Where(a => listHistoryUser.Any(h =>
                h.UserId == a.Id &&
                (

                    // ===== USER QUẢN LÝ VÙNG =====
                    (
                        ListTypeUserQuanLy_Vung.Contains(user.TypeUser.Value) &&
                        (
                            (h.ZoneId != null &&
                                (
                                    (user.ZoneIds != null && user.ZoneIds.Contains("," + h.Zone.ShortCode + ",")) ||
                                    user.ZoneId == h.ZoneId
                                )
                            )
                            ||
                            (h.OfficeId != null &&
                                (
                                    (user.ZoneIds != null && user.ZoneIds.Contains("," + h.Office.Zone.ShortCode + ",")) ||
                                    user.ZoneId == h.Office.ZoneId
                                )
                            )
                        )
                    )

                    ||

                    // ===== HISTORY USER QUẢN LÝ VÙNG =====
                    historyUsers.Any(hu =>
                        ListTypeUserQuanLy_Vung.Contains(hu.TypeUser) &&
                        hu.ZoneIds != null &&
                        (
                            (h.ZoneId != null && hu.ZoneIds.Contains("," + h.Zone.ShortCode + ",")) ||
                            (h.OfficeId != null && hu.ZoneIds.Contains("," + h.Office.Zone.ShortCode + ","))
                        )
                    )

                    ||

                    // ===== USER QUẢN LÝ CHI NHÁNH =====
                    (
                        h.OfficeId != null &&
                        (
                            (user.OfficeIds != null && user.OfficeIds.Contains("," + h.OfficeId + ",")) ||
                            (user.OfficeId != null && user.OfficeId == h.OfficeId)
                        )
                    )

                    ||

                    // ===== HISTORY USER QUẢN LÝ CHI NHÁNH =====
                    historyUsers.Any(hu =>
                        ListTypeUserQuanLy_CN.Contains(hu.TypeUser) &&
                        hu.OfficeIds != null &&
                        h.OfficeId != null &&
                        hu.OfficeIds.Contains("," + h.OfficeId + ",")
                    )
                )
            ));
        }
        if (zoneId.HasValue)
        {
            listUser = listUser.Where(l => listHistoryUser.Any(h => h.UserId == l.Id && (h.ZoneId == zoneId || (h.OfficeId != null && h.Office.ZoneId == zoneId))));
        }
        if (officeId.HasValue)
        {
            listUser = listUser.Where(l => listHistoryUser.Any(h => h.UserId == l.Id && h.OfficeId == officeId));
        }

        return listUser;
    }

    public static IQueryable<HistoryUser> GetHistoryUserManagerMonth(UnitOfWork unitOfWork, User user, IQueryable<HistoryUser> historyUsers, int year, int month, int? zoneId, int? officeId, int? userId, int? userType)
    {
        if (!user.TypeUser.HasValue)
        {
            return Enumerable.Empty<HistoryUser>().AsQueryable();
        }
        var listHistoryUser = unitOfWork.HistoryUserRepository.GetQuery(a => a.Active && a.Month == month && a.Year == year && ListTypeUserNhanVien_CN_Vung.Contains(a.TypeUser)
                    && (a.DayEnd == null || (a.DayEnd != null && ((a.DayEnd.Value.Day != 1 && a.DayEnd.Value.Month == month) || a.DayEnd.Value.Month != month))));
        if (user.TypeUser != TypeUser.HO)
        {
            //listHistoryUser = listHistoryUser.Where(h =>
            //(((user.ZoneIds != null && user.ZoneIds.Contains("," + h.Zone.ShortCode + ",")) || (user.ZoneId == h.ZoneId ))&& h.ZoneId != null && ListTypeUserQuanLy_Vung.Contains(user.TypeUser.Value))
            //|| (((user.ZoneIds != null && user.ZoneIds.Contains("," + h.Office.Zone.ShortCode + ",")) || (user.ZoneId == h.Office.ZoneId)) && h.OfficeId != null && h.Office.ZoneId != null && ListTypeUserQuanLy_Vung.Contains(user.TypeUser.Value))
            //|| historyUsers.Any(hu => hu.ZoneIds != null && (h.ZoneId != null && hu.ZoneIds.Contains("," + h.Zone.ShortCode + ",") || h.OfficeId != null && hu.ZoneIds.Contains("," + h.Office.Zone.ShortCode + ",")) && ListTypeUserQuanLy_Vung.Contains(hu.TypeUser) )
            //|| (user.OfficeIds != null && h.OfficeId != null && user.OfficeIds.Contains("," + h.OfficeId + ","))
            //|| (user.OfficeId == h.OfficeId && user.OfficeId != null)
            //|| historyUsers.Any(hu => hu.OfficeIds != null && h.OfficeId != null && hu.OfficeIds.Contains("," + h.OfficeId + ",") && ListTypeUserQuanLy_CN.Contains(hu.TypeUser))
            //);
            listHistoryUser = listHistoryUser.Where(h =>

                // ===== QUẢN LÝ VÙNG =====
                (
                    ListTypeUserQuanLy_Vung.Contains(user.TypeUser.Value) &&
                    (
                        (h.ZoneId != null &&
                            (
                                (user.ZoneIds != null && user.ZoneIds.Contains("," + h.Zone.ShortCode + ",")) ||
                                user.ZoneId == h.ZoneId
                            )
                        )
                        ||
                        (h.OfficeId != null && h.Office.ZoneId != null &&
                            (
                                (user.ZoneIds != null && user.ZoneIds.Contains("," + h.Office.Zone.ShortCode + ",")) ||
                                user.ZoneId == h.Office.ZoneId
                            )
                        )
                    )
                )

                ||

                // ===== HISTORY USER QUẢN LÝ VÙNG =====
                historyUsers.Any(hu =>
                    ListTypeUserQuanLy_Vung.Contains(hu.TypeUser) &&
                    hu.ZoneIds != null &&
                    (
                        (h.ZoneId != null && hu.ZoneIds.Contains("," + h.Zone.ShortCode + ",")) ||
                        (h.OfficeId != null && hu.ZoneIds.Contains("," + h.Office.Zone.ShortCode + ","))
                    )
                )

                ||

                // ===== QUẢN LÝ CHI NHÁNH (USER) =====
                (
                    h.OfficeId != null &&
                    (
                        (user.OfficeIds != null && user.OfficeIds.Contains("," + h.OfficeId + ",")) ||
                        (user.OfficeId != null && user.OfficeId == h.OfficeId)
                    )
                )

                ||

                // ===== HISTORY USER QUẢN LÝ CHI NHÁNH =====
                historyUsers.Any(hu =>
                    ListTypeUserQuanLy_CN.Contains(hu.TypeUser) &&
                    hu.OfficeIds != null &&
                    h.OfficeId != null &&
                    hu.OfficeIds.Contains("," + h.OfficeId + ",")
                )
            );
        }

        if (zoneId.HasValue)
        {
            listHistoryUser = listHistoryUser.Where(h => h.ZoneId == zoneId || (h.OfficeId != null && h.Office.ZoneId == zoneId));
        }
        if (officeId.HasValue)
        {
            listHistoryUser = listHistoryUser.Where(h => h.OfficeId == officeId);
        }
        if (userId.HasValue)
        {
            listHistoryUser = listHistoryUser.Where(h => h.UserId == userId);
        }
        if (userType.HasValue)
        {
            listHistoryUser = listHistoryUser.Where(h => (int)h.TypeUser == userType);
        }
        return listHistoryUser;
    }

    #endregion
}