using Newtonsoft.Json;
using OceanEduSlide.Models;
using OceanEduSlide.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using NLog;
using System.Data.Entity;
using FluentScheduler;
using Newtonsoft.Json.Linq;
using Microsoft.Ajax.Utilities;

namespace OceanEduSlide.DAL
{
    public class UserTypeService : IUserTypeService
    {
        private readonly Dictionary<string, TypeUser> _typeUserMap;
        public readonly UnitOfWork _unitOfWork = new UnitOfWork();

        public UserTypeService()
        {
            // Map mặc định
            _typeUserMap = new Dictionary<string, TypeUser>()
            {
            { "ASM", TypeUser.ASM },
            //{ "DCEO", TypeUser.HO },
            { "NDDM", TypeUser.ASM },
            { "GĐTS", TypeUser.HO },
            { "EC", TypeUser.EC },
            { "TELES", TypeUser.EC },
            { "APrO", TypeUser.EC },
            { "BM", TypeUser.BM },
            { "BM1", TypeUser.BM },
            { "BM2", TypeUser.BM },
            { "BM3", TypeUser.BM },
            { "BDM", TypeUser.BM },
            { "BAM", TypeUser.BM },
            { "ABM", TypeUser.BM },
            { "ABM1", TypeUser.BM },
            { "ABM2", TypeUser.BM },
            { "ABM3", TypeUser.BM },
            { "FBM", TypeUser.BM },
            { "PO", TypeUser.BM },
            { "APRS", TypeUser.BM },
            { "APrS", TypeUser.BM },
            { "BSA", TypeUser.SAB },
            { "SAB", TypeUser.SAB },
            { "ATL", TypeUser.ALT },
            { "CM", TypeUser.CM },
            { "BTL", TypeUser.TTL },
            { "TTL", TypeUser.TTL },
            { "AEC", TypeUser.AEC },
            { "AAL", TypeUser.AEC },
            { "BDO", TypeUser.AEC },
            };

            // Load thêm từ DB
            var dbMappings = _unitOfWork.MapTypeUserRepository.GetQuery(a => a.Active); // lấy từ bảng MapTypeUser

            foreach (var item in dbMappings)
            {
                _typeUserMap[item.CDCM.Trim()] = item.TypeUser;
            }
        }
        public TypeUser? GetTypeUser(string maChucDanh)
        {
            if (string.IsNullOrWhiteSpace(maChucDanh))
                return null;

            return _typeUserMap.TryGetValue(maChucDanh.Trim(), out var typeUser)
                ? typeUser
                : (TypeUser?)null;
        }
        public List<string> GetAllCDCM()
        {
            return _typeUserMap.Keys.ToList();
        }
        private static readonly Dictionary<TypeUser, int> _sortUserMap =
            new Dictionary<TypeUser, int>()
        {
        { TypeUser.ASM, 0 },
        { TypeUser.BM , 1},
        { TypeUser.ALT, 2},
        { TypeUser.EC, 3},
        { TypeUser.SAB, 4 },
        { TypeUser.TTL, 5},
        { TypeUser.CM, 6},
        { TypeUser.AEC, 7},
        };

        public int GetSort(TypeUser typeUser)
        {
            return _sortUserMap.TryGetValue(typeUser, out var sort) ? sort : 0;
        }
    }

}