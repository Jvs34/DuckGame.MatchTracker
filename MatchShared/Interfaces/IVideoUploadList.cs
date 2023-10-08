using MatchShared.DataClasses;
using System.Collections.Generic;

namespace MatchShared.Interfaces;

public interface IVideoUploadList
{
	List<VideoUpload> VideoUploads { get; set; }
}
