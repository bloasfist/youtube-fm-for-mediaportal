using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.Net;
using System.Xml.Serialization;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Timers;
using Lastfm.Services;
using MediaPortal.GUI.Library;
using MediaPortal.Dialogs;
using MediaPortal.Util;
using MediaPortal.TagReader;
using MediaPortal.Localisation;
using MediaPortal.Configuration;
using MediaPortal.Player;
using MediaPortal.Playlists;

using Google.GData.Client;
using Google.GData.Extensions;
using Google.GData.YouTube;
using Google.GData.Extensions.MediaRss;
using Google.YouTube;
using YouTubePlugin.Class;
using YouTubePlugin.Class.Artist;


namespace YouTubePlugin
{
  public class PlayParams
  {
    public YouTubeEntry vid;
    public bool fullscr;
    public GUIListControl facade;
  }

  public class YoutubeGUIBase : GUIWindow
  {
    public Settings _setting = new Settings();
    protected YoutubePlaylistPlayer playlistPlayer;
    public System.Timers.Timer updateStationLogoTimer = new System.Timers.Timer(0.3 * 1000);
    public WebClient Client = new WebClient();
    public Queue downloaQueue = new Queue();
    private DownloadFileObject curentDownlodingFile;
    protected YouTubeQuery.UploadTime uploadtime = YouTubeQuery.UploadTime.AllTime;
    public FileDownloader VideoDownloader = new FileDownloader();

    static public void SetLabels(YouTubeEntry vid, string type)
    {
      ClearLabels(type);
      try
      {
        if (vid.Duration != null && vid.Duration.Seconds != null)
        {
          int sec = int.Parse(vid.Duration.Seconds);
          int min = sec/60;
          GUIPropertyManager.SetProperty("#Youtube.fm." + type + ".Video.Duration",
                                         string.Format("{0}:{1:0#}", min, (sec - (min*60))));
        }

        GUIPropertyManager.SetProperty("#Youtube.fm." + type + ".Video.PublishDate", vid.Published.ToShortDateString());
        if (vid.Authors != null && vid.Authors.Count > 0)
          GUIPropertyManager.SetProperty("#Youtube.fm." + type + ".Video.Autor", vid.Authors[0].Name);
        if (vid.Rating != null)
          GUIPropertyManager.SetProperty("#Youtube.fm." + type + ".Video.Rating", (vid.Rating.Average*2).ToString());
        if (vid.Statistics != null)
        {
          if (vid.Statistics.ViewCount != null)
            GUIPropertyManager.SetProperty("#Youtube.fm." + type + ".Video.ViewCount", vid.Statistics.ViewCount);
          if (vid.Statistics.WatchCount != null)
            GUIPropertyManager.SetProperty("#Youtube.fm." + type + ".Video.WatchCount", vid.Statistics.WatchCount);
          if (vid.Statistics.FavoriteCount != null)
            GUIPropertyManager.SetProperty("#Youtube.fm." + type + ".Video.FavoriteCount",
                                           vid.Statistics.FavoriteCount);
        }
        GUIPropertyManager.SetProperty("#Youtube.fm." + type + ".Video.Image",
                                       GetLocalImageFileName(GetBestUrl(vid.Media.Thumbnails)));
        if (vid.Media.Description != null)
          GUIPropertyManager.SetProperty("#Youtube.fm." + type + ".Video.Summary", vid.Media.Description.Value);
      }
      catch (Exception ex)
      {

      }
      if (vid.Title.Text.Contains("-"))
      {
        GUIPropertyManager.SetProperty("#Youtube.fm." + type + ".Video.Title", vid.Title.Text.Split('-')[1].Trim());
        if (type == "NowPlaying")
        {
          GUIPropertyManager.SetProperty("#Play.Current.Title", vid.Title.Text.Split('-')[1].Trim().Trim());
        }
        GUIPropertyManager.SetProperty("#Youtube.fm." + type + ".Artist.Name", vid.Title.Text.Split('-')[0].Trim());
        GUIPropertyManager.SetProperty("#Youtube.fm." + type + ".Video.FanArt", GetFanArtImage(vid.Title.Text.Split('-')[0]).Trim());
      }
      else
      {
        GUIPropertyManager.SetProperty("#Youtube.fm." + type + ".Video.Title", vid.Title.Text);
        GUIPropertyManager.SetProperty("#Youtube.fm." + type + ".Artist.Name", " ");
      }
      if (type == "NowPlaying")
      {
        GUIPropertyManager.SetProperty("#Youtube.fm." + type + ".Video.Comments", " ");
        //try
        //{
        //  Uri videoEntryUrl = new Uri("http://gdata.youtube.com/feeds/api/videos/" + vid.VideoId);
        //  Video video = Youtube2MP.request.Retrieve<Video>(videoEntryUrl);

        //  Feed<Comment> comments = Youtube2MP.request.GetComments(video);
        //  string cm = "";
        //  foreach (Comment c in comments.Entries)
        //  {
        //    cm += c.Content + "\n------------------------------------------\n";
        //  }
        //  GUIPropertyManager.SetProperty("#Youtube.fm." + type + ".Video.Comments", cm);
        //}
        //catch (Exception ex)
        //{
        //  Log.Error(ex);
        //}
      }
    }

    internal static void SetProperty(string property, string value)
    {
      if (property == null)
        return;

      //// If the value is empty always add a space
      //// otherwise the property will keep 
      //// displaying it's previous value
      if (String.IsNullOrEmpty(value))
        value = " ";

      GUIPropertyManager.SetProperty(property, value);
    }


    static public void ClearLabels(string type)
    {
      GUIPropertyManager.SetProperty("#Youtube.fm." + type + ".Video.Title", " ");
      GUIPropertyManager.SetProperty("#Youtube.fm." + type + ".Video.Duration", " ");
      GUIPropertyManager.SetProperty("#Youtube.fm." + type + ".Video.Autor", " ");
      GUIPropertyManager.SetProperty("#Youtube.fm." + type + ".Video.PublishDate", " ");
      GUIPropertyManager.SetProperty("#Youtube.fm." + type + ".Video.Image", " ");
      GUIPropertyManager.SetProperty("#Youtube.fm." + type + ".Video.ViewCount", " ");
      GUIPropertyManager.SetProperty("#Youtube.fm." + type + ".Video.WatchCount", " ");
      GUIPropertyManager.SetProperty("#Youtube.fm." + type + ".Video.FavoriteCount", " ");
      GUIPropertyManager.SetProperty("#Youtube.fm." + type + ".Video.Comments", " ");
      GUIPropertyManager.SetProperty("#Youtube.fm." + type + ".Video.Rating", "0");
      GUIPropertyManager.SetProperty("#Youtube.fm." + type + ".Artist.Name", " ");
      GUIPropertyManager.SetProperty("#Youtube.fm." + type + ".Video.FanArt", " ");
      GUIPropertyManager.SetProperty("#Youtube.fm." + type + ".Video.Summary", " ");
    }

    public string FormatTitle(YouTubeEntry vid)
    {

      return string.Format("{0}", vid.Title.Text);
    }
  
    static public string GetBestUrl(ExtensionCollection<MediaThumbnail> th)
    {
      if (th.Count > 0)
      {
        return th[th.Count - 1].Url;
      }
      else
      {
        return string.Empty;
      }
    }

    static public string GetLocalImageFileName(string strURL)
    {
      if (strURL == "")
        return string.Empty;
      if (strURL == "@")
        return string.Empty;
      string url = String.Format("youtubevideos-{0}.jpg", MediaPortal.Util.Utils.EncryptLine(strURL));
      return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.InternetCache), url); ;
    }

    public bool filterVideoContens(YouTubeEntry vid)
    {
      if (_setting.MusicFilter && _setting.UseExtremFilter)
      {
        if (vid.Title.Text.Contains("-"))
          return true;
        else
          return false;
      }
      return true;
    }

    public void Err_message(int langid)
    {
      GUIDialogOK dlgOK = (GUIDialogOK)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_OK);
      if (dlgOK != null)
      {
        dlgOK.SetHeading(25660);
        dlgOK.SetLine(1, langid);
        dlgOK.SetLine(2, "");
        dlgOK.DoModal(GetID);
      }
    }

    public void Err_message(string message)
    {
      GUIDialogOK dlgOK = (GUIDialogOK)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_OK);
      if (dlgOK != null)
      {
        dlgOK.SetHeading(25660);
        dlgOK.SetLine(1, message);
        dlgOK.SetLine(2, "");
        dlgOK.DoModal(GetID);
      }
    }


    public void BackGroundDoPlay(object param_)
    {
      PlayParams param = (PlayParams) param_;
      YouTubeEntry vid=param.vid;
      bool fullscr=param.fullscr;
      GUIListControl facade = param.facade;
      if (vid != null)
      {
        GUIWaitCursor.Hide();
        VideoInfo qa = SelectQuality(vid);
        if (qa.Quality == VideoQuality.Unknow)
          return;

        GUIWaitCursor.Show();
        Youtube2MP.temp_player.Reset();
        Youtube2MP.temp_player.RepeatPlaylist = true;
        Youtube2MP.temp_player.CurrentPlaylistType = PlayListType.PLAYLIST_MUSIC_VIDEO;
        PlayList playlist = Youtube2MP.temp_player.GetPlaylist(PlayListType.PLAYLIST_MUSIC_VIDEO);
        playlist.Clear();
        g_Player.PlayBackStopped += new g_Player.StoppedHandler(g_Player_PlayBackStopped);
        g_Player.PlayBackEnded += new g_Player.EndedHandler(g_Player_PlayBackEnded);
        AddItemToPlayList(vid, ref playlist, qa);

        if (facade != null)
        {
          qa.Items = new Dictionary<string, string>();
          int selected = facade.SelectedListItemIndex;
          for (int i = selected + 2; i < facade.ListItems.Count; i++)
          {
            AddItemToPlayList(facade.ListItems[i], ref playlist, new VideoInfo(qa));
          }
        }
        else
        {
          Youtube2MP.temp_player.RepeatPlaylist = false;
        }

        PlayListPlayer.SingletonPlayer.CurrentPlaylistType = PlayListType.PLAYLIST_NONE;
        Youtube2MP.player.CurrentPlaylistType = PlayListType.PLAYLIST_NONE;
        g_Player.Stop();
        Youtube2MP.temp_player.Play(0);
        GUIWaitCursor.Hide();

        if (g_Player.Playing && fullscr)
        {
          if (_setting.ShowNowPlaying)
          {
            GUIWindowManager.ActivateWindow(29052);
          }
          else
          {
            g_Player.ShowFullScreenWindow();
          }
        }

        if (!g_Player.Playing)
        {
          Err_message("Unable to playback the item ! ");
        }
      }      
    }

    public void DoPlay(YouTubeEntry vid, bool fullscr, GUIListControl facade)
    {
      PlayParams playParams = new PlayParams() {facade = facade, fullscr = fullscr, vid = vid};

      //Thread _thread = new Thread(new ParameterizedThreadStart(BackGroundDoPlay));
      //_thread.Start(playParams);
      BackGroundDoPlay(playParams);
    }

    public VideoInfo SelectQuality(YouTubeEntry vid)
    {
      VideoInfo info = new VideoInfo();
      info.Get(Youtube2MP.getIDSimple(vid.AlternateUri.Content));
      if (!string.IsNullOrEmpty(info.Reason))
      {
        Err_message(info.Reason);
        info.Quality = VideoQuality.Unknow;
        return info;
      }

      switch (Youtube2MP._settings.VideoQuality)
      {
        case 0:
          info.Quality = VideoQuality.Normal;
          break;
        case 1:
          info.Quality = VideoQuality.High;
          break;
        case 2:
          info.Quality = VideoQuality.HD;
          break;
        case 3:
          info.Quality = VideoQuality.FullHD;
          break;
        case 4:
          {
            string title = vid.Title.Text;
            if (info.FmtMap.Contains("18"))
              info.Quality = VideoQuality.High;
            if (info.FmtMap.Contains("22"))
              info.Quality = VideoQuality.HD;
            if (info.FmtMap.Contains("37"))
              info.Quality = VideoQuality.FullHD;
            break;
          }
        case 5:
          {

            GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_MENU);
            if (dlg == null) info.Quality = VideoQuality.Normal;
            dlg.Reset();
            dlg.SetHeading("Select video quality");
            dlg.Add("Normal quality");
            dlg.Add("High quality");
            if (info.FmtMap.Contains("22"))
            {
              dlg.Add("HD quality");
            }
            if (info.FmtMap.Contains("37"))
            {
              dlg.Add("Full HD quality");
            }
            dlg.DoModal(GetID);
            if (dlg.SelectedId == -1) info.Quality = VideoQuality.Unknow;
            switch (dlg.SelectedLabel)
            {
              case 0:
                info.Quality = VideoQuality.Normal;
                break;
              case 1:
                info.Quality = VideoQuality.High;
                break;
              case 2:
                info.Quality = VideoQuality.HD;
                break;
              case 3:
                info.Quality = VideoQuality.FullHD;
                break;
            }
          }
          break;
      }
      return info;
    }

    void g_Player_PlayBackEnded(g_Player.MediaType type, string filename)
    {
      try
      {
        g_Player.Release();
        g_Player.PlayBackStopped -= g_Player_PlayBackStopped;
        g_Player.PlayBackEnded -= g_Player_PlayBackEnded;
        ClearLabels("NowPlaying");
        Youtube2MP.player.DoOnStop();
        //if (GUIWindowManager.ActiveWindow == 29052)
        //  GUIWindowManager.ShowPreviousWindow();
      }
      catch
      {
      }
    }

    void g_Player_PlayBackStopped(g_Player.MediaType type, int stoptime, string filename)
    {
      try
      {
        g_Player.Release();
        g_Player.PlayBackStopped -= g_Player_PlayBackStopped;
        g_Player.PlayBackEnded -= g_Player_PlayBackEnded;
        Youtube2MP.player.DoOnStop();
        ClearLabels("NowPlaying");
        if (GUIWindowManager.ActiveWindow == 29052)
          GUIWindowManager.ShowPreviousWindow();
      }
      catch
      {
      }
    }

    public void AddItemToPlayList(GUIListItem pItem, VideoInfo qa)
    {
        PlayList playList = Youtube2MP.player.GetPlaylist(PlayListType.PLAYLIST_MUSIC_VIDEO);
      AddItemToPlayList(pItem, ref playList, qa);
    }

    public void AddItemToPlayList(GUIListItem pItem, ref PlayList playList,VideoInfo qa)
    {
      if (playList == null || pItem == null)
        return;
      string PlayblackUrl = "";
      
      YouTubeEntry vid;

      LocalFileStruct file = pItem.MusicTag as LocalFileStruct;
      if (file != null)
      {
        Uri videoEntryUrl = new Uri("http://gdata.youtube.com/feeds/api/videos/" + file.VideoId);
        Video video = Youtube2MP.request.Retrieve<Video>(videoEntryUrl);
        vid = video.YouTubeEntry;
        
      }
      else
      {
        vid = pItem.MusicTag as YouTubeEntry;
      }

      if (vid != null)
      {
          if (vid.Media.Contents.Count > 0)
          {
              PlayblackUrl = string.Format("http://www.youtube.com/v/{0}", Youtube2MP.getIDSimple(vid.Id.AbsoluteUri));
          }
          else
          {
              PlayblackUrl = vid.AlternateUri.ToString();
          }

          PlayListItem playlistItem = new PlayListItem();
          playlistItem.Type = PlayListItem.PlayListItemType.VideoStream;// Playlists.PlayListItem.PlayListItemType.Audio;
          qa.Entry = vid;
          playlistItem.FileName = PlayblackUrl;
          playlistItem.Description = pItem.Label;
          playlistItem.Duration = pItem.Duration;
          playlistItem.MusicTag = qa;
          playList.Add(playlistItem);
      }
    }

    public void AddItemToPlayList(YouTubeEntry vid, ref PlayList playList, VideoInfo qa)
    {
        if (playList == null || vid == null)
            return;
        string PlayblackUrl = "";

        List<GUIListItem> list = new List<GUIListItem>();

        if (vid != null)
        {
            if (vid.Media.Contents.Count > 0)
            {
                PlayblackUrl = string.Format("http://www.youtube.com/v/{0}", Youtube2MP.getIDSimple(vid.Id.AbsoluteUri));
            }
            else
            {
                PlayblackUrl = vid.AlternateUri.ToString();
            }
            PlayListItem playlistItem = new PlayListItem();
            playlistItem.Type = PlayListItem.PlayListItemType.VideoStream;// Playlists.PlayListItem.PlayListItemType.Audio;
            qa.Entry = vid;
            playlistItem.FileName = PlayblackUrl;
            playlistItem.Description = vid.Title.Text;
            if (vid.Duration != null && vid.Duration.Seconds != null)
              playlistItem.Duration = Convert.ToInt32(vid.Duration.Seconds, 10);

            playlistItem.MusicTag = qa;
            playList.Add(playlistItem);
        }
    }

    static public string GetFanArtImage(string artist)
    {
      return String.Format(@"{0}\{1}_fanart.jpg", Thumbs.MusicArtists, MediaPortal.Util.Utils.MakeFileName(artist));
    }

    #region download manager



    private string DownloadImage(string Url)
    {
      //string localFile = GetLocalImageFileName(Url);
      //if (!File.Exists(localFile) && !string.IsNullOrEmpty(Url))
      //{
      //  downloaQueue.Enqueue(new DownloadFileObject(localFile, Url));
      //}
      return DownloadImage(Url, null);
    }

    private string DownloadImage(string Url, string localFile, GUIListItem item)
    {
      if (!File.Exists(localFile) && !string.IsNullOrEmpty(Url))
      {
        downloaQueue.Enqueue(new DownloadFileObject(localFile, Url, item));
      }
      return localFile;
    }

    public string DownloadImage(string Url, GUIListItem listitem)
    {
      string localFile = GetLocalImageFileName(Url);
      if (!File.Exists(localFile) && !string.IsNullOrEmpty(Url))
      {
        downloaQueue.Enqueue(new DownloadFileObject(localFile, Url, listitem));
      }
      return localFile;
    }

    public void OnDownloadTimedEvent(object source, ElapsedEventArgs e)
    {
      BackgroundWorker backgroundWorker1 = new BackgroundWorker();
      backgroundWorker1.DoWork += new DoWorkEventHandler(backgroundWorker1_DoWork);
      backgroundWorker1.RunWorkerAsync();
    }

    void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
    {
      OnDownloadTimedEvent();
    }

    public void OnDownloadTimedEvent()
    {
      if (!Client.IsBusy && downloaQueue.Count > 0)
      {
        curentDownlodingFile = (DownloadFileObject) downloaQueue.Dequeue();
        try
        {
          if (curentDownlodingFile.ListItem != null)
          {
            SiteItemEntry siteItemEntry = curentDownlodingFile.ListItem.MusicTag as SiteItemEntry;
            if (siteItemEntry != null && !string.IsNullOrEmpty(siteItemEntry.GetValue("id")))
            {
              ArtistItem artistItem = ArtistManager.Instance.GetArtistsById(siteItemEntry.GetValue("id"));
              if (string.IsNullOrEmpty(curentDownlodingFile.Url) || curentDownlodingFile.Url.Contains("@") ||
                  curentDownlodingFile.Url.Contains("ytimg.com"))
              {
                try
                {
                  Artist artist = new Artist(artistItem.Name, Youtube2MP.LastFmProfile.Session);
                  artistItem.Img_url = artist.GetImageURL(ImageSize.Large);
                  ArtistManager.Instance.Save(artistItem);
                  curentDownlodingFile.Url = artistItem.Img_url;
                  curentDownlodingFile.FileName = GetLocalImageFileName(curentDownlodingFile.Url);
                }
                catch
                {
                }
              }
            }
          }
        }
        catch (Exception ex)
        {
          Log.Error(ex);
        }
        if (!string.IsNullOrEmpty(curentDownlodingFile.FileName) &&!File.Exists(curentDownlodingFile.FileName))
        {
          try
          {
            Client.DownloadFileAsync(new Uri(curentDownlodingFile.Url), Path.GetTempPath() + @"\station.png");
          }
          catch
          {
            downloaQueue.Enqueue(curentDownlodingFile);
          }

        }
        else
        {
          OnDownloadTimedEvent(null, null);
        }
      }
    }

    public void DownloadLogoEnd(object sender, AsyncCompletedEventArgs e)
    {
      if (e.Error == null)
      {
        try
        {
          File.Copy(Path.GetTempPath() + @"\station.png", curentDownlodingFile.FileName, true);
          if (curentDownlodingFile.ListItem != null && File.Exists(curentDownlodingFile.FileName))
          {
            curentDownlodingFile.ListItem.ThumbnailImage = curentDownlodingFile.FileName;
            curentDownlodingFile.ListItem.IconImage = curentDownlodingFile.FileName;
            curentDownlodingFile.ListItem.IconImageBig = curentDownlodingFile.FileName;
            curentDownlodingFile.ListItem.RefreshCoverArt();
            OnDownloadTimedEvent(null, null);
          }
        }
        catch (Exception ex)
        {
          Log.Error(ex);
        }
        //UpdateGui();
      }
    }
   
    protected  YouTubeQuery SetParamToYouTubeQuery(YouTubeQuery query, bool safe)
    {

      //order results by the number of views (most viewed first)
      query.OrderBy = "viewCount";
      query.StartIndex = 1;
      //query.LR = "hu";
      if (_setting.UseExtremFilter)
        query.NumberToRetrieve = 50;
      else
        query.NumberToRetrieve = 50;
      ////exclude restricted content from the search
      //query.Racy = "exclude";
      query.SafeSearch = YouTubeQuery.SafeSearchValues.None;
      if (uploadtime != YouTubeQuery.UploadTime.AllTime)
        query.Time = uploadtime;
      if (_setting.MusicFilter && !safe)
      {
        query.Categories.Add(new QueryCategory("Music", QueryCategoryOperator.AND));
      }

      return query;
    }
    #endregion
  }
}
