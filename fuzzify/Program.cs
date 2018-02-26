using System;
using Fuzzy;
using System.Collections.Generic;
using System.Linq;
using static System.Math;

namespace fuzzify
{
    class Program
    {
        static Version version = new Version(1, 0);
        static List<Linguistic> kpiList = new List<Linguistic> {
            new Linguistic("AvgBlindStockoutsModel_SetB_65Fields_0Max_Median",
            "unacceptable", "bad", "tolerable", "acceptable", "good", "excellent", "outstanding",
            new List<float> {1.1F, 1.6F, 1.8F, 2.2F, 2.4F, 2.7F, 2.9F, 3.3F, 3.6F, 4.1F, 4.5F, 5.9F}),
            new Linguistic("AvgStockOutPctModel_SetB_65Fields_0Max_Median",
            "unacceptable", "bad", "tolerable", "acceptable", "good", "excellent", "outstanding",
            new List<float> { 0.33F, 0.47F, 0.52F, 0.62F, 0.68F, 0.79F, 0.83F, 0.94F, 1.01F, 1.15F, 1.25F, 1.62F }),
            new Linguistic("AvgVendFillRatioModel_SetB_65Fields_0Max_Median",
            "unacceptable", "bad", "tolerable", "acceptable", "good", "excellent", "outstanding",
            new List<float> { 4.2F, 5.1F, 5.4F, 5.9F, 6.2F, 6.7F, 6.9F, 7.4F, 7.8F, 8.4F, 9F, 10.4F }),
            new Linguistic("AvgRemovedOutdatesModel_SetB_65Fields_0Max_Median",
            "unacceptable", "bad", "tolerable", "acceptable", "good", "excellent", "outstanding",
            new List<float> {20.3F, 31.1F, 37F, 55.7F, 75F, 134.6F, 181F, 269.3F, 323.8F, 454.5F, 539.8F, 823.6F}),
            new Linguistic("PcktsNoVend_PolicyModel_SetB_65Fields_0Max_Median",
            "unacceptable", "bad", "tolerable", "acceptable", "good", "excellent", "outstanding",
            new List<float> {141F, 367F, 488F, 689F, 763F, 891F, 965F, 1134F, 1231F, 1578F, 1777F, 2618F})
        };
        static void WriteHelp()
        {
            Console.WriteLine(
                "-h, --help\t\tshows this help text\n\n" + 
                "-kpi n x\tshows the resulting terms if linguistic variable n is set to x\n" +
                "\t\tn: 0..4\n" +
                "\t\tx: any rational number, i.e. 3.14\n\n" +
                "-l, --list" +
                "\t\tlists the lingustic variables with their names and terms\n"
            );
        }
        static void WriteFuzzifiedTerms(int kpi, float value)
        {
            var lVar = kpiList[kpi];
            lVar.Value = (float)value;
            var q = from x in lVar
                    where x.Value() > 0
                    select new { name = x.Name, membership = x.Value() };
            var list = q.ToList().OrderByDescending(x => x.membership);
            foreach (var term in list)
                Console.WriteLine(term);
        }
        static void WriteLinguisticList()
        {
            foreach (var lVar in kpiList)
            {
                Console.WriteLine("\n" + lVar.Name);
                foreach (var term in lVar)
                    Console.WriteLine($"\t{term.Name.PadRight(14)}\t{term.X1} {term.X2} {term.X3} {term.X4}");
            }
        }
        static void Main(string[] args)
        {
            int kpi = -1;
            float value = float.NaN;
            for (int i = 0; i < args.Count(); i++)
            {
                switch (args[i])
                {
                    case "-v":
                    case "--version":
                        Console.WriteLine($"fuzzify version {version}");
                        break;
                    case "-h":
                    case "--help":
                        WriteHelp();
                        break;
                    case "-l":
                    case "-list":
                        WriteLinguisticList();
                        break;
                    case "-kpi":
                        kpi = int.Parse(args[++i]);
                        break;
                    default:
                        value = float.Parse(args[i]);
                        WriteFuzzifiedTerms(kpi, value);
                        break;
                }
            }
        }
    }
}
