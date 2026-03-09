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
    public static List<TypeUser> ListTypeUserQuanLy = new List<TypeUser>() { TypeUser.BM, TypeUser.CV, TypeUser.ASM, TypeUser.AEC };
    public static List<TypeUser> ListTypeUserQuanLy_Vung = new List<TypeUser>() { TypeUser.BM, TypeUser.CV, TypeUser.ASM, TypeUser.AEC };
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
            if (ListTypeUserQuanLy.Contains(user.TypeUser.Value))
                zones = zones.Where(a => user.ZoneIds.Contains("," + a.ShortCode + ",") || historyUsers.Any(hu => hu.ZoneIds != null && hu.ZoneIds.Contains("," + a.ShortCode + ",")));
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
            && ((user.ZoneIds != null && user.ZoneIds.Contains("," + h.ZoneShortCode + ","))
            || (user.ZoneId == h.ZoneId)
            || historyUsers.Any(hu => hu.ZoneIds != null && hu.ZoneIds.Contains("," + h.ZoneShortCode + ","))
            || (user.OfficeIds != null && user.OfficeIds.Contains("," + h.OfficeId + ","))
            || (user.OfficeId == h.OfficeId)
            || historyUsers.Any(hu => hu.OfficeIds != null && hu.OfficeIds.Contains("," + h.OfficeId + ",")))
            ));
        }
        if (zoneId.HasValue)
        {
            listOffice = listOffice.Where(l => historyOffices.Any(h => h.OfficeId == l.Id && h.ZoneId == zoneId));
        }

        return listOffice.ToList();
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