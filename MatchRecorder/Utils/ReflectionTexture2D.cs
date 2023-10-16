using DuckGame;
using System;
using System.Linq;
using System.Reflection;

namespace MatchRecorder.Utils;

internal static class ReflectionTexture2D
{
	private static Type PreviewPairType { get; set; }
	private static FieldInfo PreviewPairPreviewFieldInfo { get; set; }
	private static Type PreviewPairPreviewFieldType { get; set; }
	private static MethodInfo PreviewPairPreviewDisposeMethodInfo { get; set; }
	private static PropertyInfo PreviewPairPreviewWidthPropertyInfo { get; set; }
	private static PropertyInfo PreviewPairPreviewHeightPropertyInfo { get; set; }
	private static MethodInfo PreviewPairPreviewGetDataMethodInfo { get; set; }
	private static MethodInfo PreviewPairPreviewGetDataDuckColorMethodInfo { get; set; }

	static ReflectionTexture2D()
	{
		PreviewPairType = typeof( LevelMetaData.PreviewPair );
		PreviewPairPreviewFieldInfo = PreviewPairType.GetField( "preview", BindingFlags.Public | BindingFlags.Instance );
		PreviewPairPreviewFieldType = PreviewPairPreviewFieldInfo.FieldType;
		PreviewPairPreviewDisposeMethodInfo = PreviewPairPreviewFieldType.GetMethod( "Dispose", BindingFlags.Instance | BindingFlags.Public );

		PreviewPairPreviewWidthPropertyInfo = PreviewPairPreviewFieldType.GetProperty( "Width" );
		PreviewPairPreviewHeightPropertyInfo = PreviewPairPreviewFieldType.GetProperty( "Height" );

		//can't work with the overloads present //PreviewPairPreviewGetDataMethodInfo = PreviewPairPreviewFieldType.GetMethod( "GetData", BindingFlags.Instance | BindingFlags.Public );

		//thankfully we only need the first one
		PreviewPairPreviewGetDataMethodInfo = PreviewPairPreviewFieldType
			.GetMethods( BindingFlags.Instance | BindingFlags.Public )
			.FirstOrDefault( x => x.Name == "GetData" );

		if( PreviewPairPreviewGetDataMethodInfo != null )
		{
			PreviewPairPreviewGetDataDuckColorMethodInfo = PreviewPairPreviewGetDataMethodInfo.MakeGenericMethod( typeof( Color ) );
		}
	}

	internal static void DisposePreviewField( LevelMetaData.PreviewPair previewPair )
	{
		var previewObject = PreviewPairPreviewFieldInfo.GetValue( previewPair );
		if( previewObject != null )
		{
			PreviewPairPreviewDisposeMethodInfo.Invoke( previewObject, new object[] { true } );
		}
	}

	internal static int GetWidth( LevelMetaData.PreviewPair previewPair )
	{
		var previewObject = PreviewPairPreviewFieldInfo.GetValue( previewPair );
		if( previewObject != null )
		{
			return (int) PreviewPairPreviewWidthPropertyInfo.GetValue( previewObject );
		}

		return 0;
	}

	internal static int GetHeight( LevelMetaData.PreviewPair previewPair )
	{
		var previewObject = PreviewPairPreviewFieldInfo.GetValue( previewPair );
		if( previewObject != null )
		{
			return (int) PreviewPairPreviewHeightPropertyInfo.GetValue( previewObject );
		}

		return 0;
	}

}
