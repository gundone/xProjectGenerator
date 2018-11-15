// WebParser
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

public static class WebParser
{
    private static CancellationTokenSource cts;

    private static List<string[]> _titles = new List<string[]>();

    private static StringBuilder sb;

    public static string ParseTitle(string content)
    {
        return Regex.Match(content, "\\<title\\b[^>]*\\>\\s*(?<Title>[\\s\\S]*?)\\</title\\>", RegexOptions.IgnoreCase).Groups["Title"].Value;
    }

    public static string ParseDescription(string content)
    {
        return Regex.Match(content, "\\<meta property=\"og:description\" content=\"(?<Desc>[\\s\\S]*?)\\\"", RegexOptions.IgnoreCase).Groups["Desc"].Value;
    }

    public static async Task<List<string[]>> AccessTheWebAsync(List<string> urlList, int start, int threads, BackgroundWorker bw)
    {
        cts = new CancellationTokenSource();
        try
        {
            return await AccessTheWebAsync(urlList, start, threads, cts.Token, bw);
        }
        catch (Exception exc)
        {
            exc.ToString();
            return null;
        }
    }

    private static async Task<List<string[]>> AccessTheWebAsync(List<string> urlList, int start, int batch, CancellationToken ct, BackgroundWorker bw)
    {
        sb = new StringBuilder();
        HttpClientHandler handler = new HttpClientHandler
        {
            AllowAutoRedirect = true
        };
        HttpClient client = new HttpClient(handler);
        _titles.Clear();
        for (int i = 0; i <= urlList.Count; i += batch)
        {
            IEnumerable<Task<string[]>> downloadTasksQuery = (from url in urlList
                                                              select ProcessURL(url, client, ct)).Skip(i).Take(batch);
            List<Task<string[]>> downloadTasks = downloadTasksQuery.ToList();
            while (downloadTasks.Count > 0)
            {
                Task<string[]> firstFinishedTask = await Task.WhenAny(downloadTasks);
                downloadTasks.Remove(firstFinishedTask);
                string[] title = await firstFinishedTask;
                _titles.Add(title);
                bw.ReportProgress(start + i + batch - downloadTasks.Count);
            }
        }
        try
        {
            File.AppendAllText("web-errors.log", sb.ToString());
        }
        catch
        {
        }
        return (from ttl in _titles
                where !string.IsNullOrWhiteSpace((ttl != null) ? ttl[1] : null)
                select ttl).ToList();
    }

    private static async Task<string[]> ProcessURL(string url, HttpClient client, CancellationToken ct, int tries = 3)
    {
        try
        {
            if (tries == 0)
            {
                sb.AppendLine($"[{DateTime.Now:hh:mm:ss.fff}] " + url + "Operation cancelled");
                return new string[3]
                {
                    url,
                    "",
                    ""
                };
            }
            if (!url.StartsWith("http://"))
            {
                url = "http://" + url;
            }
            if (url.StartsWith("http:///"))
            {
                url = url.Replace("http:///", "http://");
            }
            HttpResponseMessage response = await client.GetAsync(url, ct);
            if (response.StatusCode == HttpStatusCode.MovedPermanently || response.StatusCode == HttpStatusCode.MovedPermanently)
            {
                response.Headers.TryGetValues("Location", out IEnumerable<string> newLocation);
                return await ProcessURL(newLocation.First(), client, ct);
            }
            string str = await response.Content.ReadAsStringAsync();
            return new string[3]
            {
                url,
                ParseTitle(str),
                ParseDescription(str)
            };
        }
        catch (OperationCanceledException)
        {
            return await ProcessURL(url, client, ct, tries - 1);
        }
        catch (Exception exc)
        {
            sb.AppendLine(url + Environment.NewLine + exc.Message + exc.InnerException?.Message);
            return new string[3]
            {
                url,
                "",
                ""
            };
        }
    }
}
