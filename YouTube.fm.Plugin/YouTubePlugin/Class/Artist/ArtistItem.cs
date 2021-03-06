﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YouTubePlugin.Class.Artist
{
  public class ArtistItem
  {
    public string Id { get; set; }
    public string Name { get; set; }
    public string Img_url { get; set; }
    public string User { get; set; }
    public int Db_id { get; set; }
    public string Tags { get; set; }
    public string Bio { get; set; }
    public string AMImg_url { get; set; }

    public string LocalImage
    {
      get { return Youtube2MP.GetLocalImageFileName(Img_url); }
    }

    public ArtistItem()
    {
      Db_id = -1;
    }

    public override string ToString()
    {
      return Name;
    }
  }
}
