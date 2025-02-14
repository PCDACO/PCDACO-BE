using Domain.Entities;

namespace Persistance.Bogus;

public class BankInfoGenerator
{
    public static BankInfo[] Execute()
    {
        return
        [
            .. BankData.Select(bank => new BankInfo
            {
                BankLookUpId = bank.Value.BankLookUpId,
                Name = bank.Value.Name,
                Code = bank.Value.Code,
                Bin = bank.Value.Bin,
                ShortName = bank.Value.ShortName,
                LogoUrl = bank.Value.LogoUrl,
                IconUrl = bank.Value.IconUrl,
                SwiftCode = bank.Value.SwiftCode ?? string.Empty,
                LookupSupported = bank.Value.LookupSupported,
            })
        ];
    }

    private static readonly Dictionary<string, BankInfo> BankData =
        new()
        {
            {
                "ABB",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("2d71f051-72fa-48bd-a362-27a08e8df3b9"),
                    Name = "Ngân hàng TMCP An Bình",
                    Code = "ABB",
                    Bin = 970425,
                    ShortName = "ABBANK",
                    LogoUrl = "https://api.vietqr.io/img/ABB.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/ABB.svg",
                    SwiftCode = "ABBKVNVX",
                    LookupSupported = 1
                }
            },
            {
                "ACB",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("5b8bb807-6d1c-485b-986e-ad0fb2a6d4d2"),
                    Name = "Ngân hàng TMCP Á Châu",
                    Code = "ACB",
                    Bin = 970416,
                    ShortName = "ACB",
                    LogoUrl = "https://api.vietqr.io/img/ACB.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/ACB.svg",
                    SwiftCode = "ASCBVNVX",
                    LookupSupported = 1
                }
            },
            {
                "BAB",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("f599edf9-40c8-4bbf-87e8-230c1787a439"),
                    Name = "Ngân hàng TMCP Bắc Á",
                    Code = "BAB",
                    Bin = 970409,
                    ShortName = "BacABank",
                    LogoUrl = "https://api.vietqr.io/img/BAB.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/BAB.svg",
                    SwiftCode = "NASCVNVX",
                    LookupSupported = 1
                }
            },
            {
                "BIDV",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("f956e517-d4c1-43b1-bbbb-277bdc5f9037"),
                    Name = "Ngân hàng TMCP Đầu tư và Phát triển Việt Nam",
                    Code = "BIDV",
                    Bin = 970418,
                    ShortName = "BIDV",
                    LogoUrl = "https://api.vietqr.io/img/BIDV.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/BIDV.svg",
                    SwiftCode = "BIDVVNVX",
                    LookupSupported = 1
                }
            },
            {
                "BVB",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("a4f2d7b2-7b85-4420-8273-3c5d0a69e8f1"),
                    Name = "Ngân hàng TMCP Bảo Việt",
                    Code = "BVB",
                    Bin = 970438,
                    ShortName = "BaoVietBank",
                    LogoUrl = "https://api.vietqr.io/img/BVB.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/BVB.svg",
                    SwiftCode = "BVBVVNVX",
                    LookupSupported = 1
                }
            },
            {
                "CAKE",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("66fa0378-a568-4ca0-b958-deb03de55ab4"),
                    Name = "TMCP Việt Nam Thịnh Vượng - Ngân hàng số CAKE by VPBank",
                    Code = "CAKE",
                    Bin = 546034,
                    ShortName = "CAKE",
                    LogoUrl = "https://api.vietqr.io/img/CAKE.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/CAKE.svg",
                    SwiftCode = "",
                    LookupSupported = 1
                }
            },
            {
                "CBB",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("78d48899-8cf5-48d7-8572-645f43b0880d"),
                    Name = "Ngân hàng Thương mại TNHH MTV Xây dựng Việt Nam",
                    Code = "CBB",
                    Bin = 970444,
                    ShortName = "CBBank",
                    LogoUrl = "https://api.vietqr.io/img/CBB.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/CBB.svg",
                    SwiftCode = "GTBAVNVX",
                    LookupSupported = 1
                }
            },
            {
                "CIMB",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("d4e933a0-e11e-470b-9634-30638e4dfa66"),
                    Name = "Ngân hàng TNHH MTV CIMB Việt Nam",
                    Code = "CIMB",
                    Bin = 422589,
                    ShortName = "CIMB",
                    LogoUrl = "https://api.vietqr.io/img/CIMB.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/CIMB.svg",
                    SwiftCode = "CIBBVNVN",
                    LookupSupported = 1
                }
            },
            {
                "COOPB",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("1fe21f42-9b91-418c-a364-6e3190365009"),
                    Name = "Ngân hàng Hợp tác xã Việt Nam",
                    Code = "COOPB",
                    Bin = 970446,
                    ShortName = "Co-op Bank",
                    LogoUrl = "https://api.vietqr.io/img/COOPBANK.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/COOPBANK.svg",
                    SwiftCode = "",
                    LookupSupported = 1
                }
            },
            {
                "DAB",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("dc6920cb-8d1f-4393-a01d-db53d4b0e289"),
                    Name = "Ngân hàng TMCP Đông Á",
                    Code = "DAB",
                    Bin = 970406,
                    ShortName = "DongABank",
                    LogoUrl = "https://api.vietqr.io/img/DOB.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/DOB.svg",
                    SwiftCode = "EACBVNVX",
                    LookupSupported = 1
                }
            },
            {
                "DBS",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("74664d52-e0fb-4c56-b157-7b7506bdaeba"),
                    Name = "DBS Bank Ltd - Chi nhánh Thành phố Hồ Chí Minh",
                    Code = "DBS",
                    Bin = 796500,
                    ShortName = "DBSBank",
                    LogoUrl = "https://api.vietqr.io/img/DBS.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/DBS.svg",
                    SwiftCode = "DBSSVNVX",
                    LookupSupported = 1
                }
            },
            {
                "EIB",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("017e81d5-2cb8-4cc6-8444-30e9479aeb9b"),
                    Name = "Ngân hàng TMCP Xuất Nhập khẩu Việt Nam",
                    Code = "EIB",
                    Bin = 970431,
                    ShortName = "Eximbank",
                    LogoUrl = "https://api.vietqr.io/img/EIB.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/EIB.svg",
                    SwiftCode = "EBVIVNVX",
                    LookupSupported = 1
                }
            },
            {
                "GPB",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("d935586a-92d0-423d-9623-a8567d668879"),
                    Name = "Ngân hàng Thương mại TNHH MTV Dầu Khí Toàn Cầu",
                    Code = "GPB",
                    Bin = 970408,
                    ShortName = "GPBank",
                    LogoUrl = "https://api.vietqr.io/img/GPB.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/GPB.svg",
                    SwiftCode = "GBNKVNVX",
                    LookupSupported = 1
                }
            },
            {
                "HDB",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("c8068add-c733-4f5e-a34b-c598669d9aee"),
                    Name = "Ngân hàng TMCP Phát triển Thành phố Hồ Chí Minh",
                    Code = "HDB",
                    Bin = 970437,
                    ShortName = "HDBank",
                    LogoUrl = "https://api.vietqr.io/img/HDB.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/HDB.svg",
                    SwiftCode = "HDBCVNVX",
                    LookupSupported = 1
                }
            },
            {
                "HLB",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("6e0b0ed8-82b3-47fd-982a-fdae96b327a9"),
                    Name = "Ngân hàng TNHH MTV Hong Leong Việt Nam",
                    Code = "HLB",
                    Bin = 970442,
                    ShortName = "Hong Leong Bank",
                    LogoUrl = "https://api.vietqr.io/img/HLBVN.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/HLBVN.svg",
                    SwiftCode = "HLBBVNVX",
                    LookupSupported = 1
                }
            },
            {
                "HSBC",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("e23cdeab-344b-4e2a-bc22-7cb2d4075166"),
                    Name = "Ngân hàng TNHH MTV HSBC (Việt Nam)",
                    Code = "HSBC",
                    Bin = 458761,
                    ShortName = "HSBC",
                    LogoUrl = "https://api.vietqr.io/img/HSBC.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/HSBC.svg",
                    SwiftCode = "HSBCVNVX",
                    LookupSupported = 1
                }
            },
            {
                "IBKHCM",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("47f8f83f-a86d-4d8f-aeab-213417b2f904"),
                    Name = "Ngân hàng Công nghiệp Hàn Quốc - Chi nhánh TP. Hồ Chí Minh",
                    Code = "IBKHCM",
                    Bin = 970456,
                    ShortName = "IBKHCM",
                    LogoUrl = "https://api.vietqr.io/img/IBK.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/IBK - HCM.svg",
                    SwiftCode = "",
                    LookupSupported = 1
                }
            },
            {
                "IBKHN",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("58f9e003-036e-4bbb-b476-8f513d5153d6"),
                    Name = "Ngân hàng Công nghiệp Hàn Quốc - Chi nhánh Hà Nội",
                    Code = "IBKHN",
                    Bin = 970455,
                    ShortName = "IBKHN",
                    LogoUrl = "https://api.vietqr.io/img/IBK.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/IBK - HN.svg",
                    SwiftCode = "",
                    LookupSupported = 1
                }
            },
            {
                "IVB",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("3fbc766d-9a2e-4d80-80cf-3e62ca32abbc"),
                    Name = "Ngân hàng TNHH Indovina",
                    Code = "IVB",
                    Bin = 970434,
                    ShortName = "Indovina Bank",
                    LogoUrl = "https://api.vietqr.io/img/IVB.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/IVB.svg",
                    SwiftCode = "",
                    LookupSupported = 1
                }
            },
            {
                "KB",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("712627bb-9ab3-4d9c-b300-b58bf0bb5d14"),
                    Name = "Ngân hàng Đại chúng TNHH Kasikornbank",
                    Code = "KB",
                    Bin = 668888,
                    ShortName = "Kasikornbank",
                    LogoUrl = "https://api.vietqr.io/img/KBANK.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/KBank.svg",
                    SwiftCode = "KASIVNVX",
                    LookupSupported = 1
                }
            },
            {
                "KBKHCM",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("6ecbd4f5-6c85-4c07-ada4-5fa9eec49431"),
                    Name = "Ngân hàng Kookmin - Chi nhánh Thành phố Hồ Chí Minh",
                    Code = "KBKHCM",
                    Bin = 970463,
                    ShortName = "KookminHCM",
                    LogoUrl = "https://api.vietqr.io/img/KBHCM.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/KBHCM.svg",
                    SwiftCode = "",
                    LookupSupported = 1
                }
            },
            {
                "KBKHN",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("6ff67324-2611-4941-861b-abcf7b88bdee"),
                    Name = "Ngân hàng Kookmin - Chi nhánh Hà Nội",
                    Code = "KBKHN",
                    Bin = 970462,
                    ShortName = "KookminHN",
                    LogoUrl = "https://api.vietqr.io/img/KBHN.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/KBHN.svg",
                    SwiftCode = "",
                    LookupSupported = 1
                }
            },
            {
                "KLB",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("8892bfab-b050-40a4-b32b-73d11089578f"),
                    Name = "Ngân hàng TMCP Kiên Long",
                    Code = "KLB",
                    Bin = 970452,
                    ShortName = "KienLongBank",
                    LogoUrl = "https://api.vietqr.io/img/KLB.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/KLB.svg",
                    SwiftCode = "KLBKVNVX",
                    LookupSupported = 1
                }
            },
            {
                "LPB",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("7d721f6b-504a-42b4-8405-41ca6830971d"),
                    Name = "Ngân hàng TMCP Lộc Phát Việt Nam",
                    Code = "LPB",
                    Bin = 970449,
                    ShortName = "LPBank",
                    LogoUrl = "https://api.vietqr.io/img/LPB.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/LPB.svg",
                    SwiftCode = "LVBKVNVX",
                    LookupSupported = 1
                }
            },
            {
                "MB",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("fe05a74d-0b50-4569-8a78-d44648fc944c"),
                    Name = "Ngân hàng TMCP Quân đội",
                    Code = "MB",
                    Bin = 970422,
                    ShortName = "MBBank",
                    LogoUrl = "https://api.vietqr.io/img/MB.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/MB.svg",
                    SwiftCode = "MSCBVNVX",
                    LookupSupported = 1
                }
            },
            {
                "MSB",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("bf9303e0-4048-4545-92e6-0702f212d1a0"),
                    Name = "Ngân hàng TMCP Hàng Hải",
                    Code = "MSB",
                    Bin = 970426,
                    ShortName = "MSB",
                    LogoUrl = "https://api.vietqr.io/img/MSB.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/MSB.svg",
                    SwiftCode = "MCOBVNVX",
                    LookupSupported = 1
                }
            },
            {
                "NAB",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("2a3b45ca-38bc-49cb-97c3-e1fdd8f389e9"),
                    Name = "Ngân hàng TMCP Nam Á",
                    Code = "NAB",
                    Bin = 970428,
                    ShortName = "NamABank",
                    LogoUrl = "https://api.vietqr.io/img/NAB.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/NAB.svg",
                    SwiftCode = "NAMAVNVX",
                    LookupSupported = 1
                }
            },
            {
                "NCB",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("428d3f77-7b99-40c5-bb07-80d34eb2d6a9"),
                    Name = "Ngân hàng TMCP Quốc Dân",
                    Code = "NCB",
                    Bin = 970419,
                    ShortName = "NCB",
                    LogoUrl = "https://api.vietqr.io/img/NCB.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/NCB.svg",
                    SwiftCode = "NVBAVNVX",
                    LookupSupported = 1
                }
            },
            {
                "NHB",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("33777996-3ce0-4b6e-9b70-71004e97e4b8"),
                    Name = "Ngân hàng Nonghyup - Chi nhánh Hà Nội",
                    Code = "NHB",
                    Bin = 801011,
                    ShortName = "Nonghyup",
                    LogoUrl = "https://api.vietqr.io/img/NHB.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/NHB HN.svg",
                    SwiftCode = "",
                    LookupSupported = 1
                }
            },
            {
                "OCB",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("f7195395-0634-4ead-8773-88139abc8275"),
                    Name = "Ngân hàng TMCP Phương Đông",
                    Code = "OCB",
                    Bin = 970448,
                    ShortName = "OCB",
                    LogoUrl = "https://api.vietqr.io/img/OCB.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/OCB.svg",
                    SwiftCode = "ORCOVNVX",
                    LookupSupported = 1
                }
            },
            {
                "OJB",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("d764fad1-d25a-40fa-8008-f7ce7ef2c029"),
                    Name = "Ngân hàng Thương mại TNHH MTV Đại Dương",
                    Code = "OJB",
                    Bin = 970414,
                    ShortName = "Oceanbank",
                    LogoUrl = "https://api.vietqr.io/img/OCEANBANK.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/Oceanbank.svg",
                    SwiftCode = "OCBKUS3M",
                    LookupSupported = 1
                }
            },
            {
                "PBVN",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("60d111bf-c742-4a46-9990-5442c9ac44d8"),
                    Name = "Ngân hàng TNHH MTV Public Việt Nam",
                    Code = "PBVN",
                    Bin = 970439,
                    ShortName = "PublicBank",
                    LogoUrl = "https://api.vietqr.io/img/PBVN.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/PBVN.svg",
                    SwiftCode = "VIDPVNVX",
                    LookupSupported = 1
                }
            },
            {
                "PGB",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("06d27ed6-d640-4201-8f2e-ee5ff37f4580"),
                    Name = "Ngân hàng TMCP Xăng dầu Petrolimex",
                    Code = "PGB",
                    Bin = 970430,
                    ShortName = "PGBank",
                    LogoUrl = "https://api.vietqr.io/img/PGB.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/PGB.svg",
                    SwiftCode = "PGBLVNVX",
                    LookupSupported = 1
                }
            },
            {
                "PVCB",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("df183cfc-c104-4ff7-9ab7-c164229376c1"),
                    Name = "Ngân hàng TMCP Đại Chúng Việt Nam",
                    Code = "PVCB",
                    Bin = 970412,
                    ShortName = "PVcomBank",
                    LogoUrl = "https://api.vietqr.io/img/PVCB.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/PVCB.svg",
                    SwiftCode = "WBVNVNVX",
                    LookupSupported = 1
                }
            },
            {
                "SCB",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("85954e77-0754-479b-aac1-15bd283ebc3e"),
                    Name = "Ngân hàng TMCP Sài Gòn Thương Tín",
                    Code = "SCB",
                    Bin = 970403,
                    ShortName = "Sacombank",
                    LogoUrl = "https://api.vietqr.io/img/STB.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/STB.svg",
                    SwiftCode = "SGTTVNVX",
                    LookupSupported = 1
                }
            },
            {
                "SCBVN",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("793b6bfc-54c9-4012-83a4-ec5d0a4582c2"),
                    Name = "Ngân hàng TNHH MTV Standard Chartered Bank Việt Nam",
                    Code = "SCBVN",
                    Bin = 970410,
                    ShortName = "Standard Chartered VN",
                    LogoUrl = "https://api.vietqr.io/img/SCVN.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/SCVN.svg",
                    SwiftCode = "SCBLVNVX",
                    LookupSupported = 1
                }
            },
            {
                "SEAB",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("b7af5470-9304-4878-80b4-f687c678f83b"),
                    Name = "Ngân hàng TMCP Đông Nam Á",
                    Code = "SEAB",
                    Bin = 970440,
                    ShortName = "SeABank",
                    LogoUrl = "https://api.vietqr.io/img/SEAB.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/SEAB.svg",
                    SwiftCode = "SEAVVNVX",
                    LookupSupported = 1
                }
            },
            {
                "SGB",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("c970732d-c1ed-4703-9f6c-b46fc84239b2"),
                    Name = "Ngân hàng TMCP Sài Gòn Công Thương",
                    Code = "SGB",
                    Bin = 970400,
                    ShortName = "SaigonBank",
                    LogoUrl = "https://api.vietqr.io/img/SGICB.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/SGICB.svg",
                    SwiftCode = "SBITVNVX",
                    LookupSupported = 1
                }
            },
            {
                "SGCB",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("01ca8729-2519-46f2-8c4a-b109e7a45292"),
                    Name = "Ngân hàng TMCP Sài Gòn",
                    Code = "SGCB",
                    Bin = 970429,
                    ShortName = "SCB",
                    LogoUrl = "https://api.vietqr.io/img/SCB.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/SCB.svg",
                    SwiftCode = "SACLVNVX",
                    LookupSupported = 1
                }
            },
            {
                "SHB",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("2fb52185-a3d3-4ed1-8c45-5f71a2842de1"),
                    Name = "Ngân hàng TMCP Sài Gòn - Hà Nội",
                    Code = "SHB",
                    Bin = 970443,
                    ShortName = "SHB",
                    LogoUrl = "https://api.vietqr.io/img/SHB.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/SHB.svg",
                    SwiftCode = "SHBAVNVX",
                    LookupSupported = 1
                }
            },
            {
                "SHBVN",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("0540840b-7f2b-46c0-93d3-5040c042b737"),
                    Name = "Ngân hàng TNHH MTV Shinhan Việt Nam",
                    Code = "SHBVN",
                    Bin = 970424,
                    ShortName = "ShinhanBank",
                    LogoUrl = "https://api.vietqr.io/img/SHBVN.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/SHBVN.svg",
                    SwiftCode = "SHBKVNVX",
                    LookupSupported = 1
                }
            },
            {
                "TCB",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("d9b10c0e-e96d-4550-ac4d-1117d03c8afc"),
                    Name = "Ngân hàng TMCP Kỹ thương Việt Nam",
                    Code = "TCB",
                    Bin = 970407,
                    ShortName = "Techcombank",
                    LogoUrl = "https://api.vietqr.io/img/TCB.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/TCB.svg",
                    SwiftCode = "VTCBVNVX",
                    LookupSupported = 1
                }
            },
            {
                "TIMO",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("fa941344-6a64-4438-9bd5-11b57077da18"),
                    Name = "Ngân hàng số Timo",
                    Code = "TIMO",
                    Bin = 963388,
                    ShortName = "Timo",
                    LogoUrl = "https://vietqr.net/portal-service/resources/icons/TIMO.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/TIMO.svg",
                    SwiftCode = "",
                    LookupSupported = 1
                }
            },
            {
                "TPB",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("b468746c-1843-4c73-a0d9-70241c7cd8f6"),
                    Name = "Ngân hàng TMCP Tiên Phong",
                    Code = "TPB",
                    Bin = 970423,
                    ShortName = "TPBank",
                    LogoUrl = "https://api.vietqr.io/img/TPB.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/TPB.svg",
                    SwiftCode = "TPBVVNVX",
                    LookupSupported = 1
                }
            },
            {
                "UB",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("7b2a7fe7-5477-4b62-a9dc-93e2ba899fe0"),
                    Name = "TMCP Việt Nam Thịnh Vượng - Ngân hàng số Ubank by VPBank",
                    Code = "UB",
                    Bin = 546035,
                    ShortName = "Ubank",
                    LogoUrl = "https://api.vietqr.io/img/UBANK.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/Ubank.svg",
                    SwiftCode = "",
                    LookupSupported = 1
                }
            },
            {
                "UOB",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("913a0253-2a29-4918-8ed3-755162a73c23"),
                    Name = "Ngân hàng United Overseas",
                    Code = "UOB",
                    Bin = 970458,
                    ShortName = "United Overseas Bank",
                    LogoUrl = "https://api.vietqr.io/img/UOB.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/UOB.svg",
                    SwiftCode = "",
                    LookupSupported = 1
                }
            },
            {
                "VAB",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("dd586b22-8367-4439-95c8-98423a762ced"),
                    Name = "Ngân hàng TMCP Việt Á",
                    Code = "VAB",
                    Bin = 970427,
                    ShortName = "VietABank",
                    LogoUrl = "https://api.vietqr.io/img/VAB.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/VAB.svg",
                    SwiftCode = "VNACVNVX",
                    LookupSupported = 1
                }
            },
            {
                "VARB",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("3cdde0d0-44f0-4cd4-9953-391fee3ec04e"),
                    Name = "Ngân hàng Nông nghiệp và Phát triển Nông thôn Việt Nam",
                    Code = "VARB",
                    Bin = 970405,
                    ShortName = "Agribank",
                    LogoUrl = "https://api.vietqr.io/img/VBA.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/VBA.svg",
                    SwiftCode = "VBAAVNVX",
                    LookupSupported = 1
                }
            },
            {
                "VB",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("bf313f9e-c658-46e3-8d0b-530e013bd76d"),
                    Name = "Ngân hàng TMCP Việt Nam Thương Tín",
                    Code = "VB",
                    Bin = 970433,
                    ShortName = "VietBank",
                    LogoUrl = "https://api.vietqr.io/img/VIETBANK.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/VIETBANK.svg",
                    SwiftCode = "VNTTVNVX",
                    LookupSupported = 1
                }
            },
            {
                "VCB",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("cc2fa1a9-6ad5-4cc8-aee7-84a8cde9f62e"),
                    Name = "Ngân hàng TMCP Ngoại Thương Việt Nam",
                    Code = "VCB",
                    Bin = 970436,
                    ShortName = "Vietcombank",
                    LogoUrl = "https://api.vietqr.io/img/VCB.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/VCB.svg",
                    SwiftCode = "BFTVVNVX",
                    LookupSupported = 1
                }
            },
            {
                "VCCB",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("ec514a5b-1cd7-4d7c-bf97-c6c412bed60c"),
                    Name = "Ngân hàng TMCP Bản Việt",
                    Code = "VCCB",
                    Bin = 970454,
                    ShortName = "Ngân hàng Bản Việt",
                    LogoUrl = "https://api.vietqr.io/img/VCCB.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/VCCB.svg",
                    SwiftCode = "VCBCVNVX",
                    LookupSupported = 1
                }
            },
            {
                "VIB",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("5e1b6cfa-d5a7-4aae-9e59-dccc008c4eef"),
                    Name = "Ngân hàng TMCP Quốc tế Việt Nam",
                    Code = "VIB",
                    Bin = 970441,
                    ShortName = "VIB",
                    LogoUrl = "https://api.vietqr.io/img/VIB.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/VIB.svg",
                    SwiftCode = "VNIBVNVX",
                    LookupSupported = 1
                }
            },
            {
                "VNPTMONEY",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("69256b98-d6a2-46ea-a62e-90b02c8eac00"),
                    Name = "VNPT Money",
                    Code = "VNPTMONEY",
                    Bin = 971011,
                    ShortName = "VNPTMoney",
                    LogoUrl = "https://api.vietqr.io/img/VNPTMONEY.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/VNPTMONEY.svg",
                    SwiftCode = "",
                    LookupSupported = 0
                }
            },
            {
                "VPB",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("20351d8b-65ab-4cc4-90c2-b177d9c871b2"),
                    Name = "Ngân hàng TMCP Việt Nam Thịnh Vượng",
                    Code = "VPB",
                    Bin = 970432,
                    ShortName = "VPBank",
                    LogoUrl = "https://api.vietqr.io/img/VPB.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/VPB.svg",
                    SwiftCode = "VPBKVNVX",
                    LookupSupported = 1
                }
            },
            {
                "VRB",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("e01d061b-f468-4480-9078-0524083d3f09"),
                    Name = "Ngân hàng Liên doanh Việt - Nga",
                    Code = "VRB",
                    Bin = 970421,
                    ShortName = "VRB",
                    LogoUrl = "https://api.vietqr.io/img/VRB.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/VRB.svg",
                    SwiftCode = "",
                    LookupSupported = 1
                }
            },
            {
                "VTB",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("6fd6330a-2044-4254-9619-2e012c560b92"),
                    Name = "Ngân hàng TMCP Công thương Việt Nam",
                    Code = "VTB",
                    Bin = 970415,
                    ShortName = "VietinBank",
                    LogoUrl = "https://api.vietqr.io/img/ICB.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/ICB.svg",
                    SwiftCode = "ICBVVNVX",
                    LookupSupported = 1
                }
            },
            {
                "VTLMONEY",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("c0da3fe8-c530-410b-97f9-4af1d0d5c1c5"),
                    Name = "Viettel Money",
                    Code = "VTLMONEY",
                    Bin = 971005,
                    ShortName = "ViettelMoney",
                    LogoUrl = "https://api.vietqr.io/img/VIETTELMONEY.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/VTLMONEY.svg",
                    SwiftCode = "",
                    LookupSupported = 1
                }
            },
            {
                "WOO",
                new BankInfo
                {
                    BankLookUpId = Guid.Parse("bc286db1-9f40-417b-a3d7-494153fbeb2d"),
                    Name = "Ngân hàng TNHH MTV Woori Việt Nam",
                    Code = "WOO",
                    Bin = 970457,
                    ShortName = "Woori",
                    LogoUrl = "https://api.vietqr.io/img/WVN.png",
                    IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/WVN.svg",
                    SwiftCode = "",
                    LookupSupported = 1
                }
            },
        };
}
