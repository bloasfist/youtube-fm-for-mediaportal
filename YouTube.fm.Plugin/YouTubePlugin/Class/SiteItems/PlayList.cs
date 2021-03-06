﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Google.GData.YouTube;

namespace YouTubePlugin.Class.SiteItems
{
  public class PlayList : ISiteItem
  {
    public PlayList()
    {
      Name = "Playlist";
      ConfigControl = new PlayListControl();
    }

    public Control ConfigControl { get; set; }
    public void Configure(SiteItemEntry entry)
    {
      ((PlayListControl)ConfigControl).SetEntry(entry);
    }

    public string Name { get; set; }
    public GenericListItemCollections GetList(SiteItemEntry entry)
    {
      GenericListItemCollections res = new GenericListItemCollections();
      res.FolderType = 1;
      string url = string.Format("http://gdata.youtube.com/feeds/api/playlists/{0}", entry.GetValue("id"));
      if (!string.IsNullOrEmpty(entry.GetValue("url")))
        url = entry.GetValue("url");
      YouTubeQuery query = new YouTubeQuery(url);
      query.NumberToRetrieve = 50;
      do
      {
        YouTubeFeed videos = Youtube2MP.service.Query(query);
        res.Title = videos.Title.Text;
        foreach (YouTubeEntry youTubeEntry in videos.Entries)
        {
          res.Items.Add(Youtube2MP.YouTubeEntry2ListItem(youTubeEntry));
        }
        query.StartIndex += 50;
        if (videos.TotalResults < query.StartIndex + 50)
          break;
      } while (true);
      res.ItemType = ItemType.Video;
      return res;
    }

    public GenericListItemCollections HomeGetList(SiteItemEntry itemEntry)
    {
      GenericListItemCollections res = new GenericListItemCollections();

      GenericListItem listItem = new GenericListItem()
      {
        Title = itemEntry.Title,
        IsFolder = true,
        //LogoUrl = YoutubeGUIBase.GetBestUrl(youTubeEntry.Media.Thumbnails),
        Tag = itemEntry
      };
      res.Items.Add(listItem);
      return res;
    }
  }
}
