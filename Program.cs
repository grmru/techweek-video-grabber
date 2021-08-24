using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Io;
using System.Collections.Generic;
using System.Data;
using Newtonsoft.Json;
using System.IO;
using CsvHelper;
using System.Globalization;

namespace techweek_download
{
    class Program
    {
        public static bool GlobalExit { get; set; } = false;
        public static Dictionary<string, bool> inProcess = new Dictionary<string, bool>();
        public static List<DataRecord> ResultData = new List<DataRecord>();

        static void Main(string[] args)
        {
            // Source data (all values here are not working, it's example)
            // 
            // Hostname of LMS instance
            string host = "https://lms.futuremba.pro";
            // You PHPSESSID5 cookie, after authentication on LMS
            string cook = "PHPSESSID5=abcdef1234567890abcdef;";
            // Path to lessons list page
            string listPage = "/teach/control/stream/view/id/123456789";

            // running main method
            Run(host, cook, listPage);

            // waiting for all threads will finish their jobs
            while (!GlobalExit) { Thread.Sleep(500); }

            // exporting whole results in json and csv
            string json = JsonConvert.SerializeObject(ResultData);
            System.IO.File.WriteAllText("out.json", json);

            string csv = jsonToCSV(json, ",");
            System.IO.File.WriteAllText("out.csv", csv);
        }

        public static DataTable jsonStringToTable(string jsonContent)
        {
            DataTable dataTable = 
                JsonConvert.DeserializeObject<DataTable>(jsonContent);
            return dataTable;
        }

        public static string jsonToCSV(string jsonContent, string delimiter)
        {
            StringWriter csvString = new StringWriter();
            using (var csv =
                new CsvWriter(csvString,
                              new CsvHelper.Configuration.CsvConfiguration(CultureInfo.CurrentCulture)
                              {
                                  Delimiter = delimiter
                              }
                             )
                  )
            {
                using (var dataTable = jsonStringToTable(jsonContent))
                {
                    foreach (DataColumn column in dataTable.Columns)
                    {
                        csv.WriteField(column.ColumnName);
                    }
                    csv.NextRecord();

                    foreach (DataRow row in dataTable.Rows)
                    {
                        for (var i = 0; i < dataTable.Columns.Count; i++)
                        {
                            csv.WriteField(row[i]);
                        }
                        csv.NextRecord();
                    }
                }
            }

            return csvString.ToString();
        }

        static async void Run(string host, string cookie, string listPage)
        {
            var config = Configuration.Default.WithDefaultLoader().WithDefaultCookies();
            var address = host + listPage;
            var context = BrowsingContext.New(config);

            context.SetCookie(new Url(host), cookie);

            var document = await context.OpenAsync(address);

            var cellSelector = ".title";
            var cells = document.QuerySelectorAll(cellSelector);

            foreach (var c in cells)
            {
                string lessonUrl = c.GetAttribute("href");
                string title = c.TextContent;
                Console.WriteLine(title);

                inProcess.Add(lessonUrl, true);
                ParseLessonPage(host, cookie, title, lessonUrl);
            }

            bool allDone = false;
            while (!allDone)
            {
                allDone = true;
                foreach (bool working in inProcess.Values)
                {
                    if (working) { allDone = false; }
                }
                Thread.Sleep(500);
            }

            Console.WriteLine("---{done}---");
            GlobalExit = true;
        }

        static async void ParseLessonPage(string host, string cookie, string title, string lessonPage)
        {
            var config = Configuration.Default.WithDefaultLoader().WithDefaultCookies();
            var address = host + lessonPage;
            var context = BrowsingContext.New(config);

            context.SetCookie(new Url(host), cookie);

            var document = await context.OpenAsync(address);

            DataRecord rec = new DataRecord();

            var iframes = document.QuerySelectorAll("iframe");
            foreach (var c in iframes)
            {
                string[] splt = c.GetAttribute("src").Split('/');
                rec.YTCode = splt[splt.Length - 1];
            }

            var blocks = document.QuerySelectorAll(".text > div");
            foreach (var b in blocks)
            {
                rec.Authors = b.TextContent;
            }

            rec.URL = $"{host}{lessonPage}";
            Console.WriteLine($"---> URL:{rec.URL}");
            rec.Title = title;

            ResultData.Add(rec);

            inProcess[lessonPage] = false;
        }
    }
}
