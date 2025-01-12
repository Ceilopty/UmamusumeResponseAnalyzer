﻿using Spectre.Console;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UmamusumeResponseAnalyzer.Localization;

namespace UmamusumeResponseAnalyzer
{
    public static class ResourceUpdater
    {
        public static async Task TryUpdateProgram(string savepath = null!)
        {
            var path = Path.Combine(Path.GetTempPath(), "latest-UmamusumeResponseAnalyzer.exe");
            var exist = File.Exists(path);
            if (!string.IsNullOrEmpty(savepath))
            {
                path = savepath;
                File.Copy(Environment.ProcessPath!, path, true);

                using var Proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = true
                    }
                };
                Proc.Start(); //把新程序复制到原来的目录后就启动
                Environment.Exit(0);
            }
            else if (exist && !(MD5.HashData(File.ReadAllBytes(Environment.ProcessPath!)).SequenceEqual(MD5.HashData(File.ReadAllBytes(path))))) //临时目录与当前目录的不一致则认为未更新
            {
                CloseToUpdate();
                exist = false; //能执行到这就代表更新文件受损，已经被删掉了
            }

            if (exist) //删除临时文件
            {
                File.Delete(path);
                await UpdateAssets(); //既然有临时文件，那必然是刚更新过的，再执行一次更新保证数据即时
                AnsiConsole.Clear();
            }
        }
        public static void CloseToUpdate()
        {
            using (var proc = new Process()) //检查下载的文件是否正常
            {
                var output = string.Empty;
                try
                {
                    proc.StartInfo = new ProcessStartInfo
                    {
                        FileName = Path.Combine(Path.GetTempPath(), "latest-UmamusumeResponseAnalyzer.exe"),
                        Arguments = $"-v",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };
                    proc.Start();
                    while (!proc.StandardOutput.EndOfStream)
                    {
                        output = proc.StandardOutput.ReadLine();
                    }
                }
                catch
                {
                }
                if (string.IsNullOrEmpty(output))
                {
                    AnsiConsole.MarkupLine("[red]更新文件受损，主程序更新失败[/]");
                    File.Delete(Path.Combine(Path.GetTempPath(), "latest-UmamusumeResponseAnalyzer.exe"));
                    return;
                }
            }
            using var Proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(Path.GetTempPath(), "latest-UmamusumeResponseAnalyzer.exe"),
                    Arguments = $"--update \"{Environment.ProcessPath}\"",
                    UseShellExecute = true
                }
            };
            Proc.Start();
            Environment.Exit(0);
        }
        public static async Task UpdateAssets()
        {
            await AnsiConsole.Progress()
                .Columns(new ProgressColumn[]
                {
                            new TaskDescriptionColumn(),
                            new ProgressBarColumn(),
                            new PercentageColumn(),
                            new RemainingTimeColumn(),
                            new SpinnerColumn()
                })
                .StartAsync(async ctx =>
                {
                    var tasks = new List<Task>();

                    var eventTask = Download(ctx, Resource.LaunchMenu_Update_DownloadEventsInstruction, Database.EVENT_NAME_FILEPATH);
                    tasks.Add(eventTask);

                    var successEventTask = Download(ctx, Resource.LaunchMenu_Update_DownloadSuccessEventsInstruction, Database.SUCCESS_EVENT_FILEPATH);
                    tasks.Add(successEventTask);

                    var namesTask = Download(ctx, Resource.LaunchMenu_Update_DownloadNamesInstruction, Database.NAMES_FILEPATH);
                    tasks.Add(namesTask);

                    var skillTask = Download(ctx, Resource.LaunchMenu_Update_DownloadSkillDataInstruction, Database.SKILLS_FILEPATH);
                    tasks.Add(skillTask);

                    var talentSkillTask = Download(ctx, Resource.LaunchMenu_Update_DownloadTalentSkillInstruction, Database.TALENT_SKILLS_FILEPATH);
                    tasks.Add(talentSkillTask);

                    var factorIdTask = Download(ctx, Resource.LaunchMenu_Update_DownloadFactorIdsInstruction, Database.FACTOR_IDS_FILEPATH);
                    tasks.Add(factorIdTask);

                    await Task.WhenAll(tasks);
                });
            AnsiConsole.MarkupLine(Resource.LaunchMenu_Update_DownloadedInstruction);
            Console.ReadKey();
        }
        public static async Task UpdateProgram()
        {
            await AnsiConsole.Progress()
                .Columns(new ProgressColumn[]
                {
                            new TaskDescriptionColumn(),
                            new ProgressBarColumn(),
                            new PercentageColumn(),
                            new RemainingTimeColumn(),
                            new SpinnerColumn()
                })
                .StartAsync(async ctx =>
                {
                    var tasks = new List<Task>();

                    var programTask = Download(ctx, Resource.LaunchMenu_Update_DownloadProgramInstruction, Path.Combine(Path.GetTempPath(), "latest-UmamusumeResponseAnalyzer.exe"));
                    tasks.Add(programTask);

                    await Task.WhenAll(tasks);
                });
            if (File.Exists(Path.Combine(Path.GetTempPath(), "latest-UmamusumeResponseAnalyzer.exe"))) //如果临时目录里有这个文件说明找到了新版本，从这里启动临时目录中的程序更新
            {
                Console.WriteLine(Resource.LaunchMenu_Update_BeginUpdateProgramInstruction);
                Console.ReadKey();
                CloseToUpdate();
            }
            else
            {
                Console.WriteLine(Resource.LaunchMenu_Update_AlreadyLatestInstruction);
            }
            Console.WriteLine(Resource.LaunchMenu_Options_BackToMenuInstruction);
            Console.ReadKey();
        }
        public static async Task DownloadNetFilter(string nfapiPath, string nfdriverPath, string redirectorPath)
        {
            if (!File.Exists(nfapiPath) || !File.Exists(nfdriverPath) || !File.Exists(redirectorPath))
            {
                await AnsiConsole.Progress()
                    .Columns(new ProgressColumn[]
                    {
                            new TaskDescriptionColumn(),
                            new ProgressBarColumn(),
                            new PercentageColumn(),
                            new RemainingTimeColumn(),
                            new SpinnerColumn()
                    })
                    .StartAsync(async ctx =>
                    {
                        var tasks = new List<Task>();

                        var nfAPITask = Download(ctx, "正在下载nfapi.dll", nfapiPath);
                        tasks.Add(nfAPITask);

                        var nfDriverTask = Download(ctx, "正在下载nfdriver.sys", nfdriverPath);
                        tasks.Add(nfDriverTask);

                        var redirectorTask = Download(ctx, "正在下载Redirector.dll", redirectorPath);
                        tasks.Add(redirectorTask);

                        await Task.WhenAll(tasks);
                    });
            }
        }
        public static async Task DownloadCmder(string cmderPath)
        {
            await AnsiConsole.Progress()
                .Columns(new ProgressColumn[]
                {
                            new TaskDescriptionColumn(),
                            new ProgressBarColumn(),
                            new PercentageColumn(),
                            new RemainingTimeColumn(),
                            new SpinnerColumn()
                })
                .StartAsync(async ctx =>
                {
                    var zipPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UmamusumeResponseAnalyzer", "cmder.zip");
                    var download = Download(ctx, "正在下载cmder.zip", zipPath);
                    var unzip = ctx.AddTask("解压cmder.zip", false).IsIndeterminate();
                    await download;
                    unzip.StartTask();
                    unzip.IsIndeterminate(false);
                    System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, cmderPath);
                    File.Delete(zipPath);
                    unzip.StopTask();
                });
        }
        static string GetDownloadUrl(string filepath)
        {
            const string CNHost = "https://assets.shuise.net/UmamusumeResponseAnalyzer";
            const string GithubHost = "https://raw.githubusercontent.com/EtherealAO/UmamusumeResponseAnalyzer/master";
            const string OSSHost = "https://assets.shuise.net/URA";
            var isCN = RegionInfo.CurrentRegion.Name == "CN" || CultureInfo.CurrentUICulture.Name == "zh-CN";
            var ext = Path.GetExtension(filepath);
            var filename = Path.GetFileName(filepath);
            switch (filename)
            {
                case var _ when filename.Contains("UmamusumeResponseAnalyzer.exe"):
                    filename = "UmamusumeResponseAnalyzer.exe";
                    break;
                case var _ when filename == "nfapi.dll":
                    return OSSHost + "/nfapi.dll";
                case var _ when filename == "nfdriver.sys":
                    return OSSHost + "/nfdriver.sys";
                case var _ when filename == "Redirector.dll":
                    return OSSHost + "/Redirector.dll";
                case var _ when filename == "cmder.zip":
                    return OSSHost + "/cmder.zip";
            }
            var host = !Config.Get(Resource.ConfigSet_ForceUseGithubToUpdate) && isCN ? CNHost : GithubHost;
            return ext switch
            {
                ".json" => $"{host}/{filename}",
                ".br" => $"{host}/{filename}",
                ".exe" => !Config.Get(Resource.ConfigSet_ForceUseGithubToUpdate) && isCN ? $"{host}/{filename}" : $"https://github.com/EtherealAO/UmamusumeResponseAnalyzer/releases/latest/download/UmamusumeResponseAnalyzer.exe"
            };
        }
        public static async Task Download(ProgressContext ctx = null!, string instruction = null!, string path = null!)
        {
            var downloadURL = GetDownloadUrl(path);
            var client = new HttpClient(new HttpClientHandler
            {
                AllowAutoRedirect = false
            })
            { DefaultRequestVersion = new Version(2, 0) };

            #region 检测更新服务器是否可用
            try
            {
                await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, downloadURL));
            }
            catch
            {
                if (new Uri(downloadURL).Host == "raw.githubusercontent.com")
                {
                    AnsiConsole.MarkupLine($"{downloadURL}: [red]无法连接到GitHub，请尝试使用代理[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine($"{downloadURL}: [red]无法连接到更新服务器，请尝试在设置中启用\"强制使用GitHub更新\"后再试[/]");
                }
                return;
            }
            #endregion

            var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, downloadURL), HttpCompletionOption.ResponseHeadersRead);
            while (response.StatusCode == System.Net.HttpStatusCode.MovedPermanently || response.StatusCode == System.Net.HttpStatusCode.Found)
            {
                response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, response.Headers.Location), HttpCompletionOption.ResponseHeadersRead);
            }
            var task = ctx?.AddTask(instruction, false);
            task?.MaxValue(response.Content.Headers.ContentLength ?? 0);
            task?.StartTask();
            if (task != null)
            {
                var programUpToDate = Path.GetExtension(path) == ".exe" && Environment.ProcessPath != default && response.Content.Headers.GetContentMD5().SequenceEqual(MD5.HashData(File.ReadAllBytes(Environment.ProcessPath)));
                var fileUpToDate = File.Exists(path) && response.Content.Headers.ContentLength == new FileInfo(path).Length;
                if (programUpToDate || fileUpToDate) //服务器返回的文件长度和当前文件大小一致，即没有新的可用版本，直接返回
                {
                    task.Increment(response.Content.Headers.ContentLength ?? 0);
                    return;
                }
            }

            response = await client.GetAsync(downloadURL, HttpCompletionOption.ResponseHeadersRead);
            while (response.StatusCode == System.Net.HttpStatusCode.MovedPermanently || response.StatusCode == System.Net.HttpStatusCode.Found)
            {
                response = await client.GetAsync(response.Headers.Location, HttpCompletionOption.ResponseHeadersRead);
            }

            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
            var buffer = new byte[8192];
            while (true)
            {
                var read = await contentStream.ReadAsync(buffer);
                if (read == 0)
                    break;
                task?.Increment(read);
                await fileStream.WriteAsync(buffer.AsMemory(0, read));
            }
        }
    }
}
