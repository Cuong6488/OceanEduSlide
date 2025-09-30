using OceanEduSlide.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OceanEduSlide.DAL
{
    internal interface IUserTypeService
    {
        TypeUser? GetTypeUser(string maChucDanh);
        int GetSort(TypeUser typeUser);
        //StatusUser? GetStatusUser(string maChucDanh);
        List<string> GetAllCDCM();

    }
}
