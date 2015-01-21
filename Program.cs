using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using DataWarehouse;
using System.Linq;
using System.Dynamic;
using Dynamitey;
using LINQtoCSV;

namespace SqlServerTableCompare
{
    class Program
    {
        static void Main(string[] args)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            //variables
            var db = new PetaPoco.Database("DataWarehouse");
            const string oldTable = "Certification_old";
            const string newTable = "Certification";
            var oldColumnNames =string.Format("select column_name,* from information_schema.columns where table_name = '{0}' order by ordinal_position",oldTable);
            var oldSql = PetaPoco.Sql.Builder.Select("*").From(oldTable).Where("SurveyId='c894e0c9-b413-4afe-adc4-a0ac0069e194'");
            var newSql = PetaPoco.Sql.Builder.Select("*").From(newTable);
            
            //get column names
            var columnNames = db.Query<string>(oldColumnNames).ToList();
            
            //get data
            var oldData = db.Fetch<dynamic>(oldSql).ToList();
            var newData = db.Fetch<dynamic>(newSql).ToList();

            //checkdata
            var counter = 0;
            var totalCount = oldData.Count;
            var unMatchedData = new BlockingCollection<UnMatchedData>();

            Parallel.ForEach( oldData, (row)=>
                {
                    Console.WriteLine("Processing row {0} of {1}", counter++, totalCount);

                    var newRow = newData.SingleOrDefault(s => s.SurveyId == row.SurveyId);
                    if (newRow == null)
                    {
                        unMatchedData.Add(new UnMatchedData
                        {
                            SurveyId = row.SurveyId
                        });
                        return;
                    }

                    foreach (var columnName in columnNames)
                    {
                        if (columnName.ToLower() != "tt2" && columnName.ToLower() != "migratedon")
                        {

                            string oldVal = StringExtensions.TryParse(Dynamic.InvokeGet(row, columnName));
                            string newVal = StringExtensions.TryParse(Dynamic.InvokeGet(newRow, columnName));

                            if (newVal.StartsWith("."))
                            {
                                newVal = "0" + newVal;
                            }

                            if (oldVal.StartsWith("."))
                            {
                                oldVal = "0" + oldVal;
                            }

                            if (string.Compare(oldVal, newVal) != 0)
                            {
                                unMatchedData.Add(new UnMatchedData
                                    {
                                        SurveyId = row.SurveyId,
                                        ColumnName = columnName,
                                        OldResponse = oldVal,
                                        NewResponse = newVal
                                    });
                            }
                        }
                    }
                });

            //output file
            var fileDescription = new CsvFileDescription
            {
                SeparatorChar = ',',
                FirstLineHasColumnNames = true
            };
            var cc = new CsvContext();
            cc.Write(unMatchedData, "unMatchedData.csv", fileDescription);

            stopWatch.Stop();
            Console.WriteLine("Comparision finsihed in {0} minutes",stopWatch.Elapsed.Minutes);
            Console.ReadLine();
        }
    }

    public static class StringExtensions
    {
        public static string TryParse(this object obj)
        {
            try
            {
                var val= obj.ToString();
                if (string.IsNullOrWhiteSpace(val) || string.IsNullOrEmpty(val))
                {
                    return "";
                }
                return val.Trim().ToLower();
            }
            catch (Exception)
            {
                return "";
            }
        }
    }
}
