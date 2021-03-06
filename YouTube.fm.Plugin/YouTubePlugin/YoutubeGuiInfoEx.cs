﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;
using Google.GData.Client;
using Google.GData.YouTube;
using Google.YouTube;
using Lastfm.Services;
using MediaPortal.GUI.Library;
using YouTubePlugin.Class.Artist;
using YouTubePlugin.Class.Database;
using YouTubePlugin.DataProvider;
using Action = MediaPortal.GUI.Library.Action;

namespace YouTubePlugin
{
  public class YoutubeGuiInfoEx : YouTubeGuiInfoBase
  {
    [SkinControlAttribute(35)]
    protected GUIButtonControl playbutton = null;

    public string VideoId { get; set; }
    public YouTubeEntry YouTubeEntry { get; set; }
    public YouTubeEntry OldYouTubeEntry { get; set; }
    public ArtistItem ArtistItem { get; set; }
    private BackgroundWorker Worker_Youtube = new BackgroundWorker();
    private BackgroundWorker Worker_Artist = new BackgroundWorker();
    private BackgroundWorker Worker_Fast = new BackgroundWorker();
    private BackgroundWorker Worker_FanArt = new BackgroundWorker();

    public override int GetID
    {
      get
      {
        return 29053;
      }
      set
      {
      }
    }

    public override bool SupportsDelayedLoad
    {
      get
      {
        return false;
      }
    }

    public override bool Init()
    {
      Worker_Fast.DoWork += Worker_Fast_DoWork;
      Worker_Youtube.DoWork += Worker_Youtube_DoWork;
      Worker_Artist.DoWork += Worker_Artist_DoWork;
      Worker_FanArt.DoWork += Worker_FanArt_DoWork;
      Client.DownloadFileCompleted += DownloadLogoEnd;
      _setting.Load();
      return Load(GUIGraphicsContext.Skin + @"\youtubeinfoex.xml");
    }

    protected override void OnClicked(int controlId, GUIControl control, Action.ActionType actionType)
    {
      base.OnClicked(controlId, control, actionType);
      if (control == playbutton)
      {
        DoPlay(YouTubeEntry, true, null);
      }
    }

    void Worker_FanArt_DoWork(object sender, DoWorkEventArgs e)
    {
      if (string.IsNullOrEmpty(GUIPropertyManager.GetProperty("#Youtube.fm.Info.Artist.Name").Trim()))
        return;
      try
      {
        string file = GetFanArtImage(GUIPropertyManager.GetProperty("#Youtube.fm.Info.Artist.Name"));

        if (File.Exists(file) && imgFanArt != null)
        {
          Log.Debug("Youtube.Fm local fanart {0} loaded ", file);
          imgFanArt.Visible = true;
          imgFanArt.FileName = file;
          imgFanArt.DoUpdate();
          return;
        }

        if (Youtube2MP._settings.LoadOnlineFanart && !Client.IsBusy)
        {
          HTBFanArt fanart = new HTBFanArt();
          //file = GetFanArtImage(GUIPropertyManager.GetProperty("#Youtube.fm.Info.Artist.Name"));
          if (!File.Exists(file))
          {
            fanart.Search(GUIPropertyManager.GetProperty("#Youtube.fm.Info.Artist.Name"));
            Log.Debug("Youtube.Fm found {0} online fanarts for {1}", fanart.ImageUrls.Count,
                     GUIPropertyManager.GetProperty("#Youtube.fm.Info.Artist.Name"));
            if (fanart.ImageUrls.Count > 0)
            {
              Log.Debug("Youtube.Fm fanart download {0} to {1}  ", fanart.ImageUrls[0].Url, file);
              Youtube2MP.DownloadFile(fanart.ImageUrls[0].Url, file);
              GUIPropertyManager.SetProperty("#Youtube.fm.Info.Video.FanArt", file);
              Log.Debug("Youtube.Fm fanart {0} loaded ", file);
              if (imgFanArt != null)
              {
                imgFanArt.Visible = true;
                imgFanArt.FileName = file;
                imgFanArt.DoUpdate();
              }
            }
            else
            {
              if (imgFanArt != null) imgFanArt.Visible = false;
            }
          }
          else
          {
            GUIPropertyManager.SetProperty("#Youtube.fm.Info.Video.FanArt", file);
            if (imgFanArt != null)
            {
              imgFanArt.Visible = true;
              imgFanArt.FileName = file;
              imgFanArt.DoUpdate();
            }
          }
        }
        else
        {
          if (imgFanArt != null) imgFanArt.Visible = false;
        }
      }
      catch (Exception exception)
      {
        Log.Error(exception);
      }
    }

    void Worker_Fast_DoWork(object sender, DoWorkEventArgs e)
    {
      try
      {
        ClearLists();
        SetLabels(YouTubeEntry, "Info");
        Worker_FanArt.RunWorkerAsync();
        LoadRelatated(YouTubeEntry);
        LoadSimilarArtists(YouTubeEntry);
        Worker_Artist.RunWorkerAsync();
        Worker_Youtube.RunWorkerAsync();
      }
      catch (Exception exception)
      {
        Log.Error(exception);
      }
    }

    protected override void OnPageDestroy(int new_windowId)
    {
      GUIWaitCursor.Hide();
      base.OnPageDestroy(new_windowId);
    }

    void Worker_Youtube_DoWork(object sender, DoWorkEventArgs e)
    {
      try
      {
        Uri videoEntryUrl =
          new Uri("http://gdata.youtube.com/feeds/api/videos/" + Youtube2MP.GetVideoId(YouTubeEntry));

        Video video = Youtube2MP.request.Retrieve<Video>(videoEntryUrl);
        Feed<Comment> comments = Youtube2MP.request.GetComments(video);
        string cm = "\n------------------------------------------\n";
        foreach (Comment c in comments.Entries)
        {
          cm += c.Author + " : " + c.Content + "------------------------------------------\n";
        }
        GUIPropertyManager.SetProperty("#Youtube.fm.Info.Video.Comments", cm);
        string vidimg = GetBestUrl(YouTubeEntry.Media.Thumbnails);
        string vidfile = Youtube2MP.GetLocalImageFileName(vidimg);
        if (!string.IsNullOrEmpty(vidimg))
        {
          if (!File.Exists(vidfile))
          {
            GUIPropertyManager.SetProperty("#Youtube.fm.Info.Video.Image", " ");
            Youtube2MP.DownloadFile(vidimg, vidfile);
          }
        }
        GUIPropertyManager.SetProperty("#Youtube.fm.Info.Video.Image", vidfile);
      }
      catch (Exception exception)
      {
        Log.Error(exception);
      }
    }

    void Worker_Artist_DoWork(object sender, DoWorkEventArgs e)
    {
      try
      {
        ArtistManager.Instance.SetSkinProperties(YouTubeEntry, "Info", true, true);

        //ArtistItem = GetArtist(YouTubeEntry);
        //if(!string.IsNullOrEmpty(ArtistItem.Id))
        //{
        //  GUIPropertyManager.SetProperty("#Youtube.fm.Info.Artist.Name", ArtistItem.Name);
        //}

        //if (!string.IsNullOrEmpty(GUIPropertyManager.GetProperty("#Youtube.fm.Info.Artist.Name").Trim()))
        //{
        //  Track track = new Track(GUIPropertyManager.GetProperty("#Youtube.fm.Info.Artist.Name"),
        //                          GUIPropertyManager.GetProperty("#Youtube.fm.Info.Video.Title"),
        //                          Youtube2MP.LastFmProfile.Session);

        //  if (string.IsNullOrEmpty(GUIPropertyManager.GetProperty("#Youtube.fm.Info.Artist.Image").Trim()))
        //  {
        //    string imgurl =
        //      ArtistManager.Instance.GetArtistsImgUrl(GUIPropertyManager.GetProperty("#Youtube.fm.Info.Artist.Name"));
        //    if (string.IsNullOrEmpty(imgurl))
        //    {
        //      imgurl = track.Artist.GetImageURL(ImageSize.Huge);
        //    }
        //    if (!string.IsNullOrEmpty(imgurl))
        //    {
        //      string artistimg = Youtube2MP.GetLocalImageFileName(imgurl);
        //      if (!File.Exists(artistimg))
        //      {
        //        DownloadFile(imgurl, artistimg);
        //      }
        //      GUIPropertyManager.SetProperty("#Youtube.fm.Info.Artist.Image", artistimg);
        //    }
        //  }

        //  ArtistBio artistBio = track.Artist.Bio;
        //  artistBio.Lang = GUILocalizeStrings.GetCultureName(GUILocalizeStrings.CurrentLanguage());
        //  string contents = Regex.Replace(HttpUtility.HtmlDecode(artistBio.getContent()), "<.*?>",
        //                                  string.Empty);
        //  if(string.IsNullOrEmpty(contents))
        //  {
        //    artistBio.Lang = string.Empty;
        //    contents = Regex.Replace(HttpUtility.HtmlDecode(artistBio.getContent()), "<.*?>",
        //                                    string.Empty);
        //  }
        //  GUIPropertyManager.SetProperty("#Youtube.fm.Info.Artist.Bio", contents);
        //  string tags = " ";
        //  if (!string.IsNullOrEmpty(ArtistItem.Id))
        //  {
        //    if (string.IsNullOrEmpty(ArtistItem.Tags))
        //    {
        //      int i = 0;
        //      TopTag[] topTags = track.Artist.GetTopTags();
        //      foreach (TopTag tag in topTags)
        //      {
        //        tags += tag.Item.Name + "|";
        //        if (i < 5)
        //        {
        //          ArtistManager.Instance.SaveTag(ArtistItem, tag.Item.Name);
        //        }
        //        i++;
        //      }
        //    }
        //    else
        //    {
        //      tags = ArtistItem.Tags;
        //    }
        //  }
        //  GUIPropertyManager.SetProperty("#Youtube.fm.Info.Artist.Tags", tags);
        //}
      }
      catch (Exception ex)
      {
        Log.Error(ex);
      }
      GUIWaitCursor.Hide();
    }

    protected override void OnPageLoad()
    {
      GUIPropertyManager.SetProperty("#currentmodule", "Youtube.Fm/Info");
      if (OldYouTubeEntry == null || (OldYouTubeEntry != null && Youtube2MP.GetVideoId(OldYouTubeEntry) != Youtube2MP.GetVideoId(YouTubeEntry)))
      {
        OldYouTubeEntry = YouTubeEntry;
        ClearInfoLabels();
        //GUIWaitCursor.Init();
        GUIWaitCursor.Show();
        if (!Worker_Fast.IsBusy)
        {
          Worker_Fast.RunWorkerAsync();
        }
        else
        {
          // not a really good method need some rework using Worker_Fast.CancelAsync();
          System.Threading.Thread.Sleep(2000);
          Worker_Fast.RunWorkerAsync();
        }
      }
      base.OnPageLoad();
    }

    static public void ClearInfoLabels()
    {
      GUIPropertyManager.SetProperty("#Youtube.fm.Info.Video.Title", " ");
      GUIPropertyManager.SetProperty("#Youtube.fm.Info.Video.Duration", " ");
      GUIPropertyManager.SetProperty("#Youtube.fm.Info.Video.Autor", " ");
      GUIPropertyManager.SetProperty("#Youtube.fm.Info.Video.PublishDate", " ");
      GUIPropertyManager.SetProperty("#Youtube.fm.Info.Video.Image", " ");
      GUIPropertyManager.SetProperty("#Youtube.fm.Info.Video.ViewCount", " ");
      GUIPropertyManager.SetProperty("#Youtube.fm.Info.Video.WatchCount", " ");
      GUIPropertyManager.SetProperty("#Youtube.fm.Info.Video.FavoriteCount", " ");
      GUIPropertyManager.SetProperty("#Youtube.fm.Info.Video.Comments", " ");
      GUIPropertyManager.SetProperty("#Youtube.fm.Info.Video.Rating", "0");
      GUIPropertyManager.SetProperty("#Youtube.fm.Info.Video.FanArt", " ");
      GUIPropertyManager.SetProperty("#Youtube.fm.Info.Video.Summary", " ");

      GUIPropertyManager.SetProperty("#Youtube.fm.Info.Video.NumLike", " ");
      GUIPropertyManager.SetProperty("#Youtube.fm.Info.Video.NumDisLike", " ");
      GUIPropertyManager.SetProperty("#Youtube.fm.Info.Video.PercentLike", "0");

      GUIPropertyManager.SetProperty("#Youtube.fm.Info.Artist.Name", " ");
      GUIPropertyManager.SetProperty("#Youtube.fm.Info.Artist.Bio", " ");
      GUIPropertyManager.SetProperty("#Youtube.fm.Info.Artist.Tags", " ");
      GUIPropertyManager.SetProperty("#Youtube.fm.Info.Artist.Image", " ");
    }
  }
}
