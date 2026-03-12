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
    public static List<TypeUser> ListTypeUserNhanVien_CN_Vung = new List<TypeUser>() { TypeUser.EC, TypeUser.ALT, TypeUser.CM, TypeUser.TTL, TypeUser.SAB, TypeUser.ALT, TypeUser.AEC };

    public static List<User> GetUserManagerPresent(UnitOfWork unitOfWork, User user)
    {
        if (!user.TypeUser.HasValue)
        {
            return new List<User>();
        }
        var listUser = unitOfWork.UserRepository.GetQuery(a => a.Active).AsNoTracking();

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

        return listUser.ToList();
    }
    #region Zone
    public static List<Zone> GetZoneManagerMonth(UnitOfWork unitOfWork, User user, IQueryable<HistoryUser> historyUsers, int year, int month)
    {
        if (!user.TypeUser.HasValue)
        {
            return new List<Zone>();
        }
        var zones = unitOfWork.ZoneRepository.GetQuery(a => a.Active).AsNoTracking();
        if (user.TypeUser != TypeUser.HO)
        {
            zones = zones.Where(a => (user.ZoneIds.Contains("," + a.ShortCode + ",") && ListTypeUserQuanLy_Vung.Contains(user.TypeUser.Value))
                || historyUsers.Any(hu => hu.ZoneIds != null && hu.ZoneIds.Contains("," + a.ShortCode + ",") && ListTypeUserQuanLy_Vung.Contains(hu.TypeUser)));
        }

        return zones.ToList();
    }

    #endregion
    #region Office
    public static List<Office> GetOfficeManagerMonth(UnitOfWork unitOfWork, User user, IQueryable<HistoryUser> historyUsers, int year, int month, int? zoneId)
    {
        if (!user.TypeUser.HasValue)
        {
            return new List<Office>();
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

        return listOffice.ToList();
    }
    public static List<User> GetUserManagerMonth(UnitOfWork unitOfWork, User user, IQueryable<HistoryUser> historyUsers, int year, int month, int? zoneId, int? officeId)
    {
        if (!user.TypeUser.HasValue)
        {
            return new List<User>();
        }
        var listUser = unitOfWork.UserRepository.GetQuery(a => a.TypeUser.HasValue && ListTypeUserNhanVien_CN_Vung.Contains(a.TypeUser.Value)).AsNoTracking();
        var listHistoryUser = unitOfWork.HistoryUserRepository.GetQuery(h => h.Active && h.Year == year && h.Month == month).Select(h => new
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
            listUser = listUser.Where(a => listHistoryUser.Any(h => h.UserId == a.Id
            && (
            (((user.ZoneIds != null && h.ZoneId != null && user.ZoneIds.Contains("," + h.Zone.ShortCode + ",")) || user.ZoneId == h.ZoneId) && ListTypeUserQuanLy_Vung.Contains(user.TypeUser.Value))
            || historyUsers.Any(hu => hu.ZoneIds != null && h.ZoneId != null && hu.ZoneIds.Contains("," + h.Zone.ShortCode + ",") && ListTypeUserQuanLy_Vung.Contains(hu.TypeUser))
            || (user.OfficeIds != null && h.OfficeId != null && user.OfficeIds.Contains("," + h.OfficeId + ","))
            || user.OfficeId == h.OfficeId
            || historyUsers.Any(hu => hu.OfficeIds != null && h.OfficeId != null && hu.OfficeIds.Contains("," + h.OfficeId + ",") && ListTypeUserQuanLy_CN.Contains(hu.TypeUser))
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

        return listUser.ToList();
    }


    public static List<Office> GetOfficeManagerPresent(UnitOfWork unitOfWork, User user)
    {
        if (!user.TypeUser.HasValue)
        {
            return new List<Office>();
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

            listOffice = listOffice.Where(a => a.Active && (listZoneShortCodeManagerString.Contains("," + a.Zone.ShortCode + ",") || listOfficeIdManagerString.Contains("," + a.Id.ToString() + ",")));
        }

        return listOffice.ToList();
    }
    #endregion

}