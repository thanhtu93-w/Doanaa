using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DoAn.Models
{
    public class PackageViewModel
    {
        public Package Package { get; set; }
        public MemberPackage MemberPackage { get; set; } 
    }

    public class PackageIndexViewModel
    {
        public List<MemberPackage> MyPackages { get; set; }
        public List<Package> AllPackages { get; set; }

    }

}