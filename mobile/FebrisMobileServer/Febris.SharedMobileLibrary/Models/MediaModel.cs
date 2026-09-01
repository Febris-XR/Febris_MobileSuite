// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Text;

namespace Febris.SharedMobileLibrary.Models
{
    public class MediaModel:BaseModel
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public MediaType MediaType { get; set; }
        public bool Uploaded { get; set; }
        public bool Compressed { get; set; }
    }

    public enum MediaType 
    {
        None,
        Video,
        Audio,
        Picture
    }
}
