using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.Net;
using System.Xml.Serialization;
using System.IO;
using System.Text;
using System.Xml;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Timers;

using MediaPortal.GUI.Library;
using MediaPortal.Dialogs;
using MediaPortal.Util;
using MediaPortal.Localisation;
using MediaPortal.Configuration;
using MediaPortal.Player;
using MediaPortal.Playlists;
using MediaPortal.Music.Database;

using Google.GData.Client;
using Google.GData.Extensions;
using Google.GData.YouTube;
using Google.GData.Extensions.MediaRss;
using Google.YouTube;


namespace YouTubePlugin
{
  public enum View
  {
    List = 0,
    Icons = 1,
    BigIcons = 2,
    Albums = 3,
    PlayList = 4,
    Filmstrip = 5
  }

  public class YouTubeGUI : YoutubeGUIBase, ISetupForm 
  {

    #region MapSettings class
    [Serializable]
    public class MapSettings
    {
      protected int _SortBy;
      protected int _ViewAs;
      protected bool _SortAscending;

      public MapSettings()
      {
        // Set default view
        _SortBy = 0;
        _ViewAs = (int)View.List;
        _SortAscending = true;
      }

      [XmlElement("SortBy")]
      public int SortBy
      {
        get { return _SortBy; }
        set { _SortBy = value; }
      }

      [XmlElement("ViewAs")]
      public int ViewAs
      {
        get { return _ViewAs; }
        set { _ViewAs = value; }
      }

      [XmlElement("SortAscending")]
      public bool SortAscending
      {
        get { return _SortAscending; }
        set { _SortAscending = value; }
      }
    }
    #endregion

    #region Base variables

    #endregion


    #region locale vars


    private Stack NavigationStack = new Stack();
    MapSettings mapSettings = new MapSettings();
    static GUIDialogProgress dlgProgress;

    YouTubeService service = new YouTubeService("My YouTube Videos For MediaPortal", "ytapi-DukaIstvan-MyYouTubeVideosF-d1ogtvf7-0", "AI39si621gfdjmMcOzulF3QlYFX_vWCqdXFn_Y5LzIgHolPoSetAUHxDPx8u4YXZVkU7CmeiObnzavrsjL5GswY_GGEmen9kdg");


    #endregion

    #region skin connection
    [SkinControlAttribute(50)]
    protected GUIFacadeControl listControl = null;
    //[SkinControlAttribute(2)]
    //protected GUISortButtonControl sortButton = null;
    [SkinControlAttribute(2)]
    protected GUIButtonControl homeButton = null;
    [SkinControlAttribute(3)]
    protected GUIButtonControl btnSwitchView = null;
    [SkinControlAttribute(5)]
    protected GUIButtonControl searchButton = null;
    [SkinControlAttribute(6)]
    protected GUIButtonControl searchHistoryButton = null;
    [SkinControlAttribute(7)]
    protected GUIButtonControl btnPlayList = null;
    [SkinControlAttribute(8)]
    protected GUIButtonControl btnNowPlaying = null;

    #endregion


    public YouTubeGUI()
    {
      _setting.Load();
      GetID = GetWindowId();
      Youtube2MP._settings = _setting;
      Youtube2MP.service = service;
      updateStationLogoTimer.AutoReset = true;
      updateStationLogoTimer.Enabled = false;
      updateStationLogoTimer.Elapsed += OnDownloadTimedEvent;
      Client.DownloadFileCompleted += DownloadLogoEnd;
      VideoDownloader.DownloadComplete += VideoDownloader_DownloadComplete;
      VideoDownloader.ProgressChanged += VideoDownloader_ProgressChanged;
      GUIWindowManager.Receivers += GUIWindowManager_Receivers;
 
    }

    void VideoDownloader_ProgressChanged(object sender, DownloadEventArgs e)
    {
      if (dlgProgress != null)
      {
        dlgProgress.SetLine(2, string.Format("{0} Mb / {1} Mb ({2}%)", e.TotalFileSize/1024/1024, e.CurrentFileSize/1024/1024, e.PercentDone));
        dlgProgress.ShowProgressBar(true);
        dlgProgress.SetPercentage(e.PercentDone);
        dlgProgress.Progress();
      }
    }

    void VideoDownloader_DownloadComplete(object sender, EventArgs e)
    {

      GUIDialogNotify dlg1 = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
      if (dlg1 != null)
      {
        dlg1.Reset();
        dlg1.SetHeading("Download done");
        dlg1.SetText(VideoDownloader.DownloadingTo);
        dlg1.Reset();
        dlg1.TimeOut = 5;
        dlg1.DoModal(GetID);
      }
      Youtube2MP._settings.LocalFile.Items.Add(new LocalFileStruct(VideoDownloader.DownloadingTo, VideoDownloader.Entry.VideoId, VideoDownloader.Entry.Title.Text));
      Youtube2MP._settings.LocalFile.Save();
      string imageFile = GetLocalImageFileName(GetBestUrl(VideoDownloader.Entry.Media.Thumbnails));
      try
      {
          if (File.Exists(imageFile))
          {
              File.Copy(imageFile, Path.GetDirectoryName(VideoDownloader.DownloadingTo) + "\\" + Path.GetFileNameWithoutExtension(VideoDownloader.DownloadingTo) + ".png");
          }
      }
      catch
      {
      }
      if (dlgProgress != null)
      {
        dlgProgress.SetPercentage(100);
        dlgProgress.Progress();
        dlgProgress.ShowProgressBar(true);
        dlgProgress.Close();
        dlgProgress = null;
      }
    }

    void GUIWindowManager_Receivers(GUIMessage message)
    {
    }

    #region ISetupForm Members
    // return name of the plugin
    public string PluginName()
    {
      return _setting.PluginName;
    }
    // returns plugin description
    public string Description()
    {
      return "Plugin for expose the YouTube Music contents";
    }
    // returns author
    public string Author()
    {
      return "Dukus";
    }
    // shows the setup dialog
    public void ShowPlugin()
    {
      SetupForm setup = new SetupForm();
      setup._settings = _setting;
      setup.ShowDialog();
    }
    // enable / disable
    public bool CanEnable()
    {
      return true;
    }
    // returns the unique id again
    public int GetWindowId()
    {
      return 29050;
    }
    // default enable?
    public bool DefaultEnabled()
    {
      return true;
    }
    // has setup gui?
    public bool HasSetup()
    {
      return true ;
    }
    // home button
    public bool GetHome(out string strButtonText, out string strButtonImage,
      out string strButtonImageFocus, out string strPictureImage)
    {
      // set the values for the buttom
      strButtonText = _setting.PluginName;

      // no image or picture
      strButtonImage = String.Empty;
      strButtonImageFocus = String.Empty;
      strPictureImage = "hover_youtubefm.png";

      return true;
    }
    // init the skin
    public override bool Init()
    {
      if (!string.IsNullOrEmpty(_setting.User))
      {
        service.setUserCredentials(_setting.User, _setting.Password);
      }
      
      return Load(GUIGraphicsContext.Skin + @"\youtubevideosbase.xml");
    }

    void provider_OnError(Exception ex)
    {
      
    }
     //do the init before page load
    protected override void OnPageLoad()
    {
      updateStationLogoTimer.Enabled = true;
      GUIPropertyManager.SetProperty("#nowplaying", " ");
      if (MessageGUI.Item != null)
      {

      }
      else
      {
        if (NavigationStack.Count == 0)
        {
          ClearLabels("Curent");
          ClearLabels("NowPlaying");
          GUIPropertyManager.SetProperty("#header.title", _setting.PluginName);
          ShowVevo("");
          //switch (_setting.InitialDisplay)
          //{
          //  case 1:
          //    ShowHome(_setting.InitialCat);
          //    break;
          //  case 2:
          //    SearchVideo(_setting.InitialSearch);
          //    break;
          //  case 3:
          //    DoHome();
          //    break;
          //  default:
          //    break;
          //}
          ShowPanel();
        }
        else
        {
          DoBack();
          GUIControl.FocusControl(GetID, listControl.GetID);
        }
      }
      base.OnPageLoad();
    }

      private string GetRegionOpt()
      {
          if (_setting.Region == "All")
              return "";
          if (_setting.Region == "Ask")
          {
              GUIDialogMenu dlg = (GUIDialogMenu) GUIWindowManager.GetWindow((int) Window.WINDOW_DIALOG_MENU);
              if (dlg == null) return "";
              dlg.Reset();
              //dlg.SetHeading(25653); // Sort options
              foreach (KeyValuePair<string, string> valuePair in Youtube2MP._settings.Regions)
              {
                  dlg.Add(valuePair.Key);
              }
              dlg.DoModal(GetID);
              if (dlg.SelectedId == -1) return "";
              return Youtube2MP._settings.Regions[dlg.SelectedLabelText];
          }
          return Youtube2MP._settings.Regions[_setting.Region];
      }

      private void InitList(string queryuri)
      {
          if (_setting.MusicFilter && queryuri != YouTubeQuery.CreateFavoritesUri(null))
              queryuri += "_Music";
          string reg = GetRegionOpt();
          if (!string.IsNullOrEmpty(reg))
              queryuri = queryuri.Replace("standardfeeds", "standardfeeds/" + reg);
          YouTubeQuery query = new YouTubeQuery(queryuri);

          //if (queryuri == YouTubeQuery.CreateFavoritesUri(null))
          //    query = SetParamToYouTubeQuery(query, true);
          //else
          //{
          //    query = SetParamToYouTubeQuery(query, false);
          //}

          query.NumberToRetrieve = 50;
          query.SafeSearch = YouTubeQuery.SafeSearchValues.None;
          if (uploadtime != YouTubeQuery.UploadTime.AllTime)
              query.Time = uploadtime;

          YouTubeFeed vidr = service.Query(query);

          if (vidr.Entries.Count > 0)
          {
              SaveListState(true);
              addVideos(vidr, false, query);
              GUIPropertyManager.SetProperty("#header.title", vidr.Title.Text);
              UpdateGui();
          }
          else
          {
              Err_message("No item was found !");
          }
      }

      // remeber the selection on page leave
    protected override void OnPageDestroy(int new_windowId)
    {
      SaveListState(false);
      _setting.Save();
      base.OnPageDestroy(new_windowId);
    }
    //// do the clicked action
    protected override void OnClicked(int controlId, GUIControl control, Action.ActionType actionType)
    {
      ////
      //// look for button pressed
      ////
      //// record ?
      if (actionType == Action.ActionType.ACTION_RECORD)
      {
        //ExecuteRecord();
      }
      else if (control == btnSwitchView)
      {
        switch ((View)mapSettings.ViewAs)
        {
          case View.List:
            mapSettings.ViewAs = (int)View.Icons;
            break;
          case View.Icons:
            mapSettings.ViewAs = (int)View.BigIcons;
            break;
          case View.BigIcons:
            mapSettings.ViewAs = (int)View.Albums;
            break;
          case View.Albums:
            mapSettings.ViewAs = (int)View.Filmstrip;
            break;
          case View.Filmstrip:
            mapSettings.ViewAs = (int)View.List;
            break;
        }
        ShowPanel();
        GUIControl.FocusControl(GetID, control.GetID);
      }
      else if (control == listControl)
      {
        // execute only for enter keys
        if (actionType == Action.ActionType.ACTION_SELECT_ITEM)
        {
          // station selected
          DoListSelection();
        }
      }
      //else if (control == sortButton)
      //{
      //  //sort button selected
      //  OnShowSortOptions();
      //}
      else if (control == searchButton)
      {
        DoSearch();
        GUIControl.FocusControl(GetID, listControl.GetID);
      }
      else if (control == homeButton)
      {
        DoHome();
        GUIControl.FocusControl(GetID, listControl.GetID);
      }
      else if (control == searchHistoryButton)
      {
        DoShowHistory();
        GUIControl.FocusControl(GetID, listControl.GetID);
      }
      else if (control == btnPlayList)
      {
        GUIWindowManager.ActivateWindow(29051);
      }
      else if (control == btnNowPlaying)
      {
        GUIWindowManager.ActivateWindow(29052);
      }
      base.OnClicked(controlId, control, actionType);
    }

    private void DoShowHistory()
    {
      if (_setting.SearchHistory.Count > 0)
      {
        GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
        if (dlg == null) return;
        dlg.Reset();
        dlg.SetHeading("Search History");
        for (int i = _setting.SearchHistory.Count; i > 0; i--)
        {
          dlg.Add(_setting.SearchHistory[i-1]);
        }
        dlg.DoModal(GetID);
        if (dlg.SelectedId == -1) return;
        SearchVideo(dlg.SelectedLabelText);
        NavigationStack.Clear();
      }
      else
      {
        Err_message("No search history was found");
      }
    }


    private void DoHome()
    {
      GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
      if (dlg == null) return;
      dlg.Reset();
      //dlg.SetHeading(25653); // Sort options
      for (int i = 0; i < _setting.Cats.Count; i++)
      {
        dlg.Add(_setting.Cats[i]);
      }

      dlg.DoModal(GetID);
      if (dlg.SelectedId == -1) return;
      int select = dlg.SelectedLabel;
      ShowHome(select);
      NavigationStack.Clear();
      uploadtime = YouTubeQuery.UploadTime.AllTime;
    }

    void GetTimeOpt()
    {
      if (_setting.Time)
      {
        GUIDialogMenu dlg1 = (GUIDialogMenu)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
        if (dlg1 == null) return;
        dlg1.Reset();
        for (int i = 0; i < _setting.TimeList.Count; i++)
        {
          dlg1.Add(_setting.TimeList[i]);
        }

        dlg1.DoModal(GetID);
        if (dlg1.SelectedId != -1)
        {
          uploadtime = (YouTubeQuery.UploadTime)dlg1.SelectedLabel + 1;
        }
      }    
    }

    private void ShowVevo(string user)
    {

      YouTubeQuery query;
      if(!string.IsNullOrEmpty(user))
      {
        if(user.ToLower()=="vevo")
        query = new YouTubeQuery(string.Format("http://gdata.youtube.com/feeds/api/users/vevo/favorites"));
        else
          query = new YouTubeQuery(string.Format("http://gdata.youtube.com/feeds/api/users/{0}/uploads",user));  
      }
      else
      {
        query = new YouTubeQuery("http://gdata.youtube.com/feeds/api/channels?q=vevo");        
      }

    
      //if (queryuri == YouTubeQuery.CreateFavoritesUri(null))
      //    query = SetParamToYouTubeQuery(query, true);
      //else
      //{
      //    query = SetParamToYouTubeQuery(query, false);
      //}

      query.NumberToRetrieve = 50;
      query.SafeSearch = YouTubeQuery.SafeSearchValues.None;
      if (uploadtime != YouTubeQuery.UploadTime.AllTime)
        query.Time = uploadtime;

      YouTubeFeed vidr = service.Query(query);
      
      if (vidr.Entries.Count > 0)
      {
        SaveListState(true);
        addVideos(vidr, false, query);
        GUIPropertyManager.SetProperty("#header.title", vidr.Title.Text);
        UpdateGui();
      }
      else
      {
        Err_message("No item was found !");
      }
    }

    private void ShowHome(int poz)
    {
      switch (poz)
      {
        case 0:
          GetTimeOpt();
          InitList(YouTubeQuery.MostViewedVideo);
          break;
        case 1:
          GetTimeOpt();
          InitList(YouTubeQuery.TopRatedVideo);
          break;
        case 2:
          InitList(YouTubeQuery.RecentlyFeaturedVideo);
          break;
        case 3:
          GetTimeOpt();
          InitList(YouTubeQuery.MostDiscussedVideo);
          break;
        case 4:
          GetTimeOpt();
          InitList(YouTubeQuery.FavoritesVideo);
          break;
        case 5:
          InitList(YouTubeQuery.MostLinkedVideo);
          break;
        case 6:
          InitList(YouTubeQuery.MostRespondedVideo);
          break;
        case 7:
          InitList(YouTubeQuery.MostRecentVideo);
          break;
        case 8:
          InitList(YouTubeQuery.CreateFavoritesUri(null));
          break;
        case 9:
          if (Youtube2MP._settings.LocalFile.Items.Count == 0)
          {
            Err_message("No downloded item was found !");
          }
          else
          {
            SaveListState(true);
            foreach (LocalFileStruct entry in Youtube2MP._settings.LocalFile.Items)
            {
              GUIListItem item = new GUIListItem();
              // and add station name & bitrate
              item.Label = entry.Title;
              item.Label2 = "";
              item.IsFolder = false;

              string imageFile = Path.GetDirectoryName(entry.LocalFile) + "\\" + Path.GetFileNameWithoutExtension(entry.LocalFile) + ".png";
              if (File.Exists(imageFile))
              {
                item.ThumbnailImage = imageFile;
                //item.IconImage = "defaultVideoBig.png";
                item.IconImage = imageFile;
                item.IconImageBig = imageFile;
              }
              item.MusicTag = entry;
              item.OnItemSelected += new GUIListItem.ItemSelectedHandler(item_OnItemSelected);
              listControl.Add(item);
            }
            GUIPropertyManager.SetProperty("#header.title", Youtube2MP._settings.Cats[9]);
            UpdateGui();
          }
          break;
      }
    }

    //// override action responses
    public override void OnAction(Action action)
    {
      if (action.wID == Action.ActionType.ACTION_PREVIOUS_MENU)
      {
        if (listControl.Focus)
        {
          if (NavigationStack.Count > 0)
          {
            DoBack();
            return;
          }
        }
      }

      if (action.wID == Action.ActionType.ACTION_PARENT_DIR)
      {
        GUIListItem item = listControl[0];
        
        if ((item != null) && item.IsFolder && (item.Label == ".."))
        {
          DoBack();
          return;
        }
      }
      UpdateGui();
      base.OnAction(action);
    }
    // do regulary updates
    public override void Process()
    {
      // update the gui
      UpdateGui();
      base.Process();
    }

    protected void OnShowSortOptions()
    {
    }
 
    #endregion
    #region helper func's

    private void DoListSelection()
    {
      GUIListItem selectedItem = listControl.SelectedListItem;
      if (selectedItem != null)
      {
        if (selectedItem.Label != "..")
        {
            YouTubeQuery qu = selectedItem.MusicTag as YouTubeQuery;
            if (qu != null)
            {
                YouTubeFeed vidr = service.Query(qu);
                Log.Debug("Next page: {0}", qu.Uri.ToString());
                if (vidr.Entries.Count > 0)
                {
                    SaveListState(true);
                    addVideos(vidr, false, qu);
                    UpdateGui();
                }
            }
          //--------------------
          LocalFileStruct file = selectedItem.MusicTag as LocalFileStruct;
          YouTubeEntry vide;
          if (file != null)
          {
            Uri videoEntryUrl = new Uri("http://gdata.youtube.com/feeds/api/videos/" + file.VideoId);
            Video video = Youtube2MP.request.Retrieve<Video>(videoEntryUrl);
            vide = video.YouTubeEntry;
          }
          else
          {
            vide = selectedItem.MusicTag as YouTubeEntry;
          }

          if (vide != null)
          {
            if (vide.VideoId == null)
            {
              ShowVevo(vide.Authors[0].Name);
            }
            else
            {
              DoPlay(vide, true, listControl.ListView);
            }
          }
        }
        else
        {
          DoBack();
        }
      }
      GUIWaitCursor.Hide();
      //throw new Exception("The method or operation is not implemented.");
    }

    private VirtualKeyboard LoadSMSKey()
    {
      return (SmsStyledKeyboard)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_VIRTUAL_SMS_KEYBOARD); 
    }

    private void DoSearch()
    {
      string searchString = "";
      VirtualKeyboard keyboard;
      // display an virtual keyboard
      if (_setting.UseSMSStyleKeyBoard)
      {
        try
        {
          keyboard = LoadSMSKey();
        }
        catch
        {
          keyboard = (VirtualKeyboard)GUIWindowManager.GetWindow((int)Window.WINDOW_VIRTUAL_KEYBOARD);
        }
      }
      else
      {
        keyboard = (VirtualKeyboard)GUIWindowManager.GetWindow((int)Window.WINDOW_VIRTUAL_KEYBOARD);
      }
      if (null == keyboard) return;

      keyboard.Reset();
      keyboard.Text = searchString;
      keyboard.DoModal(GetWindowId());
      if (keyboard.IsConfirmed)
      {
        // input confirmed -- execute the search
        searchString = keyboard.Text;
      }

      if ("" != searchString)
      {
          SearchVideo(searchString);
          NavigationStack.Clear();
      }
    }

    private void SearchVideo(string searchString)
    {
        YouTubeQuery query = new YouTubeQuery(YouTubeQuery.DefaultVideoUri);
        query = SetParamToYouTubeQuery(query, false);
        //query.VQ = searchString;
        query.Query = searchString;
        query.OrderBy = "relevance";
        
        YouTubeFeed vidr = service.Query(query);

        foreach (AtomLink link in vidr.Links)
        {
            if (link.Rel == "http://schemas.google.com/g/2006#spellcorrection")
            {
                GUIDialogYesNo dlgYesNo = (GUIDialogYesNo) GUIWindowManager.GetWindow((int) Window.WINDOW_DIALOG_YES_NO);
                if (null == dlgYesNo)
                    return;
                dlgYesNo.SetHeading("Did you mean ?"); //resume movie?
                dlgYesNo.SetLine(1, link.Title);
                dlgYesNo.SetDefaultToYes(true);
                dlgYesNo.DoModal(GUIWindowManager.ActiveWindow);
                if (dlgYesNo.IsConfirmed)
                {
                    SearchVideo(link.Title);
                    return;
                }
            }
        }

        if (vidr.Entries.Count > 0)
        {
            SaveListState(true);
            addVideos(vidr, false, query);
            UpdateGui();
            if (_setting.SearchHistory.Contains(searchString.Trim()))
                _setting.SearchHistory.Remove(searchString.Trim());
            _setting.SearchHistory.Add(searchString.Trim());
        }
        else
        {
            Err_message("No item was found !");
        }
    }

      private void DoBack()
    {
      if (NavigationStack.Count > 0)
      {
        GUIControl.ClearControl(GetID, listControl.GetID);
        NavigationObject obj = NavigationStack.Pop() as NavigationObject;
        obj.SetItems(listControl);
        listControl.SelectedListItemIndex = obj.Position;
        GUIPropertyManager.SetProperty("#header.title", obj.Title);
        mapSettings.ViewAs = (int)obj.CurrentView;
        ShowPanel();
      }
    }
    
    //public void UpdateList()
    //{
   
    //}

    void ShowPanel()
    {
      int itemIndex = listControl.SelectedListItemIndex;
      if (mapSettings.ViewAs == (int)View.BigIcons)
      {
        listControl.View = GUIFacadeControl.ViewMode.LargeIcons;
      }
      else if (mapSettings.ViewAs == (int)View.Albums)
      {
        listControl.View = GUIFacadeControl.ViewMode.AlbumView;
      }
      else if (mapSettings.ViewAs == (int)View.Icons)
      {
        listControl.View = GUIFacadeControl.ViewMode.SmallIcons;
      }
      else if (mapSettings.ViewAs == (int)View.List)
      {
        listControl.View = GUIFacadeControl.ViewMode.List;
      }
      else if (mapSettings.ViewAs == (int)View.Filmstrip)
      {
        listControl.View = GUIFacadeControl.ViewMode.Filmstrip;
      }
      else if (mapSettings.ViewAs == (int)View.PlayList)
      {
        listControl.View = GUIFacadeControl.ViewMode.Playlist;
      }

      if (itemIndex > -1)
      {
        GUIControl.SelectItemControl(GetID, listControl.GetID, itemIndex);
      }
     
    }

    /// <summary>
    /// Called when [show context menu].
    /// </summary>
    protected override void OnShowContextMenu()
    {
      GUIListItem selectedItem = listControl.SelectedListItem;
      
      YouTubeEntry videoEntry;
      LocalFileStruct file = selectedItem.MusicTag as LocalFileStruct;
      if (file != null)
      {
        Uri videoEntryUrl = new Uri("http://gdata.youtube.com/feeds/api/videos/" + file.VideoId);
        Video video = Youtube2MP.request.Retrieve<Video>(videoEntryUrl);
        videoEntry = video.YouTubeEntry;
      }
      else
      {
        videoEntry = selectedItem.MusicTag as YouTubeEntry;
      }

      if (videoEntry == null)
        return;
      GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_MENU);
      if (dlg == null)
        return;
      dlg.Reset();
      dlg.SetHeading(498); // menu
      dlg.Add("Related Videos");
      dlg.Add("Video responses for this video");
      dlg.Add("All videos from this user : "+videoEntry.Authors[0].Name);
      dlg.Add("Add to playlist");
      dlg.Add("Add All to playlist");
      dlg.Add("Add to favorites");
      dlg.Add("Options");
      dlg.Add("Download Video"); 
      dlg.DoModal(GetID);
      if (dlg.SelectedId == -1)
        return;
      switch (dlg.SelectedLabel)
      {
        case 0: //relatated
          {
            if (videoEntry.RelatedVideosUri != null)
            {
              YouTubeQuery query = new YouTubeQuery(videoEntry.RelatedVideosUri.Content);
              YouTubeFeed vidr = service.Query(query);
              if (vidr.Entries.Count > 0)
              {
                SaveListState(true);
                addVideos(vidr, false, query);
                UpdateGui();
              }
              else
              {
                Err_message("No item was found !");
              }

            }
          }
          break;
        case 1: //respponse
          {
            if (videoEntry.VideoResponsesUri != null)
            {
              YouTubeQuery query = new YouTubeQuery(videoEntry.VideoResponsesUri.Content);
              YouTubeFeed vidr = service.Query(query);
              if (vidr.Entries.Count > 0)
              {
                SaveListState(true);
                addVideos(vidr, false, query);
                UpdateGui();
              }
              else
              {
                Err_message("No response was found !");
              }

            }
          }
          break;
        case 2: //relatated
          {
            if (videoEntry.RelatedVideosUri != null)
            {
              Video video = Youtube2MP.request.Retrieve<Video>(new Uri("http://gdata.youtube.com/feeds/api/videos/" + videoEntry.VideoId));
              YouTubeQuery query = new YouTubeQuery(string.Format("http://gdata.youtube.com/feeds/api/users/{0}/uploads", video.Author));
              YouTubeFeed vidr = service.Query(query);
              if (vidr.Entries.Count > 0)
              {
                SaveListState(true);
                addVideos(vidr, false, query);
                UpdateGui();
              }
              else
              {
                Err_message("No item was found !");
              }

            }
          }
          break;
        case 3:
          {
            VideoInfo inf = SelectQuality(videoEntry);
            if (inf.Quality != VideoQuality.Unknow)
            {
              AddItemToPlayList(selectedItem, inf);
            }
          }
          break;
        case 4:
          {
            VideoInfo inf = SelectQuality(videoEntry);
            inf.Items = new Dictionary<string, string>();
            foreach (GUIListItem item in listControl.ListView.ListItems)
            {
                AddItemToPlayList(item, new VideoInfo(inf));
            }
          }
          break;
        case 5:
          {
            try
            {
              service.Insert(new Uri(YouTubeQuery.CreateFavoritesUri(null)), videoEntry);
            }
            catch (Exception)
            {
              Err_message("Wrong request or wrong user identification");
            }
          }
          break;
        case 6:
          DoOptions();
          break;
        case 7: // download
          {
            if (Youtube2MP._settings.LocalFile.Get(videoEntry.VideoId) != null)
            {
              Err_message("Item already downloaded !");
            }
            else
            {
              if (VideoDownloader.IsBusy)
              {
                Err_message("Another donwnload is in progress");
                dlgProgress = (GUIDialogProgress)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_PROGRESS);
                if (dlgProgress != null)
                {
                  dlgProgress.Reset();
                  dlgProgress.SetHeading("Download progress");
                  dlgProgress.SetLine(1, "");
                  dlgProgress.SetLine(2, "");
                  dlgProgress.SetPercentage(0);
                  dlgProgress.Progress();
                  dlgProgress.ShowProgressBar(true);
                  dlgProgress.DoModal(GetID);
                }
              }
              else
              {
                VideoInfo inf = SelectQuality(videoEntry);
                string streamurl = Youtube2MP.StreamPlaybackUrl(videoEntry, inf);
                  VideoDownloader.AsyncDownload(streamurl,
                                                Youtube2MP._settings.DownloadFolder + "\\" +
                                                Utils.GetFilename(videoEntry.Title.Text + "{" + videoEntry.VideoId + "}") +
                                                Path.GetExtension(streamurl));
                VideoDownloader.Entry = videoEntry;
              }
            }
          }
          break;
      }
    }


    private void DoOptions()
    {
      GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_MENU);
      if (dlg == null)
        return;
      do
      {
        dlg.Reset();
        dlg.SetHeading(498); // menu
        dlg.Add(string.Format("NowPlaying in place of Fullscreen: {0}", _setting.ShowNowPlaying));
        dlg.Add(string.Format("Ask for time period: {0}", _setting.Time));
        dlg.Add(string.Format("Enable music videos filtering: {0}", _setting.MusicFilter));
        dlg.Add(string.Format("Use extrem filter music videos: {0}", _setting.UseExtremFilter));
        dlg.Add(string.Format("Use SMS style keyboard: {0}", _setting.UseSMSStyleKeyBoard));
        dlg.DoModal(GetID);
        if (dlg.SelectedId == -1)
          return;
        switch (dlg.SelectedLabel)
        {
          case 0:
            _setting.ShowNowPlaying = !_setting.ShowNowPlaying;
            break;
          case 1:
            _setting.Time = !_setting.Time;
            break;
          case 2:
            _setting.MusicFilter = !_setting.MusicFilter;
            break;
          case 3:
            _setting.UseExtremFilter = !_setting.UseExtremFilter;
            break;
          case 4:
            _setting.UseSMSStyleKeyBoard = !_setting.UseSMSStyleKeyBoard;
            break;
        }

      } while (true);
    }

    private void DoInfo()
    {
      //YahooMusicInfoGUI guiInfo = (YahooMusicInfoGUI)GUIWindowManager.GetWindow(27051);
      //guiInfo.InfoItem = listControl.SelectedListItem.MusicTag as VideoResponse;
      //guiInfo.provider = this.provider;
      //GUIWindowManager.ActivateWindow(27051);
    }

  
    /// <summary>
    /// Adds to favorites.
    /// </summary>
    /// <param name="p">The station id.</param>
    private void AddToFavorites(int p)
    {
      //try
      //{
      //  FavoriteFolderUpdateRequest req = new FavoriteFolderUpdateRequest();
      //  req.ItemIds = new int[] { p };
      //  req.Identification = iden;
      //  websrv.Favorite_StationListAdd(req);
      //  grabber.GetData(grabber.CurentUrl, grabber.CacheIsUsed, false);
      //  UpdateList();
      //}
      //catch (Exception)
      //{
      //  Err_message(25659);
      //}
    }

    public void UpdateGui()
    {

      string textLine = string.Empty;
      View view = (View)mapSettings.ViewAs;
      bool sortAsc = mapSettings.SortAscending;
      switch (view)
      {
        case View.List:
          textLine = GUILocalizeStrings.Get(101);
          break;
        case View.Icons:
          textLine = GUILocalizeStrings.Get(100);
          break;
        case View.BigIcons:
          textLine = GUILocalizeStrings.Get(417);
          break;
        case View.Albums:
          textLine = GUILocalizeStrings.Get(529);
          break;
        case View.Filmstrip:
          textLine = GUILocalizeStrings.Get(733);
          break;
        case View.PlayList:
          textLine = GUILocalizeStrings.Get(101);
          break;

      }
      GUIControl.SetControlLabel(GetID, btnSwitchView.GetID, textLine);

    }

    void SortChanged(object sender, SortEventArgs e)
    {
      // save the new state
      mapSettings.SortAscending = e.Order != SortOrder.Descending;
      // update the list
      //UpdateList();
      //UpdateButtonStates();
      GUIControl.FocusControl(GetID, ((GUIControl)sender).GetID);
    }

    #endregion



    void addVideos(YouTubeFeed videos, bool level,YouTubeQuery qu)
    {
      int count = 0;
      if (level)
      {
        GUIListItem item = new GUIListItem();
        item.Label = "..";
        item.IsFolder = true;
        MediaPortal.Util.Utils.SetDefaultIcons(item);
        listControl.Add(item);
      }
      GUIPropertyManager.SetProperty("#header.title", videos.Title.Text);
      updateStationLogoTimer.Enabled = false;
      downloaQueue.Clear();
      foreach (YouTubeEntry entry in videos.Entries)
      {
        if (filterVideoContens(entry))
        {
          GUIListItem item = new GUIListItem();
          // and add station name & bitrate
          item.Label = entry.Title.Text; 
          item.Label2 = "";
          item.IsFolder = false;
          count++;
          try
          {
            item.Duration = Convert.ToInt32(entry.Duration.Seconds, 10);
              item.Rating = (float) entry.Rating.Average*2;
          }
          catch (Exception ex)
          {
            Log.Error(ex);
          }
          if (entry.Media != null)
          {
            string imageFile = GetLocalImageFileName(GetBestUrl(entry.Media.Thumbnails));
            if (File.Exists(imageFile))
            {
              item.ThumbnailImage = imageFile;
              //item.IconImage = "defaultVideoBig.png";
              item.IconImage = imageFile;
              item.IconImageBig = imageFile;
            }
            else
            {
              Utils.SetDefaultIcons(item);
              DownloadImage(GetBestUrl(entry.Media.Thumbnails), item);
            }
          }
          item.MusicTag = entry;
          item.OnItemSelected+=item_OnItemSelected;
          listControl.Add(item);
        } 
      }
      if (qu.NumberToRetrieve > 0 && videos.TotalResults > qu.NumberToRetrieve)
      {
        GUIListItem item = new GUIListItem();
        item.Label = string.Format("Next Page {0} - {1} ", qu.StartIndex + count, qu.StartIndex + qu.NumberToRetrieve + count);
        qu.StartIndex += qu.NumberToRetrieve;
        item.Label = "";
        item.Label2 = "Next page";
        item.IsFolder = true;
        MediaPortal.Util.Utils.SetDefaultIcons(item);
        item.MusicTag = qu;
        listControl.Add(item);
      }
      UpdateGui();
      updateStationLogoTimer.Enabled = true;
    }

    private void item_OnItemSelected(GUIListItem item, GUIControl parent)
    {
      YouTubeEntry vid = item.MusicTag as YouTubeEntry ;
      if (vid != null)
      {
        SetLabels(vid, "Curent");
      }
      else
      {
        ClearLabels("Curent"); ;
      }
    }

    private void SaveListState(bool clear)
    {
      if (listControl.ListView.ListItems.Count > 0)
      {
        NavigationStack.Push(new NavigationObject(listControl.ListView, GUIPropertyManager.GetProperty("#header.title"), listControl.SelectedListItemIndex, (View)mapSettings.ViewAs));
      }
      if (clear)
      {
        GUIControl.ClearControl(GetID, listControl.GetID);
        Youtube2MP.temp_player.Reset();
        Youtube2MP.temp_player.GetPlaylist(PlayListType.PLAYLIST_MUSIC_VIDEO).Clear();
      }
    }
    

  }
}
