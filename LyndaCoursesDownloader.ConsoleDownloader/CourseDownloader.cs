﻿using LyndaCoursesDownloader.CourseContent;
using ShellProgressBar;
using System;
using System.IO;
using System.Linq;
using System.Net;

namespace LyndaCoursesDownloader.ConsoleDownloader
{
    public static class CourseDownloader
    {
        private static ChildProgressBar pbarVideo;
        private static string currentVideo;
        #region ProgressBarOptions


        private readonly static ProgressBarOptions optionsChapter = new ProgressBarOptions
        {
            ScrollChildrenIntoView = true,
            ForegroundColor = ConsoleColor.Blue,
            ForegroundColorDone = ConsoleColor.DarkGreen,
            BackgroundColor = ConsoleColor.Gray,
            ProgressBarOnBottom = true,
            CollapseWhenFinished = false
        };

        private readonly static ProgressBarOptions optionsVideo = new ProgressBarOptions
        {
            ScrollChildrenIntoView = true,
            ForegroundColor = ConsoleColor.Yellow,
            ForegroundColorDone = ConsoleColor.DarkGreen,
            BackgroundColor = ConsoleColor.DarkGray,
            ProgressCharacter = '\u2593',
            ProgressBarOnBottom = true,
            CollapseWhenFinished = true
        };
        private readonly static ProgressBarOptions optionsCourse = new ProgressBarOptions
        {
            ScrollChildrenIntoView = true,
            ForegroundColor = ConsoleColor.DarkGray,
            ForegroundColorDone = ConsoleColor.DarkGreen,
            BackgroundColor = ConsoleColor.White,
            ProgressBarOnBottom = true,
            CollapseWhenFinished = false
        };
        #endregion
        private static string ToSafeFileName(string fileName) => string.Concat(fileName.Split(Path.GetInvalidFileNameChars()));
        public static void DownloadCourse(Course course, DirectoryInfo courseRootDirectory)
        {
            try
            {
                using (var pbarCourse = new ProgressBar(course.Chapters.ToList().Count, "Downloading Course : " + course.Name, optionsCourse))
                {
                    var courseDirectory = courseRootDirectory.CreateSubdirectory(ToSafeFileName(course.Name));
                    foreach (var chapter in course.Chapters)
                    {

                        var chapterDirectory = courseDirectory.CreateSubdirectory($"[{chapter.Id}] {ToSafeFileName(chapter.Name)}");
                        using (var pbarChapter = pbarCourse.Spawn(chapter.Videos.ToList().Count, $"Downloading Chapter {chapter.Id} : {chapter.Name}", optionsChapter))
                        {
                            foreach (var video in chapter.Videos)
                            {

                                currentVideo = video.Name;
                                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                                using (var downloadClient = new WebClient())
                                using (pbarVideo = pbarChapter.Spawn(100, $"Downloading Video {video.Id} : {currentVideo}", optionsVideo))
                                {
                                    downloadClient.DownloadProgressChanged += DownloadClient_DownloadProgressChanged;
                                    downloadClient.DownloadFileCompleted += DownloadClient_DownloadFileCompleted;
                                    string captionName = $"[{ video.Id}] { ToSafeFileName(video.Name)}.srt";
                                    string videoName = $"[{ video.Id}] { ToSafeFileName(video.Name)}.mp4";
                                    File.WriteAllText($"{Path.Combine(chapterDirectory.FullName, ToSafeFileName(captionName))}", video.CaptionText);
                                    downloadClient.DownloadFileTaskAsync(new Uri(video.VideoDownloadUrl), Path.Combine(chapterDirectory.FullName, videoName)).Wait();
                                }
                                pbarChapter.Tick();
                            }
                            pbarChapter.Message = $"Chapter {chapter.Id} : {chapter.Name} chapter has been downloaded successfully";
                        }
                        pbarCourse.Tick();
                    }
                    pbarCourse.Message = $"{course.Name} course has been downloaded successfully";
                }
            }
            catch (Exception ex)
            {
                TUI.ShowError("An error occured while downloading the course");
                TUI.ShowError("Error details : " + ex.StackTrace);
                TUI.ShowError("Trying again...");
                DownloadCourse(course, courseRootDirectory);
            }
        }

        private static void DownloadClient_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            pbarVideo.Message = "Video : " + currentVideo + " has been downloaded successfully";
            pbarVideo.AsProgress<float>().Report(1);
        }

        private static void DownloadClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            float KbReceived = e.BytesReceived / 1024;
            float TotalKbToReceive = e.TotalBytesToReceive / 1024;
            //pbarVideo.Message = "Downloading Video : " + currentVideo + "  " + KbReceived + "KB out of " + TotalKbToReceived;
            pbarVideo.Message = String.Format("Downloading Video : {0} {1}KB out of {2}KB", currentVideo, KbReceived, TotalKbToReceive);
            float percentage = KbReceived / TotalKbToReceive;
            var progress = pbarVideo.AsProgress<float>();
            progress?.Report(percentage);
        }
    }
}
